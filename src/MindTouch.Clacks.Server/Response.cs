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

using System.Collections.Generic;

namespace MindTouch.Clacks.Server {
    public class Response : IResponse {

        public static Response Create(string status) {
            return new Response(status);
        }

        private readonly string _status;
        private readonly List<string> _args = new List<string>();
        private byte[] _data;

        private Response(string status) {
            _status = status;
        }

        public Response WithArguments(string[] args) {
            _args.AddRange(args);
            return this;
        }

        public Response WithArgument<T>(T arg) {
            _args.Add(arg.ToString());
            return this;
        }

        public Response WithData(byte[] payload) {
            _data = payload;
            return this;
        }

        string IResponse.Status { get { return _status; } }
        IEnumerable<string> IResponse.Arguments { get { return _args; } }
        byte[] IResponse.Data { get { return _data; } }
    }
}