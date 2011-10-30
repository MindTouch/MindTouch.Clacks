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

namespace MindTouch.Arpysee.Server.Sync {
    public class SyncCommandRepository : ISyncCommandDispatcher {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly Dictionary<string, SyncCommandRegistration> _commands = new Dictionary<string, SyncCommandRegistration>();
        private Func<IRequest, Exception, IResponse> _errorHandler;
        private SyncCommandRegistration _defaultCommandRegistration = new SyncCommandRegistration(r => Response.Create("UNKNOWN"));
        private SyncCommandRegistration _disconnectRegistration = new SyncCommandRegistration(r => Response.Create("BYE"));
        private string _disconnectCommand = "BYE";

        public ISyncCommandHandler GetHandler(string[] commandArgs) {
            var command = commandArgs[0];
            if(command == _disconnectCommand) {
                return BuildDisconnectHandler();
            }
            SyncCommandRegistration registration;
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
            return new SyncCommandHandler(command, arguments, dataLength, registration.Handler, _errorHandler);
        }

        private ISyncCommandHandler BuildDisconnectHandler() {
            return SyncCommandHandler.DisconnectHandler(_disconnectCommand, request => {
                try {
                    return _disconnectRegistration.Handler(request);
                } catch(Exception handlerException) {
                    _log.Warn("disconnect handler threw an exception, continuating with disconnect", handlerException);
                    return Response.Create("BYE");
                }
            });
        }

        public void Default(Func<IRequest, IResponse> handler) {
            _defaultCommandRegistration = new SyncCommandRegistration(handler);
        }

        public void Error(Func<IRequest, Exception, IResponse> handler) {
            _errorHandler = handler;
        }

        public void Disconnect(string command, Func<IRequest, IResponse> handler) {
            _disconnectCommand = command;
            _disconnectRegistration = new SyncCommandRegistration(handler);
        }

        public void AddCommand(string command, Func<IRequest, IResponse> handler, DataExpectation dataExpectation) {
            _commands[command] = new SyncCommandRegistration(handler, dataExpectation);
        }
    }
}