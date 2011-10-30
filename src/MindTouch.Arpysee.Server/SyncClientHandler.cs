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
using System.Threading;

namespace MindTouch.Arpysee.Server {
    public class SyncClientHandler : AClientRequestHandler {

        private const string TERMINATOR = "\r\n";
        private static readonly Logger.ILog _log = Logger.CreateLog();

        public SyncClientHandler(Socket socket, ICommandDispatcher dispatcher, Action<IClientHandler> removeCallback)
            : base(socket, dispatcher, removeCallback) { }

        // 13/14.
        protected override void Receive(Action<int, int> continuation) {
            int received;
            try {
                _bufferPosition = 0;
                received = _socket.Receive(_buffer, 0, _buffer.Length, SocketFlags.None);
                if(received == 0) {
                    Dispose();
                    return;
                }
            } catch(SocketException e) {
                _log.Warn("Receive failed", e);
                Dispose();
                return;
            } catch(ObjectDisposedException) {
                _log.Debug("socket was already disposed");
                return;
            }
            continuation(0, received);
        }

        protected override void ProcessResponse() {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            string finalStatus = null;
            //foreach(var response in _handler.GetResponse()) {
            var response = _handler.GetSingleResponse(); 
                finalStatus = response.Status;
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
                    _socket.Send(data);
                } catch(SocketException e) {
                    _log.Warn("Send failed", e);
                    Dispose();
                    return;
                } catch(ObjectDisposedException) {
                    _log.Debug("socket was already disposed");
                    return;
                }
            //}
            ThreadPool.QueueUserWorkItem((o) => EndCommandRequest(finalStatus));
        }
    }
}
