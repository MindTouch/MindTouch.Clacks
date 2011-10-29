/*
 * MindTouch.Arpysee
 * 
 * Copyright (C) 2011 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/MindTouch.Arpysee
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
using System.Net;
using MindTouch.Arpysee.Client.Net;

namespace MindTouch.Arpysee.Client {

    // TODO: need way to control Receive and Send timeouts
    public class ArpyseeClient : IDisposable {
        public const string DEFAULT_TUBE = "default";
        private readonly ISocket _socket;
        private readonly byte[] _buffer = new byte[16 * 1024];
        private bool _disposed;

        public ArpyseeClient(IPAddress address, int port)
            : this(ConnectionPool.GetPool(address, port)) {
        }

        public ArpyseeClient(string host, int port)
            : this(ConnectionPool.GetPool(host, port)) {
        }

        public ArpyseeClient(IConnectionPool pool)
            : this(pool.GetSocket()) {
        }

        public ArpyseeClient(ISocket socket) {
            if(socket == null) {
                throw new ArgumentNullException("socket");
            }
            _socket = socket;
            InitSocket();
        }

        protected virtual void InitSocket() {}

        public bool Disposed {
            get {
                if(_disposed) {
                    return true;
                }
                Connected();
                return _disposed;
            }
        }

        public Response Exec(Request request) {
            VerifyConnection();
            _socket.SendRequest(request);
            var response = _socket.ReceiveResponse(_buffer, request);
            return response;
        }

        private void VerifyConnection() {
            ThrowIfDisposed();
            if(Connected()) {
                return;
            }
            ThrowIfDisposed();
        }

        private void ThrowIfDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private bool Connected() {
            if(!_socket.Connected) {
                _socket.Dispose();
                _disposed = true;
            }
            return !_disposed;
        }

        public void Dispose() {
            Dispose(true);
        }

        private void Dispose(bool suppressFinalizer) {
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

        ~ArpyseeClient() {
            Dispose(false);
        }
    }
}

