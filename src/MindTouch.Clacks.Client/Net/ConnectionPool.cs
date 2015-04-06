/*
 * MindTouch.Clacks
 * 
 * Copyright (C) 2011-2013 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/MindTouch.Clacks
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Linq;
using System.Reflection;
using log4net;
using MindTouch.Clacks.Client.Net.Helper;

namespace MindTouch.Clacks.Client.Net {

    // TODO: need to timeout idle pools
    // TODO: need way to configure "BYE" command for automatic socket disposal
    public class ConnectionPool : IConnectionPool, IDisposable {

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private class Available {
            public readonly DateTime Queued;
            public readonly ISocket Socket;

            public Available(ISocket socket) {
                Queued = DateTime.UtcNow;
                Socket = socket;
            }
        }

        private class WaitHandle {
            private readonly ManualResetEventSlim _handle = new ManualResetEventSlim();
            private ISocket _socket;
            private bool _waiting = true;

            public bool Provide(ISocket socket) {
                if(!_waiting) {
                    return false;
                }
                _socket = socket;
                _handle.Set();
                return true;
            }

            public void Wait(TimeSpan timeout) {
                _handle.Wait(timeout);
            }

            public ISocket Socket {
                get {
                    _waiting = false;
                    return _socket;
                }
            }
        }

        private class WaitingQueue {
            private readonly object _syncroot;
            private readonly Queue<WaitHandle> _queue = new Queue<WaitHandle>();

            public WaitingQueue(object syncroot) {
                _syncroot = syncroot;
            }

            public int Count { get { lock(_syncroot) { return _queue.Count; } } }

            public bool ProvideSocket(ISocket socket) {
                lock(_syncroot) {
                    while(_queue.Any()) {
                        var next = _queue.Dequeue();
                        if(next.Provide(socket)) {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public ISocket AwaitSocket(TimeSpan timeout) {
                var handle = new WaitHandle();
                lock(_syncroot) {
                    _queue.Enqueue(handle);
                }
                handle.Wait(timeout);
                lock(_syncroot) {
                    return handle.Socket;
                }
            }
        }

        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan DefaultCheckInterval = TimeSpan.FromSeconds(10);
        public static readonly int DefaultMaxConnections = 100;
        private static readonly Dictionary<string, ConnectionPool> _pools = new Dictionary<string, ConnectionPool>();

        public static ConnectionPool GetPool(IPEndPoint endPoint) {
            lock(_pools) {
                ConnectionPool pool;
                var key = string.Format("{0}:{1}", endPoint.Address, endPoint.Port);
                if(!_pools.TryGetValue(key, out pool)) {
                    pool = new ConnectionPool(endPoint);
                    _pools[key] = pool;
                }
                return pool;
            }
        }

        public static ConnectionPool GetPool(string host, int port) {
            lock(_pools) {
                ConnectionPool pool;
                var key = string.Format("{0}:{1}", host, port);
                if(!_pools.TryGetValue(key, out pool)) {
                    pool = new ConnectionPool(host, port);
                    _pools[key] = pool;
                }
                return pool;
            }
        }

        public static ConnectionPool Create(IPEndPoint endpoint) {
            return new ConnectionPool(endpoint);
        }

        public static ConnectionPool Create(string host, int port) {
            return new ConnectionPool(host, port);
        }

        private readonly object _syncroot = new object();
        private readonly Func<ISocket> _socketFactory;
        private readonly List<Available> _availableSockets = new List<Available>();
        private readonly WaitingQueue _waitingQueue;
        private readonly Dictionary<ISocket, WeakReference> _busySockets = new Dictionary<ISocket, WeakReference>();
        private readonly Timer _socketCleanupTimer;
        private TimeSpan _cleanupInterval = TimeSpan.FromSeconds(60);
        private bool _disposed;

        private ConnectionPool(IPEndPoint endpoint) : this(() => SocketAdapter.Open(endpoint, DefaultConnectTimeout)) { }

        private ConnectionPool(string host, int port) : this(() => SocketAdapter.Open(host, port, DefaultConnectTimeout)) { }

        public ConnectionPool(Func<ISocket> socketFactory) {
            ConnectTimeout = DefaultConnectTimeout;
            MaxConnections = DefaultMaxConnections;
            IdleTimeout = DefaultIdleTimeout;
            CheckInterval = DefaultCheckInterval;
            _socketFactory = socketFactory;
            _waitingQueue = new WaitingQueue(_syncroot);
            _socketCleanupTimer = new Timer(ReapSockets, null, _cleanupInterval, _cleanupInterval);
        }

        public TimeSpan IdleTimeout { get; set; }
        public TimeSpan CheckInterval { get; set; }
        public TimeSpan ConnectTimeout { get; set; }
        public int MaxConnections { get; set; }

        public TimeSpan CleanupInterval {
            get { return _cleanupInterval; }
            set {
                ThrowIfDisposed();
                if(value == _cleanupInterval) {
                    return;
                }
                _cleanupInterval = value;
                _socketCleanupTimer.Change(_cleanupInterval, _cleanupInterval);
            }
        }

        public int ActiveConnections {
            get {
                lock(_syncroot) {
                    return _busySockets.Count;
                }
            }
        }

        public int IdleConnections {
            get {
                lock(_syncroot) {
                    return _availableSockets.Count;
                }
            }
        }

        public ISocket GetSocket() {
            ThrowIfDisposed();
            ISocket socket = null;
            lock(_syncroot) {
                while(true) {
                    while(true) {
                        if(!_availableSockets.Any()) {
                            break;
                        }

                        // take available from end of list
                        var available = _availableSockets[_availableSockets.Count - 1];
                        _availableSockets.RemoveAt(_availableSockets.Count - 1);

                        // if socket is disposed, has been idle for more than 10 seconds or is not Connected
                        if(!available.Socket.IsDisposed && (available.Queued > DateTime.UtcNow.Add(-CheckInterval) || available.Socket.Connected)) {
                            socket = available.Socket;
                            break;
                        }

                        // dead socket, clean up and try pool again
                        if(!available.Socket.IsDisposed) {
                            available.Socket.Dispose();
                        }
                    }
                    if(socket != null) {
                        break;
                    }
                    if(_availableSockets.Count + _busySockets.Count < MaxConnections) {
                        socket = _socketFactory();
                        _log.DebugFormat("created new socket for pool (max: {0}, available: {1}, busy: {2}, queued: {3})",
                            MaxConnections,
                            _availableSockets.Count,
                            _busySockets.Count,
                            _waitingQueue.Count
                        );
                        break;
                    }
                    ReapSockets(null);
                    if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {
                        break;
                    }
                }
                if(socket != null) {
                    return WrapSocket(socket);
                }
            }
            socket = _waitingQueue.AwaitSocket(ConnectTimeout);
            if(socket == null) {
                throw new PoolExhaustedException(MaxConnections);
            }
            return WrapSocket(socket);
        }

        private ISocket WrapSocket(ISocket socket) {
            lock(_syncroot) {
                var poolsocket = new PoolSocket(socket, Reclaim);
                _busySockets.Add(socket, new WeakReference(poolsocket));
                return poolsocket;
            }
        }

        private void Reclaim(ISocket socket) {
            if(_disposed) {
                socket.Dispose();
                return;
            }
            lock(_syncroot) {
                if(!_busySockets.Remove(socket)) {
                    return;
                }
                if(socket.IsDisposed) {

                    // drop disposed sockets
                    socket.Dispose();
                    return;
                }

                // Try to hand the socket to the next client waiting in line
                if(_waitingQueue.ProvideSocket(socket)) {
                    return;
                }

                if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {

                    // drop socket if we've already hit the max
                    socket.Dispose();
                    return;
                }

                // Add reclaimed socket to end of available list
                _availableSockets.Add(new Available(socket));
            }
        }

        private void ReapSockets(object state) {
            lock(_syncroot) {
                var disposed = (from busy in _busySockets where busy.Key.IsDisposed select busy.Key).ToArray();
                foreach(var socket in disposed) {
                    _busySockets.Remove(socket);
                }
                var staleTime = DateTime.UtcNow.Subtract(IdleTimeout);
                var dead = 0;
                while(true) {

                    // check oldest (first in available list) for staleness
                    if(!_availableSockets.Any() || _availableSockets[0].Queued > staleTime) {
                        break;
                    }
                    var idle = _availableSockets[0];
                    _availableSockets.RemoveAt(0);
                    idle.Socket.Dispose();
                    dead++;
                }
                _log.DebugFormat("reaped {4} disposed and & {5} dead sockets (max: {0}, available: {1}, busy: {2}, queued: {3})",
                    MaxConnections,
                    _availableSockets.Count,
                    _busySockets.Count,
                    _waitingQueue.Count,
                    disposed.Length,
                    dead
                );
            }
        }

        private void ThrowIfDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        public void Dispose() {
            if(_disposed) {
                return;
            }
            _disposed = true;
            lock(_syncroot) {
                foreach(var available in _availableSockets) {
                    available.Socket.Dispose();
                }
                _availableSockets.Clear();
            }
        }
    }
}
