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

namespace MindTouch.Clacks.Server.Async {
    public class AsyncCommandRepository : IAsyncCommandDispatcher {

        private readonly Dictionary<string, IAsyncCommandRegistration> _commands = new Dictionary<string, IAsyncCommandRegistration>(StringComparer.InvariantCultureIgnoreCase);
        private Action<IRequest, Exception, Action<IResponse>> _errorHandler = DefaultHandlers.ErrorHandler;
        private IAsyncCommandRegistration _defaultCommandRegistration = DefaultHandlers.AsyncCommandRegistration;

        public AsyncCommandRepository() {
            AddCommand("BYE", (r,c) => c(DefaultHandlers.DisconnectHandler(r)), DataExpectation.Never);
        }

        public IAsyncCommandHandler GetHandler(Connection connection, string[] commandArgs) {
            var command = commandArgs.FirstOrDefault() ?? string.Empty;
            IAsyncCommandRegistration registration;
            if(!_commands.TryGetValue(command, out registration)) {
                registration = _defaultCommandRegistration;
            }
            return registration.GetHandler(connection, commandArgs, _errorHandler);
        }


        public void Default(Action<IRequest, Action<IResponse>> handler) {
            _defaultCommandRegistration = new AsyncCommandRegistration(
                DataExpectation.Auto,
                (client, command, dataLength, arguments, errorHandler) =>
                    new AsyncSingleCommandHandler(client, command, arguments, dataLength, false, handler, errorHandler)
            );
        }

        public void Error(Action<IRequest, Exception, Action<IResponse>> handler) {
            _errorHandler = handler;
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(
                dataExpectation, 
                (client, cmd, dataLength, arguments, errorHandler) =>
                    new AsyncSingleCommandHandler(client, cmd, arguments, dataLength, false, handler, errorHandler)
            );
        }

        public void AddCommand(string command, Action<IRequest, Action<IResponse, Action>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(
                dataExpectation,
                (connection, cmd, dataLength, arguments, errorHandler) =>
                    new AsyncMultiCommandHandler(connection, cmd, arguments, dataLength, handler, errorHandler)
            );
        }

        public void AddCommand(string command, Action<IRequest, Action<IEnumerable<IResponse>>> handler, DataExpectation dataExpectation) {
            _commands[command] = new AsyncCommandRegistration(
                dataExpectation,
                (connection, cmd, dataLength, arguments, errorHandler) =>
                    new SyncMultiCommandHandler(connection, cmd, arguments, dataLength, handler, errorHandler)
            );
        }
    }

}