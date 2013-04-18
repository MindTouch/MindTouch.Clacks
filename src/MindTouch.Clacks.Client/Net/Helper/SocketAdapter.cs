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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MindTouch.Clacks.Client.Net.Helper {
    public class SocketAdapter : ISocket {

        public static ISocket Open(string host, int port, TimeSpan connectTimeout) {
            return Open(host, port, (int)connectTimeout.TotalMilliseconds);
        }

        public static ISocket Open(string host, int port, int connectTimeout = Timeout.Infinite, int receiveTimeout = Timeout.Infinite, int sendTimeout = Timeout.Infinite) {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, SendTimeout = sendTimeout, ReceiveTimeout = receiveTimeout };
            var ar = socket.BeginConnect(host, port, null, null);
            if(ar.AsyncWaitHandle.WaitOne(connectTimeout)) {
                socket.EndConnect(ar);
            } else {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
                throw new TimeoutException();
            }
            return new SocketAdapter(socket);
        }

        public static ISocket Open(IPEndPoint endPoint, TimeSpan connectTimeout) {
            return Open(endPoint, (int)connectTimeout.TotalMilliseconds);
        }

        public static ISocket Open(IPEndPoint endPoint, int connectTimeout = Timeout.Infinite, int receiveTimeout = Timeout.Infinite, int sendTimeout = Timeout.Infinite) {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, SendTimeout = sendTimeout, ReceiveTimeout = receiveTimeout };
            var ar = socket.BeginConnect(endPoint, null, null);
            if(ar.AsyncWaitHandle.WaitOne(connectTimeout)) {
                socket.EndConnect(ar);
            } else {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
                throw new TimeoutException();
            }
            return new SocketAdapter(socket);
        }

        private readonly Socket _socket;

        public SocketAdapter(Socket socket) {
            _socket = socket;
        }

        public void Dispose() {
            try {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socket.Dispose();
            } catch { }
        }

        public bool Connected {
            get {
                if(!_socket.Connected) {
                    return false;
                }
                var blockingState = _socket.Blocking;
                try {
                    var tmp = new byte[1];

                    _socket.Blocking = false;
                    _socket.Send(tmp, 0, 0);
                    return true;
                } catch(SocketException e) {
                    // 10035 == WSAEWOULDBLOCK 
                    if(e.NativeErrorCode.Equals(10035)) {
                        return true;
                    } else {
                        return false;
                    }
                } finally {
                    _socket.Blocking = blockingState;
                }
            }
        }

        public int Send(byte[] buffer, int offset, int size) {
            return _socket.Send(buffer, offset, size, SocketFlags.None);
        }

        public int Receive(byte[] buffer, int offset, int size) {
            return _socket.Receive(buffer, offset, size, SocketFlags.None);
        }
    }
}