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
using MindTouch.Arpysee.Client.Net;

namespace MindTouch.Arpysee.Client {
    public class ResponseReceiver {
        private readonly ISocket _socket;
        private readonly StringBuilder _statusBuffer = new StringBuilder();
        private readonly byte[] _buffer = new byte[16 * 1024];
        private IRequestInfo _requestInfo;
        private int _bufferPosition;
        private int _bufferDataLength;
        private bool _carriageReturn;
        private Response _currentResponse;
        private byte[] _responseData;
        private int _responseExpectedBytes = 0;
        private int _responseDataPosition;

        public ResponseReceiver(ISocket socket) {
            _socket = socket;
        }

        private class Read {
            public readonly int Position;
            public readonly int Length;
            public Read(int position, int length) {
                Position = position;
                Length = length;
            }
        }

        public void Reset(IRequestInfo requestInfo) {
            _requestInfo = requestInfo;
            _carriageReturn = false;
            _statusBuffer.Length = 0;
            _bufferPosition = 0;
            _bufferDataLength = 0;
            _responseDataPosition = 0;
        }

        private Read Receive() {
            _bufferPosition = 0;
            var received = _socket.Receive(_buffer, 0, _buffer.Length);
            return received == 0 ? null : new Read(0, received);
        }

        protected void InitializeHandler(string[] command) {

        }

        public Response GetResponse() {
            ProcessCommandData(_bufferPosition != 0 ? new Read(_bufferPosition, _bufferDataLength) : Receive());
            return _currentResponse;
        }

        private void ProcessCommandData(Read read) {

            // look for \r\n
            for(var i = 0; i < read.Length; i++) {
                var idx = read.Position + i;
                if(_buffer[idx] == '\r') {
                    _carriageReturn = true;
                } else if(_carriageReturn) {
                    if(_buffer[idx] != '\n') {

                        // it wasn't a \r followed by an \n
                        throw new IncompleteCommandTerminator();
                    }
                    _carriageReturn = false;
                    _bufferPosition = idx + 1;
                    _bufferDataLength = read.Length - i - 1;
                    var consumeLength = i - 1;
                    if(consumeLength > 0) {

                        // it is possible that this buffer pass is only the terminator or part of the terminator, so only
                        // read from it if there are non-terminator characters
                        _statusBuffer.Append(Encoding.ASCII.GetString(_buffer, read.Position, consumeLength));
                    }
                    // 6.
                    var status = _statusBuffer.ToString().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    _statusBuffer.Length = 0;
                    _currentResponse = new Response(status);
                    _responseExpectedBytes = _requestInfo.ExpectedBytes(_currentResponse);
                    if(_responseExpectedBytes >= 0) {
                        _responseDataPosition = 0;
                        _responseData = new byte[_responseExpectedBytes];
                        ProcessPayloadData(_bufferPosition != 0 ? new Read(_bufferPosition, _bufferDataLength) : Receive());
                    }

                    // at this point _currentResponse should be fully populated
                    return;
                }
            }
            var length = read.Length;
            if(_carriageReturn) {

                // we've found the \r, but the \n wasn't part of the buffer, so consume all but the last char from buffer
                length--;
            }
            _statusBuffer.Append(Encoding.ASCII.GetString(_buffer, read.Position, length));
            ProcessCommandData(Receive());
        }

        private void ProcessPayloadData(Read read) {
            if(_responseExpectedBytes > 0) {
                var dataLength = Math.Min(read.Length, _responseExpectedBytes);
                Array.Copy(_buffer, read.Position, _responseData, _responseDataPosition, dataLength);
                _responseDataPosition += dataLength;
                _responseExpectedBytes -= dataLength;
                read = new Read(read.Position + dataLength, read.Length - dataLength);
            }

            // check for trailing \r\n
            if(_responseExpectedBytes <= 0) {
                var responseCompleted = false;
                for(var i = 0; i < read.Length; i++) {
                    var idx = read.Position + i;
                    if(_buffer[idx] == '\r') {
                        _carriageReturn = true;
                    } else if(_carriageReturn && _buffer[idx] == '\n') {
                        _carriageReturn = false;
                        _bufferPosition = idx + 1;
                        _bufferDataLength = read.Length - i - 1;
                        responseCompleted = true;
                        break;
                    }
                    _responseExpectedBytes++;
                    if(_responseExpectedBytes < -2) {
                        throw new DataTerminatorMissingException();
                    }
                }
                if(responseCompleted) {
                    _currentResponse.Data = _responseData;
                    return;
                }
            }
            ProcessPayloadData(Receive());
        }
    }
}
