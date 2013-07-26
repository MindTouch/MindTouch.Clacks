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
using System.Net;

namespace MindTouch.Clacks.Server.Sync {
    public class SyncCommandRepository : ISyncCommandDispatcher {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly Dictionary<string, ISyncCommandRegistration> _commands = new Dictionary<string, ISyncCommandRegistration>();
        private Func<IRequest, Exception, IResponse> _errorHandler = DefaultHandlers.ErrorHandler;
        private ISyncCommandRegistration _defaultCommandRegistration = DefaultHandlers.SyncCommandRegistration;
        private Func<IRequest, IResponse> _disconnectHandler = DefaultHandlers.DisconnectHandler;
        private string _disconnectCommand = "BYE";

        public ISyncCommandHandler GetHandler(Connection connection, string[] commandArgs) {
            var command = commandArgs.FirstOrDefault() ?? string.Empty;
            if(command == _disconnectCommand) {
                return BuildDisconnectHandler();
            }
            ISyncCommandRegistration registration;
            if(!_commands.TryGetValue(command, out registration)) {
                registration = _defaultCommandRegistration;
            }
            return registration.GetHandler(connection, commandArgs, _errorHandler);
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
            _defaultCommandRegistration = new SyncCommandRegistration(
                DataExpectation.Auto,
                (client, cmd, dataLength, arguments, errorHandler) =>
                    new SyncSingleCommandHandler(client, cmd, arguments, dataLength, handler, errorHandler)
            );
        }

        public void Error(Func<IRequest, Exception, IResponse> handler) {
            _errorHandler = handler;
        }

        public void Disconnect(string command, Func<IRequest, IResponse> handler) {
            _disconnectCommand = command;
            _disconnectHandler = handler;
        }

        public void AddCommand(string command, Func<IRequest, IResponse> handler, DataExpectation dataExpectation) {
            _commands[command] = new SyncCommandRegistration(
                dataExpectation,
                (client, cmd, dataLength, arguments, errorHandler) =>
                    new SyncSingleCommandHandler(client, cmd, arguments, dataLength, handler, errorHandler)
            );
        }
        public void AddCommand(string command, Func<IRequest, IEnumerable<IResponse>> handler, DataExpectation dataExpectation) {
            _commands[command] = new SyncCommandRegistration(
                dataExpectation,
                (connection, cmd, dataLength, arguments, errorHandler) =>
                    new SyncMultiCommandHandler(connection, cmd, arguments, dataLength, handler, errorHandler)
            );
        }
    }
}