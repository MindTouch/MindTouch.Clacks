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

namespace MindTouch.Clacks.Server.Sync {
    public class SyncMultiCommandHandler : ISyncCommandHandler {
        private readonly Connection _connection;
        private readonly string _command;
        private readonly string[] _arguments;
        private readonly Func<IRequest, IEnumerable<IResponse>> _handler;
        private readonly Func<IRequest, Exception, IResponse> _errorHandler;
        private readonly int _dataLength;
        private int _received;
        private List<byte[]> _dataChunks;

        public SyncMultiCommandHandler(Connection connection, string command, string[] arguments, int dataLength, Func<IRequest, IEnumerable<IResponse>> handler, Func<IRequest, Exception, IResponse> errorHandler) {
            _connection = connection;
            _command = command;
            _arguments = arguments;
            _dataLength = dataLength;
            _handler = handler;
            _errorHandler = errorHandler;
        }

        public bool ExpectsData { get { return _dataLength > 0; } }
        public int OutstandingBytes { get { return _dataLength - _received; } }
        public string Command { get { return _command; } }

        public void AcceptData(byte[] chunk) {
            if(_dataChunks == null) {
                _dataChunks = new List<byte[]>();
            }
            _dataChunks.Add(chunk);
            _received += chunk.Length;
            if(_received > _dataLength) {
                throw new DataExpectationException(true);
            }
        }

        public IEnumerable<IResponse> GetResponse() {
            if(_received < _dataLength) {
                throw new DataExpectationException(false);
            }
            var request = new Request(_connection, _command, _arguments, _dataLength, _dataChunks);
            IEnumerable<IResponse> responses;
            try {
                responses = _handler(request);
            } catch(Exception e) {
                responses = new[] {_errorHandler(request, e) };
            }
            return responses;
        }

        public void Dispose() { }
    }
}