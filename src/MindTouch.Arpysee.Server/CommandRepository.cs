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
using System.Text;

namespace MindTouch.Arpysee.Server {
    public class CommandRepository : ICommandRegistry, ICommandDispatcher {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly Dictionary<string, ICommandHandlerFactory> _commands = new Dictionary<string, ICommandHandlerFactory>();
        private ICommandHandlerFactory _defaultCommandHandlerFactory;
        private Action<IRequest, Exception, Action<IResponse>> _errorHandler;

        public ICommandHandler GetHandler(string[] command) {
            ICommandHandlerFactory commandHandlerFactory;
            if(!_commands.TryGetValue(command[0], out commandHandlerFactory)) {
                commandHandlerFactory = _defaultCommandHandlerFactory;
            }
            return commandHandlerFactory.Handle(command);
        }

        public void HandleError(IRequest request, Exception ex, Action<IResponse> response) {
            try {
                if(_errorHandler != null) {
                    _errorHandler(request, ex, response);
                    return;
                }
            } catch(Exception e) {
                _log.Warn(string.Format("The error handler failed on exception of type {0}", ex.GetType()), e);
            }
            response(Response.WithStatus("ERROR").With(ex.Message).WithData(Encoding.ASCII.GetBytes(ex.StackTrace)));
        }

        public void Default(Action<IRequest, Action<IResponse>> handler) {
            _defaultCommandHandlerFactory = new CommandHandlerFactory(handler);
        }

        public void Error(Action<IRequest, Exception, Action<IResponse>> handler) {
            _errorHandler = handler;
        }

        public ICommandRegistration Command(string command, Action<IRequest, Action<IResponse>> handler) {
            var registration = new CommandHandlerFactory(handler);
            _commands[command] = registration;
            return registration;
        }
    }
}