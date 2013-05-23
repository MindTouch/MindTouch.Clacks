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

namespace MindTouch.Clacks.Server {
    public class ClacksServer : IDisposable {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly IPEndPoint _listenEndpoint;
        private readonly IClacksInstrumentation _instrumentation;
        private readonly IClientHandlerFactory _clientHandlerFactory;
        private readonly Socket _listenSocket;
        private readonly Dictionary<Guid, IClientHandler> _openConnections = new Dictionary<Guid, IClientHandler>();

        public ClacksServer(IPEndPoint listenEndpoint, IClacksInstrumentation instrumentation, IClientHandlerFactory clientHandlerFactory) {
            _listenEndpoint = listenEndpoint;
            _instrumentation = instrumentation;
            _clientHandlerFactory = clientHandlerFactory;
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            _listenSocket.Bind(_listenEndpoint);
            _listenSocket.Listen((int) SocketOptionName.MaxConnections);
            _log.InfoFormat("Created server on {0} with handler {1}", listenEndpoint, clientHandlerFactory);
            BeginWaitForConnection();
        }

        private void BeginWaitForConnection() {
            try {
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
            } catch(SocketException e) {
                _log.Warn("Wait for connection failed", e);
                return;
            } catch(ObjectDisposedException e) {
                _log.Debug("Aborted wait for socket due to object disposed");
                return;
            }
        }

        private void OnAccept(IAsyncResult result) {
            BeginWaitForConnection();
            Socket socket;
            try {
                socket = _listenSocket.EndAccept(result);
                _log.DebugFormat("Accepted client from {0}", socket.RemoteEndPoint);
            } catch(SocketException e) {
                _log.Error("Socket error on receive, shutting down server", e);
                return;
            } catch(ObjectDisposedException e) {
                _log.Debug("Server already disposed, abort listen");
                return;
            }
            var id = Guid.NewGuid();
            var handler = _clientHandlerFactory.Create(id, socket, _instrumentation, RemoveHandler);
            lock(_openConnections) {
                _openConnections.Add(id, handler);
            }
            handler.ProcessRequests();
        }

        private void RemoveHandler(IClientHandler handler) {
            lock(_openConnections) {
                _openConnections.Remove(handler.Id);
            }
        }

        public void Dispose() {
            _listenSocket.Close();
            lock(_openConnections) {
                foreach(var connection in _openConnections.Values) {
                    connection.Dispose();
                }
                _openConnections.Clear();
            }
            _log.Debug("Disposed server socket");
        }
    }
}