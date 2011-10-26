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
using System.Net.Sockets;
using System.Text;

namespace MindTouch.Arpysee.Server {
    public class ArpyseeAsyncClientHandler : IDisposable {

        private readonly Socket _socket;
        private readonly ICommandDispatcher _dispatcher;
        private readonly StringBuilder _commandBuffer = new StringBuilder();
        private readonly byte[] _buffer = new byte[16 * 1024];
        private int _bufferPosition;
        private int _bufferDataLength;
        private bool _carriageReturn;
        private ICommandHandler _handler;

        public ArpyseeAsyncClientHandler(Socket socket, ICommandDispatcher dispatcher) {
            _socket = socket;
            _dispatcher = dispatcher;
            GetCommandData();
        }

        /* WorkFlow
         * 1. If data in buffer go to 4.
         * 2. Wait for command data
         * 3. Receive command data
         * 4. Build command from data
         * 5. If not enough data for command, go to 2.
         * 6. Get command handler
         * 7. If command doesn't expect data go to 13.
         * 8. If data in buffer go to 11.
         * 9. Wait for payload data
         * 10. Receive payload data
         * 11. Give data to handler
         * 12. If less payload than expected go to 9.
         * 13. End command request phase
         * 14. Get response data from handler
         * 15. If no response data go to 1.
         * 16. Begin send response data
         * 17. Complete send response data
         * 18. go to 14.
         */

        // 1.
        private void GetCommandData() {
            if(_bufferPosition != 0) {
                ProcessCommandData(_bufferPosition, _bufferDataLength);
            }
            ReceiveCommandData();
        }

        // 2.
        private void ReceiveCommandData() {
            try {
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, r => {

                    // 3.
                    try {
                        var received = _socket.EndReceive(r);
                        if(received == 0) {
                            Dispose();
                            return;
                        }
                        ProcessCommandData(0, received);
                    } catch(SocketException) {
                        Dispose();
                    } catch(ObjectDisposedException) {
                        return;
                    }
                }, null);
            } catch(SocketException) {
                Dispose();
            } catch(ObjectDisposedException) {
                return;
            }
        }

        // 4.
        private void ProcessCommandData(int position, int length) {

            // look for \r\n
            for(var i = 0; i < length; i++) {
                var idx = position + i;
                if(_buffer[idx] == '\r') {
                    _carriageReturn = true;
                } else if(_carriageReturn && _buffer[idx] == '\n') {
                    _carriageReturn = false;
                    _bufferPosition = idx + 1;
                    _bufferDataLength = length - i - 1;
                    _commandBuffer.Append(Encoding.ASCII.GetString(_buffer, position, idx - 1));

                    // 6.
                    var command = _commandBuffer.ToString().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    _commandBuffer.Length = 0;
                    _handler = _dispatcher.GetHandler(command);
                    if(_handler.ExpectsData) {
                        GetPayloadData();
                    } else {

                        // 7.
                        ProcessResponse();
                    }
                    return;
                }
            }
            _commandBuffer.Append(Encoding.ASCII.GetString(_buffer, position, length));

            // 5.
            ReceiveCommandData();
        }

        // 8.
        private void GetPayloadData() {
            if(_bufferPosition != 0) {
                ProcessPayloadData(_bufferPosition, _bufferDataLength);
            }
            ReceivePayloadData();
        }

        // 9.
        private void ReceivePayloadData() {
            try {
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, r => {

                    // 10.
                    try {
                        var received = _socket.EndReceive(r);
                        if(received == 0) {
                            Dispose();
                            return;
                        }
                        ProcessPayloadData(0, received);
                    } catch(SocketException) {
                        Dispose();
                    } catch(ObjectDisposedException) {
                        return;
                    }
                }, null);
            } catch(SocketException) {
                Dispose();
            } catch(ObjectDisposedException) {
                return;
            }
        }

        // 11.
        private void ProcessPayloadData(int position, int length) {
            var payload = new byte[length];
            Array.Copy(_buffer, position, payload, 0, length);
            if(_handler.AddData(payload)) {
                ReceivePayloadData();
            } else {
                ProcessResponse();
            }
        }

        // 13/14.
        private void ProcessResponse() {
            var response = _handler.GetResponse();
            AsyncResponseHandler.SendResponse(_socket, response, e => {
                if(e != null) {
                    Dispose();
                    return;
                }
                GetCommandData();
            });
        }

        public void Dispose() {
            _socket.Dispose();
        }
    }
}
