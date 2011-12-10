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
using MindTouch.Clacks.Client.Net;

namespace MindTouch.Clacks.Client {

    // TODO: need way to control Receive and Send timeouts
    // Note: ClacksClient is not threadsafe. It's assumed that whatever code incorporates manages access to it
    // in a threadsafe manner
    public class ClacksClient : IDisposable {
        public const string DEFAULT_TUBE = "default";
        private readonly ISocket _socket;
        private readonly byte[] _buffer = new byte[16 * 1024];
        private readonly ResponseReceiver _receiver;
        private bool _disposed;

        public ClacksClient(IPEndPoint endPoint)
            : this(ConnectionPool.GetPool(endPoint)) {
        }

        public ClacksClient(string host, int port)
            : this(ConnectionPool.GetPool(host, port)) {
        }

        public ClacksClient(IConnectionPool pool)
            : this(pool.GetSocket()) {
        }

        public ClacksClient(ISocket socket) {
            if(socket == null) {
                throw new ArgumentNullException("socket");
            }
            _socket = socket;
            _receiver = new ResponseReceiver(_socket);
            InitSocket();
        }

        protected virtual void InitSocket() { }

        public bool Disposed {
            get {
                if(_disposed) {
                    return true;
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
            _socket.SendRequest(request);
            _receiver.Reset(request);
            var response = _receiver.GetResponse();
            return response;
        }

        public IEnumerable<Response> Exec(MultiRequest request) {
            var requestInfo = request as IMultiRequestInfo;
            if(!requestInfo.IsValid) {
                throw new InvalidRequestException();
            }
            ThrowIfDisposed();
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
        }

        private void ThrowIfDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool suppressFinalizer) {
            if(_disposed) {
                return;
            }
            if(suppressFinalizer) {
                GC.SuppressFinalize(this);
            }
            if(_socket != null) {
                _socket.Dispose();
            }
            _disposed = true;
        }

        ~ClacksClient() {
            Dispose(false);
        }
    }
}

