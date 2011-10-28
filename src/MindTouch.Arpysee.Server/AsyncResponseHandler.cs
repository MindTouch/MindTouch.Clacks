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
    public class AsyncResponseHandler {
        private const string TERMINATOR = "\r\n";

        public static void SendResponse(Socket socket, IResponse response, Action<Exception> callback) {
            var handler = new AsyncResponseHandler(socket, response);
            handler.Exec(callback);
        }

        private readonly Socket _socket;
        private readonly IResponse _response;

        private AsyncResponseHandler(Socket socket, IResponse response) {
            _socket = socket;
            _response = response;
        }

        private void Exec(Action<Exception> callback) {
            var sb = new StringBuilder();
            sb.Append(_response.Status);
            if(_response.Arguments != null) {
                sb.Append(" ");
                sb.Append(string.Join(" ", _response.Arguments));
            }
            if(_response.Data != null) {
                sb.Append(" ");
                sb.Append(_response.Data.Length);
            }
            sb.Append(TERMINATOR);
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            if(_response.Data != null) {
                var d2 = new byte[data.Length + _response.Data.Length + 2];
                data.CopyTo(d2, 0);
                Array.Copy(_response.Data, 0, d2, data.Length, _response.Data.Length);
                d2[d2.Length - 2] = (byte)'\r';
                d2[d2.Length - 1] = (byte)'\n';
                data = d2;
            }
            try {
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, r => callback(null), null);
            } catch(Exception e) {
                callback(e);
            }
        }
    }
}