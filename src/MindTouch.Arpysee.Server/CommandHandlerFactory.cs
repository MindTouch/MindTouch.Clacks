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
    public class CommandHandlerFactory : ICommandHandlerFactory, ICommandRegistration {
        private DataExpectation _dataExpectation = DataExpectation.Auto;
        private readonly Action<IRequest, Action<IResponse>> _handler;

        public CommandHandlerFactory(Action<IRequest, Action<IResponse>> handler) {
            _handler = handler;
        }

        public ICommandHandler Handle(string[] commandArgs) {
            var command = commandArgs[0];
            int dataLength = 0;
            string[] arguments;
            if(_dataExpectation == DataExpectation.Auto) {
                if(commandArgs.Length > 1) {
                    int.TryParse(commandArgs[commandArgs.Length - 1], out dataLength);
                }
            } else if(_dataExpectation == DataExpectation.Always) {
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
            return new CommandHandler(command, arguments, dataLength, _handler);
        }


        ICommandRegistration ICommandRegistration.ExpectData() {
            _dataExpectation = DataExpectation.Always;
            return this;
        }

        ICommandRegistration ICommandRegistration.ExpectNoData() {
            _dataExpectation = DataExpectation.Never;
            return this;
        }

    }

    public class InvalidCommandException : Exception { }
}