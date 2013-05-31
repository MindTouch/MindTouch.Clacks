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
using System.Net.Sockets;
using MindTouch.Clacks.Client.Net;

namespace MindTouch.Clacks.Client {

    // TODO: need way to control Receive and Send timeouts
    // Note: ClacksClient is not threadsafe. It's assumed that whatever code incorporates it manages access to it
    // in a threadsafe manner
    public class ClacksClient : IDisposable {

        private readonly IConnectionPool _pool;
        private readonly bool _attemptReconnect;
        private ResponseReceiver _receiver;
        private bool _disposed;
        private ISocket _socket;

        public ClacksClient(IPEndPoint endPoint, bool attemptReconnect = true)
            : this(ConnectionPool.GetPool(endPoint), attemptReconnect) {
        }

        public ClacksClient(string host, int port, bool attemptReconnect = true)
            : this(ConnectionPool.GetPool(host, port), attemptReconnect) {
        }

        public ClacksClient(IConnectionPool pool, bool attemptReconnect = true) {
            _pool = pool;
            _attemptReconnect = attemptReconnect;
        }

        public ClacksClient(ISocket socket) {
            if(socket == null) {
                throw new ArgumentNullException("socket");
            }
            _socket = socket;
            _receiver = new ResponseReceiver(_socket);
        }

        protected virtual void InitSocket() {
            if(_socket != null && !_socket.IsDisposed) {
                return;
            }
            _socket = _pool.GetSocket();
            _receiver = new ResponseReceiver(_socket);
        }

        public bool Disposed {
            get {
                if(_disposed) {
                    return true;
                }
                if(_socket == null) {
                    return _disposed;
                }
                if(!_socket.Connected) {
                    _socket.Dispose();
                    _disposed = true;
                }
                return _disposed;
            }
        }

        public Response Exec(Request request) {
            ThrowIfDisposed();
            var isReconnect = !_attemptReconnect;
            while(true) {
                InitSocket();
                try {
                    _socket.SendRequest(request);
                    _receiver.Reset(request);
                    return _receiver.GetResponse();
                } catch(SocketException) {
                    DisposeSocket();
                    if(isReconnect) {
                        throw;
                    }
                } catch(ObjectDisposedException) {
                    DisposeSocket();
                    if(isReconnect) {
                        throw;
                    }
                }
                isReconnect = true;
                _socket = null;
            }
        }

        public IEnumerable<Response> Exec(MultiRequest request) {
            var requestInfo = request as IMultiRequestInfo;
            if(!requestInfo.IsValid) {
                throw new InvalidRequestException();
            }
            ThrowIfDisposed();
            var isReconnect = !_attemptReconnect;
            while(true) {
                InitSocket();
                try {
                    _socket.SendRequest(request);
                    _receiver.Reset(request);
                    var responses = new List<Response>();
                    while(true) {
                        var response = _receiver.GetResponse();
                        if(!requestInfo.IsExpected(response.Status)) {
                            return new[] { response };
                        }
                        responses.Add(response);
                        if(response.Status == requestInfo.TerminationStatus) {
                            return responses;
                        }
                    }
                } catch(SocketException) {
                    DisposeSocket();
                    if(isReconnect) {
                        throw;
                    }
                } catch(ObjectDisposedException) {
                    DisposeSocket();
                    if(isReconnect) {
                        throw;
                    }
                }
                isReconnect = true;
                _socket = null;
            }
        }

        private void ThrowIfDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        public void Dispose() {
            _disposed = true;
            DisposeSocket();
        }

        private void DisposeSocket() {
            if(_socket != null) {
                _socket.Dispose();
            }
        }
    }
}

