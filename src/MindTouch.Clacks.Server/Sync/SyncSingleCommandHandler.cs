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
using System.Net;

namespace MindTouch.Clacks.Server.Sync {
    public class SyncSingleCommandHandler : ISyncCommandHandler {

        // TODO: should this receive client id and client ip as well?
        public static ISyncCommandHandler DisconnectHandler(string command, Func<IRequest, IResponse> handler) {
            return new SyncSingleCommandHandler(command, handler);
        }

        private readonly Connection _connection;
        private readonly string _command;
        private readonly string[] _arguments;
        private readonly Func<IRequest, IResponse> _handler;
        private readonly Func<IRequest, Exception, IResponse> _errorHandler;
        private readonly int _dataLength;
        private readonly bool _disconnect;
        private int _received;
        private List<byte[]> _dataChunks;

        public SyncSingleCommandHandler(Connection connection, string command, string[] arguments, int dataLength, Func<IRequest, IResponse> handler, Func<IRequest, Exception, IResponse> errorHandler) {
            _connection = connection;
            _command = command;
            _arguments = arguments;
            _dataLength = dataLength;
            _handler = handler;
            _errorHandler = errorHandler;
        }

        private SyncSingleCommandHandler(string command, Func<IRequest, IResponse> handler) {
            _command = command;
            _handler = handler;
            _disconnect = true;
        }

        public bool ExpectsData { get { return _dataLength > 0; } }
        public bool DisconnectOnCompletion { get { return _disconnect; } }
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
            IResponse response;
            try {
                response = _handler(request);
            } catch(Exception e) {
                response = _errorHandler(request, e);
            }
            return new[] { response };
        }

        public void Dispose() { }
    }
}