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
using System.Net.Sockets;

namespace MindTouch.Arpysee.Server {
    public class ArpyseeServer : IDisposable {
        private readonly IPEndPoint _listenEndpoint;
        private readonly ICommandDispatcher _dispatcher;
        private readonly Socket _listenSocket;

        public ArpyseeServer(IPEndPoint listenEndpoint, ICommandDispatcher dispatcher) {
            _listenEndpoint = listenEndpoint;
            _dispatcher = dispatcher;
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(_listenEndpoint);
            _listenSocket.Listen(10);
            Console.WriteLine("listening on {0}", _listenEndpoint);
            BeginWaitForConnection();
        }

        private void BeginWaitForConnection() {
            _listenSocket.BeginAccept(OnAccept, _listenSocket);
        }

        private void OnAccept(IAsyncResult result) {
            ArpyseeAsyncClientHandler asyncClientHandler = null;
            try {
                asyncClientHandler = new ArpyseeAsyncClientHandler(_listenSocket.EndAccept(result), _dispatcher);
            } catch(SocketException) {
                if(asyncClientHandler != null) {
                    asyncClientHandler.Dispose();
                }
            } catch(ObjectDisposedException) {
                return;
            }
            BeginWaitForConnection();
        }

        private void OnAcceptSync(IAsyncResult result) {
            BeginWaitForConnection();
            ArpyseeSyncClientHandler asyncClientHandler = null;
            try {
                asyncClientHandler = new ArpyseeSyncClientHandler(_listenSocket.EndAccept(result), _dispatcher);
            } catch(SocketException) {
                if(asyncClientHandler != null) {
                    asyncClientHandler.Dispose();
                }
            } catch(ObjectDisposedException) {
                return;
            }
        }

        public void Dispose() {
            _listenSocket.Dispose();
        }
    }
}