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
using System.Reflection;
using log4net;

namespace MindTouch.Clacks.Server.Async {
    public class AsyncCommandRepository : IAsyncCommandDispatcher {

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, IAsyncCommandRegistration> _commands = new Dictionary<string, IAsyncCommandRegistration>(StringComparer.InvariantCultureIgnoreCase);
        private Action<IRequest, Exception, Action<IResponse>> _errorHandler = DefaultHandlers.ErrorHandler;
        private IAsyncCommandRegistration _defaultCommandRegistration = DefaultHandlers.AsyncCommandRegistration;
        private Action<IRequest, Action<IResponse>> _disconnectHandler = DefaultHandlers.DisconnectHandler;
        private string _disconnectCommand = "BYE";

        public IAsyncCommandHandler GetHandler(IPEndPoint client, string[] commandArgs) {
            var command = commandArgs.FirstOrDefault() ?? string.Empty;
            if(command.Equals(_disconnectCommand, StringComparison.InvariantCultureIgnoreCase)) {
                return BuildDisconnectHandler();
            }
            IAsyncCommandRegistration registration;
            if(!_commands.TryGetValue(command, out registration)) {
                registration = _defaultCommandRegistration;
            }
            return registration.GetHandler(client, commandArgs, _errorHandler);
        }

        private IAsyncCommandHandler BuildDisconnectHandler() {
            return AsyncSingleCommandHandler.DisconnectHandler(_disconnectCommand, (request, response) => {
                try {
                    _disconnectHandler(request, response);
                } catch(Exception handlerException) {
                    _log.Warn("disconnect handler threw an exception, continuing with disconnect", handlerException);
                    response(Response.Create("BYE"));
                }
            });
        }

        public void Default(Action<IRequest, Action<IResponse>> handler) {
            _defaultCommandRegistration = new AsyncCommandRegistration(
                DataExpectation.Auto,
                (client, command, dataLength, arguments, errorHandler) =>
                    new AsyncSingleCommandHandler(client, command, arguments, dataLength, handler, errorHandler)
            );
        }

        public void Error(Action<IRequest, Exception, Action<IResponse>> handler) {
            _errorHandler = handler;
        }

        public void Disconnect(string command, Action<IRequest, Action<IResponse>> handler) {
            _disconnectCommand = command;
            _disconnectHandler = handler;
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(
                dataExpectation,
                (client, cmd, dataLength, arguments, errorHandler) =>
                    new AsyncSingleCommandHandler(client, cmd, arguments, dataLength, handler, errorHandler)
            );
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse, Action>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(
                dataExpectation,
                (client, cmd, dataLength, arguments, errorHandler) =>
                    new AsyncMultiCommandHandler(client, cmd, arguments, dataLength, handler, errorHandler)
            );
        }

        public void AddCommand(string command, Action<IRequest, Action<IEnumerable<IResponse>>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(
                dataExpectation,
                (client, cmd, dataLength, arguments, errorHandler) =>
                    new SyncMultiCommandHandler(client, cmd, arguments, dataLength, handler, errorHandler)
            );
        }
    }

}