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
using System.Linq;
using System.Text;

namespace MindTouch.Arpysee.Client {
    public abstract class ARequest {

        protected const string TERMINATOR = "\r\n";
        private readonly string _command;
        private readonly List<string> _arguments = new List<string>();
        protected readonly HashSet<string> _expectsData = new HashSet<string>();
        private byte[] _data;

        protected ARequest(string command) {
            _command = command;
        }

        public string Command { get { return _command; } }

        protected void InternalWithArgument<T>(T arg) {
            _arguments.Add(arg.ToString());
        }

        protected void InternalWithData(byte[] data) {
            _data = data;
        }

        public void InternalExpectData(string status) {
            _expectsData.Add(status);
        }

        protected int InternalExpectedBytes(Response response) {
            if(!_expectsData.Contains(response.Status)) {
                return -1;
            }
            return int.Parse(response.Arguments[response.Arguments.Length - 1]);
        }

        protected byte[] GetBytes() {
            var sb = new StringBuilder();
            sb.Append(_command);
            if(_arguments.Any()) {
                sb.Append(" ");
                sb.Append(string.Join(" ", _arguments.ToArray()));
            }
            if(_data != null) {
                sb.Append(" ");
                sb.Append(_data.Length);
            }
            sb.Append(TERMINATOR);
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            if(_data != null) {
                var d2 = new byte[data.Length + _data.Length + 2];
                data.CopyTo(d2, 0);
                Array.Copy(_data, 0, d2, data.Length, _data.Length);
                d2[d2.Length - 2] = (byte)'\r';
                d2[d2.Length - 1] = (byte)'\n';
                data = d2;
            }
            return data;
        }
    }
}