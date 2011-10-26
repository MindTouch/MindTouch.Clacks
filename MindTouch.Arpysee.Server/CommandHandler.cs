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
    public class CommandHandler : ICommandHandler {

        private readonly string[] _command;
        private readonly bool _expectData;
        private readonly Func<IRequest, IResponse> _handler;
        private readonly int _dataLength;
        private int _received;
        private readonly List<byte[]> _dataChunks;

        public CommandHandler(string[] command, bool expectData, Func<IRequest, IResponse> handler) {
            _command = command;
            _expectData = expectData;
            _handler = handler;
            if(_expectData) {
                _dataLength = int.Parse(_command[_command.Length - 1]);
                _dataChunks = new List<byte[]>();
            }
        }

        public void Dispose() { }

        public bool ExpectsData { get { return _expectData; } }

        public bool AddData(byte[] chunk) {
            _dataChunks.Add(chunk);
            _received += chunk.Length;
            if(_received > _dataLength) {
                throw new InvalidOperationException("too much data");
            }
            return _received == _dataLength;
        }

        public IResponse GetResponse() {
            return _handler(new ArpyseeRequest(_command, _dataLength, _dataChunks));
        }
    }
}