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

namespace MindTouch.Arpysee.Server.Sync {
    public class SyncFluentCommandRegistration : ISyncFluentCommandRegistration {
        private readonly ServerBuilder _serverBuilder;
        private readonly SyncCommandRepository _repository;
        private readonly string _command;
        private bool _isDisconnect;
        private DataExpectation _dataExpectation = DataExpectation.Auto;
        private Func<IRequest, IResponse> _handler;

        public SyncFluentCommandRegistration(ServerBuilder serverBuilder, SyncCommandRepository repository, string command) {
            _serverBuilder = serverBuilder;
            _repository = repository;
            _command = command;
        }

        public ISyncFluentCommandRegistration IsDisconnect() {
            _isDisconnect = true;
            return this;
        }

        public ISyncFluentCommandRegistration HandledBy(Func<IRequest, IResponse> handler) {
            _handler = handler;
            return this;
        }

        public ISyncFluentCommandRegistration HandledBy(Func<IRequest, IEnumerable<IResponse>> handler) {
            throw new NotImplementedException();
        }

        public ISyncFluentCommandRegistration ExpectsData() {
            _dataExpectation = DataExpectation.Always;
            return this;
        }

        public ISyncFluentCommandRegistration ExpectsNoData() {
            _dataExpectation = DataExpectation.Never;
            return this;
        }

        public ISyncServerBuilder Register() {
            if(_handler == null) {
                throw new CommandConfigurationException(string.Format("Must define a handler for command '{0}'", _command));
            }
            if(_isDisconnect) {
                _repository.Disconnect(_command, _handler);
            } else {
                _repository.AddCommand(_command, _handler, _dataExpectation);
            }
            return _serverBuilder;
        }
    }
}