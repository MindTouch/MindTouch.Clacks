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
using System.IO;

namespace MindTouch.Arpysee.Client {
    public class Response {
        public readonly string Status;
        public readonly string[] Arguments;
        private readonly long _dataLength;
        private readonly MemoryStream _data;

        public Response(string response, bool expectData) {
            var tokens = response.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if(tokens.Length == 0) {
                throw new EmptyResponseException();
            }
            Status = tokens[0];
            if(!expectData) {
                Arguments = new string[tokens.Length - 1];
                if(Arguments.Length > 0) {
                    Array.Copy(tokens, 1, Arguments, 0, Arguments.Length);
                }
                return;
            }
            Arguments = new string[tokens.Length - 2];
            if(Arguments.Length > 1) {
                Array.Copy(tokens, 1, Arguments, 0, Arguments.Length - 1);
            }
            _dataLength = long.Parse(tokens[tokens.Length - 1]);
            _data = new MemoryStream((int)_dataLength);
        }

        public Stream Data {
            get {
                if(_data == null) {
                    return null;
                }
                _data.Position = 0;
                return _data;
            }
        }

        public long DataLength { get { return _dataLength; } }

        public void AddData(byte[] buffer, int offset, int count) {
            _data.Write(buffer, offset, count);
        }
    }
}