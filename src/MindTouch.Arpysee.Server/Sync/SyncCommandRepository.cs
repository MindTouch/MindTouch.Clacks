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

        private readonly Dictionary<string, ISyncCommandRegistration> _commands = new Dictionary<string, ISyncCommandRegistration>();
        private Func<IRequest, Exception, IResponse> _errorHandler;
        private SyncSingleCommandRegistration _defaultCommandRegistration = new SyncSingleCommandRegistration(r => Response.Create("UNKNOWN"));
        private Func<IRequest, IResponse> _disconnectHandler = r => Response.Create("BYE");
        private string _disconnectCommand = "BYE";

        public ISyncCommandHandler GetHandler(string[] commandArgs) {
            var command = commandArgs[0];
            if(command == _disconnectCommand) {
                return BuildDisconnectHandler();
            }
            ISyncCommandRegistration registration;
            if(!_commands.TryGetValue(command, out registration)) {
                registration = _defaultCommandRegistration;
            }
            return registration.GetHandler(commandArgs, _errorHandler);
        }

        private ISyncCommandHandler BuildDisconnectHandler() {
            return SyncSingleCommandHandler.DisconnectHandler(_disconnectCommand, request => {
                try {
                    return _disconnectHandler(request);
                } catch(Exception handlerException) {
                    _log.Warn("disconnect handler threw an exception, continuating with disconnect", handlerException);
                    return Response.Create("BYE");
                }
            });
        }

        public void Default(Func<IRequest, IResponse> handler) {
            _defaultCommandRegistration = new SyncSingleCommandRegistration(handler);
        }

        public void Error(Func<IRequest, Exception, IResponse> handler) {
            _errorHandler = handler;
        }

        public void Disconnect(string command, Func<IRequest, IResponse> handler) {
            _disconnectCommand = command;
            _disconnectHandler = handler;
        }

        public void AddCommand(string command, Func<IRequest, IResponse> handler, DataExpectation dataExpectation) {
            _commands[command] = new SyncSingleCommandRegistration(handler, dataExpectation);
        }
        public void AddCommand(string command, Func<IRequest, IEnumerable<IResponse>> handler, DataExpectation dataExpectation) {
            _commands[command] = new SyncMultiCommandRegistration(handler, dataExpectation);
        }
    }
}