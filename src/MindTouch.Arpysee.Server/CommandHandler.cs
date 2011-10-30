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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace MindTouch.Arpysee.Server {
    public class CommandHandler : ICommandHandler {

        public static ICommandHandler DisconnectHandler(string command, Action<IRequest, Action<IResponse>> handler) {
            return new CommandHandler(command, handler);
        }

        private readonly string _command;
        private readonly string[] _arguments;
        private readonly Action<IRequest, Action<IResponse>> _handler;
        private readonly int _dataLength;
        private readonly bool _disconnect;
        private int _received;
        private List<byte[]> _dataChunks;

        public CommandHandler(string command, string[] arguments, int dataLength, Action<IRequest, Action<IResponse>> handler) {
            _command = command;
            _arguments = arguments;
            _dataLength = dataLength;
            _handler = handler;
        }

        private CommandHandler(string command, Action<IRequest, Action<IResponse>> handler) {
            _command = command;
            _handler = handler;
            _disconnect = true;
        }

        public void Dispose() { }

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
            return new SingleResponseEnumerable(new Request(_command, _arguments, _dataLength, _dataChunks), _handler);
        }
    }

    public class SingleResponseEnumerable : IEnumerable<IResponse> {
        private readonly SingleResponseEnumerator _enumerator;

        public SingleResponseEnumerable(Request request, Action<IRequest, Action<IResponse>> handler) {
            _enumerator = new SingleResponseEnumerator(request, handler);
        }

        public IEnumerator<IResponse> GetEnumerator() {
            return _enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public class SingleResponseEnumerator : IEnumerator<IResponse> {
        private readonly Request _request;
        private readonly Action<IRequest, Action<IResponse>> _handler;
        private IResponse _response = null;
        private ManualResetEvent _signal = new ManualResetEvent(false);

        public SingleResponseEnumerator(Request request, Action<IRequest, Action<IResponse>> handler) {
            _request = request;
            _handler = handler;
        }

        public void Dispose() { }

        public bool MoveNext() {
            if(_response != null) {
                return false;
            }
            _handler(_request, response => {
                _response = response;
                _signal.Set();
            });
            return true;
        }

        public void Reset() {
            throw new NotSupportedException();
        }

        public IResponse Current {
            get {
                if(_response == null) {
                    _signal.WaitOne();
                }
                return _response;
            }
        }

        object IEnumerator.Current {
            get { return Current; }
        }
    }
}