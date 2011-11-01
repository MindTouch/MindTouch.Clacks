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

        private readonly Dictionary<string, IAsyncCommandRegistration> _commands = new Dictionary<string, IAsyncCommandRegistration>();
        private Action<IRequest, Exception, Action<IResponse>> _errorHandler;
        private AsyncSingleCommandRegistration _defaultCommandRegistration = new AsyncSingleCommandRegistration((r, c) => c(Response.Create("UNKNOWN")));
        private Action<IRequest, Action<IResponse>> _disconnectHandler = (r, c) => c(Response.Create("BYE"));
        private string _disconnectCommand = "BYE";

        public IAsyncCommandHandler GetHandler(string[] commandArgs) {
            var command = commandArgs[0];
            if(command == _disconnectCommand) {
                return BuildDisconnectHandler();
            }
            IAsyncCommandRegistration registration;
            if(!_commands.TryGetValue(command, out registration)) {
                registration = _defaultCommandRegistration;
            }
            return registration.GetHandler(commandArgs, _errorHandler);
        }

        private IAsyncCommandHandler BuildDisconnectHandler() {
            return AsyncSingleCommandHandler.DisconnectHandler(_disconnectCommand, (request, response) => {
                try {
                    _disconnectHandler(request, response);
                } catch(Exception handlerException) {
                    _log.Warn("disconnect handler threw an exception, continuating with disconnect", handlerException);
                    response(Response.Create("BYE"));
                }
            });
        }

        public void Default(Action<IRequest, Action<IResponse>> handler) {
            _defaultCommandRegistration = new AsyncSingleCommandRegistration(handler, DataExpectation.Auto);
        }

        public void Error(Action<IRequest, Exception, Action<IResponse>> handler) {
            _errorHandler = handler;
        }

        public void Disconnect(string command, Action<IRequest, Action<IResponse>> handler) {
            _disconnectCommand = command;
            _disconnectHandler = handler;
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncSingleCommandRegistration(handler, dataExpectation);
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse, Action>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncMultiCommandRegistration(handler, dataExpectation);
        }

        public void AddCommand(string command, Action<IRequest, Action<IEnumerable<IResponse>>> handler, DataExpectation dataExpectation) {
            _commands[command] = new SyncMultiCommandRegistration(handler, dataExpectation);
        }
    }

}