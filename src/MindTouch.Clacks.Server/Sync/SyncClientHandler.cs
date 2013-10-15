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
using System.Linq;
using System.Net.Sockets;

namespace MindTouch.Clacks.Server.Sync {
    public class SyncClientHandler : AClientRequestHandler {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly ISyncCommandDispatcher _dispatcher;
        private ISyncCommandHandler _commandHandler;
        private IEnumerable<IResponse> _responses;
        private IResponse _handledResponse;

        public SyncClientHandler(Guid clientId, Socket socket, ISyncCommandDispatcher dispatcher, IClacksInstrumentation instrumentation, Action<IClientHandler> removeCallback)
            : base(clientId, socket, instrumentation, removeCallback) {
            _dispatcher = dispatcher;
        }

        public override void ProcessRequests() {
            while(!IsDisposed) {
                StartCommandRequest();
            }
        }

        protected override ICommandHandler Handler {
            get { return _commandHandler; }
        }

        protected override void Receive(Action<int, int> continuation) {
            int received;
            try {
                _bufferPosition = 0;
                received = _socket.Receive(_buffer, 0, _buffer.Length, SocketFlags.None);
                if(received == 0) {
                    Dispose();
                    return;
                }
            } catch(ObjectDisposedException) {
                _log.Debug("socket was already disposed");
                return;
            } catch(Exception e) {
                FailAndDispose("Receive failed", e);
                return;
            }
            continuation(0, received);
        }

        protected override string InitializeHandler(string[] command) {
            _commandHandler = _dispatcher.GetHandler(Connection, command);
            return _commandHandler.Command;
        }

        // 13/14.
        protected override void ProcessCommand() {
            _responses = _commandHandler.GetResponse();
            var status = _responses.First().Status;
            PrepareResponse(status);
        }

        protected override void SendResponse() {
            string finalStatus = null;
            foreach(var response in _responses) {
                _handledResponse = response;
                finalStatus = response.Status;
                var data = response.GetBytes();
                try {
                    _socket.Send(data);
                } catch(ObjectDisposedException) {
                    _log.Debug("socket was already disposed");
                    return;
                } catch(Exception e) {
                    FailAndDispose("Send failed", e);
                    return;
                }
            }
            EndCommandRequest(finalStatus);
        }

        protected override void CompleteRequest() {
            if(_handledResponse.IsDisconnect) {
                Dispose();
            }
        }
    }
}
