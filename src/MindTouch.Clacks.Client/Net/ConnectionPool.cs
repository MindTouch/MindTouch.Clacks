/*
 * MindTouch.Clacks
 * 
 * Copyright (C) 2011 Arne F. Claassen
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
using MindTouch.Clacks.Client.Net.Helper;

namespace MindTouch.Clacks.Client.Net {

    public enum ConnectionTestingBehavior {
        Poll,
        Reconnect
    }


    // TODO: need to timeout idle pools
    // TODO: need way to configure "BYE" command for automatic socket disposal
    public class ConnectionPool : IConnectionPool, IDisposable {

        private class Available {
            public readonly DateTime Queued;
            public readonly ISocket Socket;

            public Available(ISocket socket) {
                Queued = DateTime.UtcNow;
                Socket = socket;
            }
        }

        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(30);
        public static readonly ConnectionTestingBehavior DefaultConnectionTestingBehavior = ConnectionTestingBehavior.Poll;
        public static readonly int DefaultMaxConnections = 100;
        private static readonly Dictionary<string, ConnectionPool> _pools = new Dictionary<string, ConnectionPool>();

        public static ConnectionPool GetPool(IPEndPoint endPoint) {
            lock(_pools) {
                ConnectionPool pool;
                var key = string.Format("{0}:{1}", endPoint.Address, endPoint.Port);
                if(!_pools.TryGetValue(key, out pool)) {
                    pool = new ConnectionPool(endPoint, DefaultConnectionTestingBehavior);
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
                    pool = new ConnectionPool(host, port, DefaultConnectionTestingBehavior);
                    _pools[key] = pool;
                }
                return pool;
            }
        }

        public static ConnectionPool Create(IPEndPoint endpoint, ConnectionTestingBehavior connectionTestingBehavior = ConnectionTestingBehavior.Poll) {
            return new ConnectionPool(endpoint, connectionTestingBehavior);
        }

        public static ConnectionPool Create(string host, int port, ConnectionTestingBehavior connectionTestingBehavior = ConnectionTestingBehavior.Poll) {
            return new ConnectionPool(host, port, connectionTestingBehavior);
        }

        private readonly Func<ISocket> _socketFactory;
        private readonly List<Available> _availableSockets = new List<Available>();
        private readonly Dictionary<ISocket, WeakReference> _busySockets = new Dictionary<ISocket, WeakReference>();
        private readonly Timer _socketCleanupTimer;
        private TimeSpan _cleanupInterval = TimeSpan.FromSeconds(60);
        private bool _disposed;

        private ConnectionPool(IPEndPoint endpoint, ConnectionTestingBehavior connectionTestingBehavior)
            : this(() => SocketAdapter.Open(endpoint, DefaultConnectTimeout), connectionTestingBehavior) { }

        private ConnectionPool(string host, int port, ConnectionTestingBehavior connectionTestingBehavior)
            : this(() => SocketAdapter.Open(host, port, DefaultConnectTimeout), connectionTestingBehavior) { }

        public ConnectionPool(Func<ISocket> socketFactory) : this(socketFactory, DefaultConnectionTestingBehavior) { }

        public ConnectionPool(Func<ISocket> socketFactory, ConnectionTestingBehavior connectionTestingBehavior) {
            ConnectTimeout = DefaultConnectTimeout;
            MaxConnections = DefaultMaxConnections;
            IdleTimeout = DefaultIdleTimeout;
            ConnectionTestingBehavior = connectionTestingBehavior;
            _socketFactory = socketFactory;
            _socketCleanupTimer = new Timer(ReapSockets, null, _cleanupInterval, _cleanupInterval);
        }

        public TimeSpan IdleTimeout { get; set; }
        public TimeSpan ConnectTimeout { get; set; }
        public int MaxConnections { get; set; }

        public TimeSpan CleanupInterval {
            get { return _cleanupInterval; }
            set {
                if(value == _cleanupInterval) {
                    return;
                }
                _cleanupInterval = value;
                _socketCleanupTimer.Change(_cleanupInterval, _cleanupInterval);
            }
        }

        public ConnectionTestingBehavior ConnectionTestingBehavior { get; private set; }

        public int ActiveConnections {
            get {
                lock(_availableSockets) {
                    return _busySockets.Count;
                }
            }
        }

        public int IdleConnections {
            get {
                lock(_availableSockets) {
                    return _availableSockets.Count;
                }
            }
        }

        public ISocket GetSocket() {
            lock(_availableSockets) {
                ISocket socket = null;
                while(socket == null && _availableSockets.Count > 0) {
                    if(_availableSockets.Count <= 0) {
                        break;
                    }

                    // take available from end of list
                    var available = _availableSockets[_availableSockets.Count - 1];
                    _availableSockets.RemoveAt(_availableSockets.Count - 1);
                    if(ConnectionTestingBehavior == ConnectionTestingBehavior.Reconnect || available.Socket.Connected) {
                        socket = available.Socket;
                        continue;
                    }
                    available.Socket.Dispose();
                }
                if(socket == null) {
                    if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {
                        ReapSockets(null);
                        if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {
                            throw new PoolExhaustedException(MaxConnections);
                        }
                        return GetSocket();
                    }
                    socket = _socketFactory();
                }
                return WrapSocket(socket);
            }
        }

        private ISocket WrapSocket(ISocket socket) {
            var poolsocket = new PoolSocket(socket, Reclaim, Reconnect);
            _busySockets.Add(socket, new WeakReference(poolsocket));
            return poolsocket;
        }

        private void Reclaim(ISocket socket) {
            if(_disposed) {
                socket.Dispose();
                return;
            }
            lock(_availableSockets) {
                if(!_busySockets.Remove(socket)) {
                    return;
                }
                if(socket.IsDisposed
                    || (ConnectionTestingBehavior == ConnectionTestingBehavior.Poll && !socket.Connected)
                    || _availableSockets.Count + _busySockets.Count >= MaxConnections
                ) {

                    // drop socket
                    socket.Dispose();
                    return;
                }
                
                // Add reclaimed socket to end of available list
                _availableSockets.Add(new Available(socket));
            }
        }

        private ISocket Reconnect(PoolSocket poolSocket, ISocket deadSocket) {
            if(_disposed || ConnectionTestingBehavior == ConnectionTestingBehavior.Poll) {
                return null;
            }
            lock(_availableSockets) {
                _busySockets.Remove(deadSocket);
                try {
                    deadSocket.Dispose();
                } catch { }
                var socket = _socketFactory();
                _busySockets.Add(socket, new WeakReference(poolSocket));
                return socket;
            }
        }

        private void ReapSockets(object state) {
            lock(_availableSockets) {
                var disposed = (from busy in _busySockets where busy.Key.IsDisposed select busy.Key).ToArray();
                foreach(var socket in disposed) {
                    _busySockets.Remove(socket);
                }
                var staleTime = DateTime.UtcNow.Subtract(IdleTimeout);
                while(true) {
                    
                    // check oldest (first in available list) for staleness
                    if(!_availableSockets.Any() || _availableSockets[0].Queued > staleTime) {
                        break;
                    }
                    var idle = _availableSockets[0];
                    _availableSockets.RemoveAt(0);
                    idle.Socket.Dispose();
                }
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        private void Dispose(bool suppressFinalizer) {
            if(_disposed) {
                return;
            }
            _disposed = true;
            if(suppressFinalizer) {
                GC.SuppressFinalize(this);
            }
            lock(_availableSockets) {
                foreach(var available in _availableSockets) {
                    available.Socket.Dispose();
                }
                _availableSockets.Clear();
            }
        }

        ~ConnectionPool() {
            Dispose(false);
        }
    }
}
