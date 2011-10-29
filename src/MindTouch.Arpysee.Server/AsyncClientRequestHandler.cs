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
    public class AsyncClientRequestHandler : AClientRequestHandler {

        public AsyncClientRequestHandler(Socket socket, ICommandDispatcher dispatcher) : base(socket, dispatcher) {}

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

        // 2.
        protected override void Receive(Action<int,int> continuation) {
            try {
                _bufferPosition = 0;
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, r => {

                    // 3.
                    try {
                        var received = _socket.EndReceive(r);
                        if(received == 0) {
                            Dispose();
                            return;
                        }
                        continuation(0, received);
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

        // 13/14.
        protected override void ProcessResponse() {
            _handler.GetResponse(response => AsyncResponseHandler.SendResponse(_socket, response, e => {
                if (e != null) {
                    Dispose();
                    return;
                }
                EndCommandRequest(response.Status);
            }));
        }
    }
}
