/*
 * MindTouch.Clacks
 * 
 * Copyright (C) 2011 Arne F. Claassen
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

namespace MindTouch.Clacks.Server {
    public abstract class CommandRegistration<THandler,TError> {
        private readonly DataExpectation _dataExpectation;
        private readonly CommandHandlerBuilder<THandler, TError> _builder;

        protected CommandRegistration(DataExpectation dataExpectation, CommandHandlerBuilder<THandler, TError> builder) {
            _dataExpectation = dataExpectation;
            _builder = builder;
        }

        public DataExpectation DataExpectation { get { return _dataExpectation; } }

        public THandler GetHandler(string[] commandArgs, TError errorHandler) {
            var command = commandArgs[0];
            var dataLength = 0;
            switch(_dataExpectation) {
            case DataExpectation.Auto:
                if(commandArgs.Length > 1) {
                    int.TryParse(commandArgs[commandArgs.Length - 1], out dataLength);
                }
                break;
            case DataExpectation.Always:
                if(commandArgs.Length == 1 || !int.TryParse(commandArgs[commandArgs.Length - 1], out dataLength)) {
                    throw new InvalidCommandException();
                }
                break;
            }
            var arguments = new string[commandArgs.Length - 1];
            if(arguments.Length > 0) {
                Array.Copy(commandArgs, 1, arguments, 0, arguments.Length);
            }
            return _builder(command, dataLength, arguments, errorHandler);
        }
    }
}