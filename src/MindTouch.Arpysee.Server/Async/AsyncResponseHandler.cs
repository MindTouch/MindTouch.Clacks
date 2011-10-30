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

namespace MindTouch.Arpysee.Server.Async {
    public class AsyncResponseHandler {
        private const string TERMINATOR = "\r\n";

        private readonly Socket _socket;
        private readonly Action<string> _completion;
        private readonly Action<Exception> _error;

        public AsyncResponseHandler(Socket socket, Action<string> completion, Action<Exception> error) {
            _socket = socket;
            _completion = completion;
            _error = error;
        }

        public void SendResponse(IResponse response, Action callback) {
            var sb = new StringBuilder();
            sb.Append(response.Status);
            if(response.Arguments != null) {
                sb.Append(" ");
                sb.Append(string.Join(" ", response.Arguments));
            }
            if(response.Data != null) {
                sb.Append(" ");
                sb.Append(response.Data.Length);
            }
            sb.Append(TERMINATOR);
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            if(response.Data != null) {
                var d2 = new byte[data.Length + response.Data.Length + 2];
                data.CopyTo(d2, 0);
                Array.Copy(response.Data, 0, d2, data.Length, response.Data.Length);
                d2[d2.Length - 2] = (byte)'\r';
                d2[d2.Length - 1] = (byte)'\n';
                data = d2;
            }
            try {
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, r => {
                    try {
                        _socket.EndSend(r);
                        if(callback != null) {
                            callback();
                        } else {
                            _completion(response.Status);
                        }
                    } catch(Exception e) {
                        _error(e);
                    }
                }, null);
            } catch(Exception e) {
                _error(e);
            }
        }
    }

    public class NoResponseException : Exception { }
}