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
using System.Text;

namespace MindTouch.Arpysee.Server {
    public class CommandRegistration : ICommandRegistration {

        private static readonly Logger.ILog _log = Logger.CreateLog();
        private Action<IRequest, Exception, Action<IResponse>> _errorHandler;

        public CommandRegistration() {
            DataExpectation = DataExpectation.Auto;
        }

        public DataExpectation DataExpectation { get; set; }
        public Action<IRequest, Action<IResponse>> Handler { get; private set; }
        public Func<IRequest, IResponse> SyncHandler { get; private set; }
        public CommandRegistration(Action<IRequest, Action<IResponse>> handler) {
            Handler = handler;
        }
        public CommandRegistration(Func<IRequest, IResponse> handler) {
            SyncHandler = handler;
        }

        ICommandRegistration ICommandRegistration.ExpectData() {
            DataExpectation = DataExpectation.Always;
            return this;
        }

        ICommandRegistration ICommandRegistration.ExpectNoData() {
            DataExpectation = DataExpectation.Never;
            return this;
        }

        public ICommandHandler GetHandler(string command, string[] arguments, int dataLength, Action<IRequest, Exception, Action<IResponse>> errorHandler) {
            _errorHandler = errorHandler;
            return SyncHandler == null
                       ? (ICommandHandler)new CommandHandler(command, arguments, dataLength, WrappedHandler)
                       : new SyncCommandHandler(command, arguments, dataLength, SyncHandler);
        }

        private void WrappedHandler(IRequest request, Action<IResponse> response) {
            try {
                Handler(request, response);
            } catch(Exception handlerException) {
                try {

                    if(_errorHandler != null) {
                        _errorHandler(request, handlerException, response);
                        return;
                    }
                } catch(Exception errorHandlerException) {
                    _log.Warn(string.Format("The error handler failed on exception of type {0}", handlerException.GetType()), errorHandlerException);
                }
                response(Response.Create("ERROR").WithArgument(handlerException.Message).WithData(Encoding.ASCII.GetBytes(handlerException.StackTrace)));
            }
        }
    }
}