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
using System.Collections.Generic;

namespace MindTouch.Arpysee.Server {
    public class ArpyseeResponse : IResponse {

        public static ArpyseeResponse WithStatus(string status) {
            return new ArpyseeResponse(status);
        }

        private readonly string _status;
        private string[] _args;
        private byte[] _data;

        private ArpyseeResponse(string status) {
            _status = status;
        }

        public ArpyseeResponse WithArguments(string[] args) {
            _args = args;
            return this;
        }

        public ArpyseeResponse WithPayload(byte[] payload) {
            _data = payload;
            return this;
        }

        string IResponse.Status { get { return _status; } }
        string[] IResponse.Arguments { get { return _args; } }
        byte[] IResponse.Data { get { return _data; } }
    }

    public interface IRequest {
        string Command { get; }
        string[] Arguments { get; }
        byte[] Data { get; }

    }

    public class ArpyseeRequest : IRequest {

        private readonly string _command;
        private readonly string[] _arguments;
        private readonly byte[] _data;

        public ArpyseeRequest(string[] command) {
            _command = command[0];
        }

        public ArpyseeRequest(string[] command, int dataLength, IEnumerable<byte[]> data) {
            _command = command[0];
            if(dataLength == 0) {
                _arguments = new string[command.Length - 1];
                Array.Copy(command, 1, Arguments, 0, Arguments.Length);
            } else {
                _arguments = new string[command.Length - 2];
                Array.Copy(command, 1, Arguments, 0, Arguments.Length - 1);
                _data = new byte[dataLength];
                var idx = 0;
                foreach(var chunk in data) {
                    Array.Copy(chunk, 0, _data, idx, chunk.Length);
                    idx += chunk.Length;
                }
            }
        }

        public string Command { get { return _command; } }
        public string[] Arguments { get { return _arguments; } }
        public byte[] Data { get { return _data; } }
    }

}