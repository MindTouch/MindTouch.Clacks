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
using System.Collections.Generic;
using System.Net;

namespace MindTouch.Clacks.Server {
    public class Request : IRequest {
        private readonly IPEndPoint _client;
        private readonly string _command;
        private readonly string[] _arguments;
        private readonly byte[] _data;

        public Request(string[] command) {
            _command = command[0];
        }

        public Request(IPEndPoint client, string command, string[] arguments, int dataLength, IEnumerable<byte[]> data) {
            _client = client;
            _command = command;
            _arguments = arguments;
            if(dataLength <= 0)
                return;
            _data = new byte[dataLength];
            var idx = 0;
            foreach(var chunk in data) {
                Array.Copy(chunk, 0, _data, idx, chunk.Length);
                idx += chunk.Length;
            }
        }

        public IPEndPoint Client { get { return _client; } }
        public string Command { get { return _command; } }
        public string[] Arguments { get { return _arguments; } }
        public byte[] Data { get { return _data; } }
    }
}