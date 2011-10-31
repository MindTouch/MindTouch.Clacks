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

namespace MindTouch.Arpysee.Server.Async {
    public class AsyncFluentCommandRegistration : IAsyncFluentCommandRegistration {
        private readonly ServerBuilder _serverBuilder;
        private readonly AsyncCommandRepository _repository;
        private readonly string _command;
        private bool _isDisconnect;
        private DataExpectation _dataExpectation = DataExpectation.Auto;
        private Action<IRequest, Action<IResponse>> _singleResponseHandler;
        private Action<IRequest, Action<IResponse, Action>> _multiResponseHandler;

        public AsyncFluentCommandRegistration(ServerBuilder serverBuilder, AsyncCommandRepository repository, string command) {
            _serverBuilder = serverBuilder;
            _repository = repository;
            _command = command;
        }

        public IAsyncFluentCommandRegistration IsDisconnect() {
            _isDisconnect = true;
            return this;
        }

        public IAsyncFluentCommandRegistration HandledBy(Action<IRequest, Action<IResponse>> handler) {
            _singleResponseHandler = handler;
            return this;
        }

        public IAsyncFluentCommandRegistration HandledBy(Action<IRequest, Action<IResponse, Action>> handler) {
            _multiResponseHandler = handler;
            return this;
        }

        public IAsyncFluentCommandRegistration ExpectsData() {
            _dataExpectation = DataExpectation.Always;
            return this;
        }

        public IAsyncFluentCommandRegistration ExpectsNoData() {
            _dataExpectation = DataExpectation.Never;
            return this;
        }

        public IAsyncServerBuilder Register() {
            if(_singleResponseHandler == null && _multiResponseHandler == null) {
                throw new CommandConfigurationException(string.Format("Must define a handler for command '{0}'", _command));
            }
            if(_isDisconnect) {
                _repository.Disconnect(_command, _singleResponseHandler);
            } else if(_singleResponseHandler != null) {
                _repository.AddCommand(_command, _singleResponseHandler, _dataExpectation);
            } else {
                _repository.AddCommand(_command, _multiResponseHandler, _dataExpectation);
            }
            return _serverBuilder;
        }
    }
}