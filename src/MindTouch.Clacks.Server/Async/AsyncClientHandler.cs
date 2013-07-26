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

namespace MindTouch.Clacks.Server.Async {
    public class AsyncClientHandler : AClientRequestHandler {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly IAsyncCommandDispatcher _dispatcher;
        private IAsyncCommandHandler _commandHandler;
        private IResponse _response;
        private Action _nextResponseCallback;
        private string _lastStatus;

        public AsyncClientHandler(Guid clientId, Socket socket, IAsyncCommandDispatcher dispatcher, IClacksInstrumentation instrumentation, Action<IClientHandler> removeCallback)
            : base(clientId, socket, instrumentation, removeCallback) {
            _dispatcher = dispatcher;
        }

        public override void ProcessRequests() {
            try {
                StartCommandRequest();
            } catch(Exception e) {
                _log.Warn("starting request processing failed", e);
            }
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
                    } catch(ObjectDisposedException) {
                        _log.Debug("socket was already disposed (EndReceive)");
                    } catch(Exception e) {
                        FailAndDispose("EndReceive failed", e);
                    }
                }, null);
            } catch(ObjectDisposedException) {
                _log.Debug("socket was already disposed (BeginReceive)");
            } catch(Exception e) {
                FailAndDispose("BeginReceive failed", e);
            }
        }

        protected override string InitializeHandler(string[] command) {
            _commandHandler = _dispatcher.GetHandler(Connection, command);
            return _commandHandler.Command;
        }

        // 13/14.
        protected override void ProcessCommand() {
            _commandHandler.GetResponse(ProcessResponse);
        }

        private void ProcessResponse(IResponse response, Action nextResponseCallback) {
            Action continuation = SendResponse;
            if(_response == null) {
                continuation = () => PrepareResponse(response.Status);
            }

            // null response: the last callback returned no more results, so we're done
            if(response == null) {
                _response = null;
                EndCommandRequest(_lastStatus);
                return;
            }
            _response = response;
            _nextResponseCallback = nextResponseCallback;
            _lastStatus = _response.Status;
            continuation();
        }

        protected override void SendResponse() {
            try {
                var data = _response.GetBytes();
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, r => {
                    try {
                        _socket.EndSend(r);
                        if(_nextResponseCallback != null) {
                            _nextResponseCallback();
                        } else {

                            // null callback: the source already knows there won't be any more responses
                            var status = _response.Status;
                            _response = null;
                            EndCommandRequest(status);
                        }
                    } catch(ObjectDisposedException) {
                        _log.Debug("socket was already disposed (EndSend)");
                    } catch(Exception e) {
                        FailAndDispose("Send failed (EndSend)", e);
                    }
                }, null);
            } catch(ObjectDisposedException) {
                _log.Debug("socket was already disposed (BeginSend)");
            } catch(Exception e) {
                FailAndDispose("Send failed (BeginSend)", e);
            }
        }

        protected override void CompleteRequest() {
            _response = null;
            if(Handler.DisconnectOnCompletion) {
                Dispose();
            } else {
                StartCommandRequest();
            }
        }
    }
}
