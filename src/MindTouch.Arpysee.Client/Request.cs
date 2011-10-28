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
using System.Text;
using MindTouch.Arpysee.Client.Protocol;

namespace MindTouch.Arpysee.Client {
    public class Request {

        private static readonly byte[] _linefeed = new[] { (byte)'\r', (byte)'\n' };

        public static Request Create(string command) {
            return new Request(command);
        }

        private readonly string _command;
        private readonly MemoryStream _request = new MemoryStream();
        private Stream _data;
        private long _dataLength;
        private bool _done;
        private bool _expectData;

        public Request(string command) {
            _command = command;
            _request.Write(Encoding.ASCII.GetBytes(command));
        }

        public string Command { get { return _command; } }
        public bool ExpectsData { get { return _expectData; } }
        public Request With(string arg) {
            ThrowIfDone();
            _request.WriteByte((byte)' ');
            _request.Write(Encoding.ASCII.GetBytes(arg));
            return this;
        }

        public Request With(uint arg) {
            ThrowIfDone();
            _request.WriteByte((byte)' ');
            _request.Write(Encoding.ASCII.GetBytes(arg.ToString()));
            return this;
        }

        public Request With(TimeSpan arg) {
            ThrowIfDone();
            _request.WriteByte((byte)' ');
            _request.Write(Encoding.ASCII.GetBytes(((uint)arg.TotalSeconds).ToString()));
            return this;
        }

        public Request WithData(Stream data, long dataLength) {
            ThrowIfDone();
            _data = data;
            _dataLength = dataLength;
            return With((uint)_dataLength);
        }

        public Request ExpectData() {
            ThrowIfDone();
            _expectData = true;
            return this;
        }

        public RequestData[] GetData() {
            ThrowIfDone();
            _done = true;
            _request.Write(_linefeed);
            _request.Seek(0, SeekOrigin.Begin);
            if(_data == null) {
                return new[] { new RequestData(_request, _request.Length) };
            }
            if(_data.CanSeek) {
                _data.Seek(0, SeekOrigin.Begin);
            }
            return new[] { new RequestData(_request, _request.Length), new RequestData(_data, _dataLength), new RequestData(_linefeed) };
        }

        private void ThrowIfDone() {
            if(_done) {
                throw new InvalidOperationException("cannot append request after it has been converted to streams");
            }
        }
    }
}