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

namespace MindTouch.Arpysee.Server.Async {
    public class AsyncResponseHandler {

        private readonly Socket _socket;
        private readonly Action<string> _completion;
        private readonly Action<Exception> _error;
        private string _lastStatus;

        public AsyncResponseHandler(Socket socket, Action<string> completion, Action<Exception> error) {
            _socket = socket;
            _completion = completion;
            _error = error;
        }

        public void SendResponse(IResponse response, Action callback) {

            // null response: the last callback discovere no more results, so we're done
            if(response == null) {
                _completion(_lastStatus);
                return;
            }
            try {
                var data = response.GetBytes();
                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, r => {
                    try {
                        _socket.EndSend(r);
                        if(callback != null) {
                            callback();
                        } else {

                            // null callback: the source already knows there won't be any more responses
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
}