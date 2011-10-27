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

namespace MindTouch.Arpysee.Server {
    public class Response : IResponse {

        public static Response WithStatus(string status) {
            return new Response(status);
        }

        private readonly string _status;
        private string[] _args;
        private byte[] _data;

        private Response(string status) {
            _status = status;
        }

        public Response WithArguments(string[] args) {
            _args = args;
            return this;
        }

        public Response WithPayload(byte[] payload) {
            _data = payload;
            return this;
        }

        string IResponse.Status { get { return _status; } }
        string[] IResponse.Arguments { get { return _args; } }
        byte[] IResponse.Data { get { return _data; } }
    }
}