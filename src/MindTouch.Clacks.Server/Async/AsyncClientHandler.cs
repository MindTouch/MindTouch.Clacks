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
using System.Net.Sockets;

namespace MindTouch.Clacks.Server.Async {
    public class AsyncClientHandler : AClientRequestHandler {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly IAsyncCommandDispatcher _dispatcher;
        private IAsyncCommandHandler _commandHandler;

        public AsyncClientHandler(Socket socket, IAsyncCommandDispatcher dispatcher, Action<IClientHandler> removeCallback)
            : base(socket, removeCallback) {
            _dispatcher = dispatcher;
        }

        // 2.
        protected override ICommandHandler Handler {
            get { return _commandHandler; }
        }

        protected override void Receive(Action<int, int> continuation) {
            try {
                _bufferPosition = 0;
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, r => {

                    // 3.
                    try {
                        var received = _socket.EndReceive(r);
                        if(received == 0) {
                            _log.Debug("received nothing, connection must have gone away");
                            Dispose();
                            return;
                        }
                        continuation(0, received);
                    } catch(SocketException e) {
                        _log.Warn("EndReceive failed", e);
                        Dispose();
                    } catch(ObjectDisposedException) {
                        _log.Debug("socket was already disposed (EndReceive)");
                        return;
                    }
                }, null);
            } catch(SocketException e) {
                _log.Warn("BeginReceive failed", e);
                Dispose();
            } catch(ObjectDisposedException) {
                _log.Debug("socket was already disposed (BeginReceive)");
            }
        }

        protected override void InitializeHandler(string[] command) {
            _commandHandler = _dispatcher.GetHandler(command);
        }

        // 13/14.
        protected override void ProcessResponse() {
            var responseHandler = new AsyncResponseHandler(
                _socket,
                EndCommandRequest,
                error => {
                    _log.Warn("Send failed", error);
                    Dispose();

                }
            );
            _commandHandler.GetResponse(responseHandler.SendResponse);
        }
    }
}
