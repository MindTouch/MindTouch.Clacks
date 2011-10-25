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

namespace MindTouch.Arpysee.Server {
    public class CommandRepository : ICommandRegistry, ICommandDispatcher {

        private readonly Dictionary<string, ICommandHandlerFactory> _commands = new Dictionary<string, ICommandHandlerFactory>();
        private ICommandHandlerFactory _defaultCommandHandlerFactory;

        public void RegisterDefault(Func<string[], ServerResponse> handler) {
            _defaultCommandHandlerFactory = new SimpleCommandHandlerFactory(handler);
        }

        public void Register(string command, Func<string[], ServerResponse> handler) {
            _commands[command] = new SimpleCommandHandlerFactory(handler);
        }

        public void Register(string command, Func<string[], Action<byte[]>, ServerResponse> handler) {
        }

        public ICommandHandler GetHandler(string[] command) {
            ICommandHandlerFactory commandHandlerFactory;
            if(!_commands.TryGetValue(command[0], out commandHandlerFactory)) {
                commandHandlerFactory = _defaultCommandHandlerFactory;
            }
            return commandHandlerFactory.Handle(command);
        }
    }
}