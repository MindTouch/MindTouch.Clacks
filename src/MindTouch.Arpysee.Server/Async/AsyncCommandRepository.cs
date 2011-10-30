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
using System.Collections.Generic;

namespace MindTouch.Arpysee.Server.Async {
    public class AsyncCommandRepository : IAsyncCommandDispatcher {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly Dictionary<string, AsyncCommandRegistration> _commands = new Dictionary<string, AsyncCommandRegistration>();
        private Action<IRequest, Exception, Action<IResponse>> _errorHandler;
        private AsyncCommandRegistration _defaultCommandRegistration = new AsyncCommandRegistration((r, c) => c(Response.Create("UNKNOWN")));
        private AsyncCommandRegistration _disconnectRegistration = new AsyncCommandRegistration((r, c) => c(Response.Create("BYE")));
        private string _disconnectCommand = "BYE";

        public IAsyncCommandHandler GetHandler(string[] commandArgs) {
            var command = commandArgs[0];
            if(command == _disconnectCommand) {
                return BuildDisconnectHandler();
            }
            AsyncCommandRegistration registration;
            if(!_commands.TryGetValue(command, out registration)) {
                registration = _defaultCommandRegistration;
            }
            int dataLength = 0;
            string[] arguments;
            if(registration.DataExpectation == DataExpectation.Auto) {
                if(commandArgs.Length > 1) {
                    int.TryParse(commandArgs[commandArgs.Length - 1], out dataLength);
                }
            } else if(registration.DataExpectation == DataExpectation.Always) {
                if(commandArgs.Length == 1 || !int.TryParse(commandArgs[commandArgs.Length - 1], out dataLength)) {
                    throw new InvalidCommandException();
                }
            }
            if(dataLength == 0) {
                arguments = new string[commandArgs.Length - 1];
                if(arguments.Length > 0) {
                    Array.Copy(commandArgs, 1, arguments, 0, arguments.Length);
                }
            } else {
                arguments = new string[commandArgs.Length - 2];
                if(arguments.Length > 0) {
                    Array.Copy(commandArgs, 1, arguments, 0, arguments.Length - 1);
                }
            }
            return new AsyncCommandHandler(command, arguments, dataLength, registration.Handler, _errorHandler);
        }

        private IAsyncCommandHandler BuildDisconnectHandler() {
            return AsyncCommandHandler.DisconnectHandler(_disconnectCommand, (request, response) => {
                try {
                    _disconnectRegistration.Handler(request, response);
                } catch(Exception handlerException) {
                    _log.Warn("disconnect handler threw an exception, continuating with disconnect", handlerException);
                    response(Response.Create("BYE"));
                }
            });
        }

        public void Default(Action<IRequest, Action<IResponse>> handler) {
            _defaultCommandRegistration = new AsyncCommandRegistration(handler, DataExpectation.Auto);
        }

        public void Error(Action<IRequest, Exception, Action<IResponse>> handler) {
            _errorHandler = handler;
        }

        public void Disconnect(string command, Action<IRequest, Action<IResponse>> handler) {
            _disconnectCommand = command;
            _disconnectRegistration = new AsyncCommandRegistration(handler, DataExpectation.Never);
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(handler, dataExpectation);
        }

    }
}