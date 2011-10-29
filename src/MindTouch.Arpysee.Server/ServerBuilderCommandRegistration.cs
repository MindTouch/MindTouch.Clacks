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

namespace MindTouch.Arpysee.Server {
    public class ServerBuilderCommandRegistration : IServerBuilderCommandRegistration {
        private readonly ServerBuilder _serverBuilder;
        private readonly CommandRepository _repository;
        private readonly string _command;
        private bool _isDisconnect;
        private bool? _expectsData = null;
        private Action<IRequest, Action<IResponse>> _handler;

        public ServerBuilderCommandRegistration(ServerBuilder serverBuilder, CommandRepository repository, string command) {
            _serverBuilder = serverBuilder;
            _repository = repository;
            _command = command;
        }

        public IServerBuilderCommandRegistration IsDisconnect() {
            _isDisconnect = true;
            return this;
        }

        public IServerBuilderCommandRegistration HandledBy(Action<IRequest, Action<IResponse>> handler) {
            _handler = handler;
            return this;
        }

        public IServerBuilderCommandRegistration ExpectsData() {
            _expectsData = true;
            return this;
        }

        public IServerBuilderCommandRegistration ExpectsNoData() {
            _expectsData = false;
            return this;
        }

        public IServerBuilder Then() {
            if(_handler == null) {
                throw new CommandConfigurationException(string.Format("Must define a handler for command '{0}'", _command));
            }
            if(_isDisconnect) {
                _repository.Disconnect(_command, _handler);
            } else {
                var registration = _repository.Command(_command, _handler);
                if(_expectsData.HasValue) {
                    if(_expectsData.Value) {
                        registration.ExpectData();
                    } else {
                        registration.ExpectNoData();
                    }
                }
            }
            return _serverBuilder;
        }
    }
}