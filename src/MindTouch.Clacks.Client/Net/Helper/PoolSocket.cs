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
using System.Net.Sockets;

namespace MindTouch.Clacks.Client.Net.Helper {
    public class PoolSocket : ISocket {
        private readonly ISocket _socket;
        private readonly Action<ISocket> _reclaim;
        private bool _disposed;

        public PoolSocket(ISocket socket, Action<ISocket> reclaim) {
            _socket = socket;
            _reclaim = reclaim;
        }

        public bool Connected { get { return !_disposed && _socket.Connected; } }
        public bool IsDisposed { get { return _disposed; } }

        public void Dispose() {
            if(_disposed) {
                return;
            }
            _disposed = true;
            _reclaim(_socket);
        }

        public int Send(byte[] buffer, int offset, int size) {
            return Try(() => _socket.Send(buffer, offset, size));
        }

        public int Receive(byte[] buffer, int offset, int size) {
            return Try(() => _socket.Receive(buffer, offset, size));
        }

        private T Try<T>(Func<T> func, bool retry = true) {
            if(_disposed) {
                throw new ObjectDisposedException("PoolSocket");
            }
            try {
                return func();
            } catch(ObjectDisposedException e) {
                try {
                    Dispose();
                } catch { }
                throw;

            } catch(SocketException e) {
                try {
                    _socket.Dispose();
                    Dispose();
                } catch { }
                throw;

            }
        }
    }
}