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
    public class ResponseHandler {
        private const string TERMINATOR = "\r\n";
        private static readonly byte[] TERMINATOR_BYTES = Encoding.ASCII.GetBytes(TERMINATOR);

        public static void SendResponse(Socket socket, IResponse response, Action<Exception> callback) {
            var handler = new ResponseHandler(socket, response);
            handler.Exec(callback);
        }

        private readonly Socket _socket;
        private readonly IResponse _response;

        private ResponseHandler(Socket socket, IResponse response) {
            _socket = socket;
            _response = response;
        }

        private void Exec(Action<Exception> callback) {
            byte[] responseMessage = GetMessage(_response);
            AsyncCallback completion = _response.Payload == null
                ? (AsyncCallback)(r => callback(null))
                : (r => {
                    try {
                        _socket.BeginSend(_response.Payload, 0, _response.Payload.Length, SocketFlags.None, r2 => {
                            try {
                                _socket.BeginSend(TERMINATOR_BYTES, 0, TERMINATOR_BYTES.Length, SocketFlags.None, r3 => callback(null), null);
                            } catch(Exception e) {
                                callback(e);
                            }
                        }, null);
                    } catch(Exception e) {
                        callback(e);
                    }
                });
            try {
                _socket.BeginSend(responseMessage, 0, responseMessage.Length, SocketFlags.None, completion, null);
            } catch(Exception e) {
                callback(e);
            }
        }

        private byte[] GetMessage(IResponse response) {
            var sb = new StringBuilder();
            sb.Append(_response.Status);
            sb.Append(" ");
            if(_response.Arguments != null) {
                sb.Append(string.Join(" ", _response.Arguments));
            }
            if(_response.Payload != null) {
                sb.Append(" ");
                sb.Append(_response.Payload.Length);
            }
            sb.Append(TERMINATOR);
            return Encoding.ASCII.GetBytes(sb.ToString());
        }
    }
}