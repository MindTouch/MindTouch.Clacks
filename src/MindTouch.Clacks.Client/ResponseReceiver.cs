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
using System.Text;
using MindTouch.Clacks.Client.Net;

namespace MindTouch.Clacks.Client {
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

        public void Reset(IRequestInfo requestInfo) {
            _requestInfo = requestInfo;
            _carriageReturn = false;
            _statusBuffer.Length = 0;
            _bufferPosition = 0;
            _bufferDataLength = 0;
            _responseDataPosition = 0;
            _currentResponse = new Response(new[] { "ERROR", "INCOMPLETE" });
        }

        private void Receive() {
            _bufferPosition = 0;
            _bufferDataLength = _socket.Receive(_buffer, 0, _buffer.Length);
        }

        protected void InitializeHandler(string[] command) {

        }

        public Response GetResponse() {
            if(_bufferDataLength == 0) {
                Receive();
            }
            ProcessCommandData();
            return _currentResponse;
        }

        private void ProcessCommandData() {
            while(true) {

                // look for \r\n
                for(var i = 0; i < _bufferDataLength; i++) {
                    var idx = _bufferPosition + i;

                    if(_buffer[idx] == '\r') {
                        _carriageReturn = true;
                    } else if(_carriageReturn) {
                        if(_buffer[idx] != '\n') {

                            // it wasn't a \r followed by an \n
                            var receivedCommandData = Encoding.ASCII.GetString(_buffer, _bufferPosition, i - 1);
                            throw new IncompleteCommandTerminator(receivedCommandData);
                        }
                        _carriageReturn = false;
                        var consumeLength = i - 1;
                        if(consumeLength > 0) {

                            // it is possible that this buffer pass is only the terminator or part of the terminator, so only
                            // read from it if there are non-terminator characters
                            _statusBuffer.Append(Encoding.ASCII.GetString(_buffer, _bufferPosition, consumeLength));
                        }
                        var status = _statusBuffer.ToString().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        _statusBuffer.Length = 0;
                        _currentResponse = new Response(status);
                        _responseExpectedBytes = _requestInfo.ExpectedBytes(_currentResponse);
                        _bufferPosition += (i + 1);
                        _bufferDataLength -= (i + 1);
                        if(_responseExpectedBytes >= 0) {
                            if(_bufferDataLength == 0) {
                                Receive();
                            }
                            _responseDataPosition = 0;
                            _responseData = new byte[_responseExpectedBytes];
                            ProcessPayloadData();
                        }

                        // at this point _currentResponse should be fully populated
                        return;
                    }
                }
                var length = _bufferDataLength;
                if(_carriageReturn) {

                    // we've found the \r, but the \n wasn't part of the buffer, so consume all but the last char from buffer
                    // since we never want to read the terminator into the status buffer
                    length--;
                    if(length < 0) {
                        return;
                    }
                }
                _statusBuffer.Append(Encoding.ASCII.GetString(_buffer, _bufferPosition, length));
                Receive();
            }
        }

        private void ProcessPayloadData() {
            while(true) {
                var dataLength = Math.Min(_bufferDataLength, _responseExpectedBytes);
                Array.Copy(_buffer, _bufferPosition, _responseData, _responseDataPosition, dataLength);
                _responseDataPosition += dataLength;
                _responseExpectedBytes -= dataLength;
                _bufferPosition += dataLength;
                _bufferDataLength -= dataLength;
                if(_responseExpectedBytes == 0) {
                    ProcessPayloadTermination();
                    return;
                }
                Receive();
            }
        }

        private void ProcessPayloadTermination() {

            // check for trailing \r\n
            while(true) {
                for(var i = 0; i < _bufferDataLength; i++) {
                    var idx = _bufferPosition + i;
                    if(!_carriageReturn && _buffer[idx] == '\r') {
                        _carriageReturn = true;
                        continue;
                    }
                    if(_carriageReturn && _buffer[idx] == '\n') {
                        _carriageReturn = false;
                        _bufferPosition += (i + 1);
                        _bufferDataLength -= (i + 1);
                        _currentResponse.Data = _responseData;
                        return;
                    }

                    // at this point there may only be \r\n 
                    throw new DataTerminatorMissingException();
                }
                Receive();
            }
        }
    }
}
