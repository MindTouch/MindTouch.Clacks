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
using System.IO;
using System.Linq;
using System.Text;

namespace MindTouch.Arpysee.Client {

    //public interface IRequest {
    //    IRequest WithArgument<T>(T arg);
    //    IRequest WithData(byte[] data);
    //    IRequest ExpectData(string status);
    //}

    //public interface IMultiRequest {
    //    IMultiRequest WithArgument<T>(T arg);
    //    IMultiRequest WithData(byte[] data);
    //    IMultiRequest ExpectMultiple(string status, bool withData);
    //    IMultiRequest TerminatedBy(string status);
    //}

    //public class Request : IRequest, IMultiRequest, IRequestInfo {

    //    private const string TERMINATOR = "\r\n";

    //    public static IRequest Create(string command) {
    //        return new Request(command);
    //    }

    //    public static IMultiRequest CreateMulti(string command) {
    //        return new Request(command);
    //    }

    //    private readonly string _command;
    //    private readonly List<string> _arguments = new List<string>();
    //    private readonly HashSet<string> _expectsData = new HashSet<string>();
    //    private byte[] _data;

    //    public Request(string command) {
    //        _command = command;
    //    }

    //    public string Command { get { return _command; } }

    //    IRequest IRequest.WithArgument<T>(T arg) {
    //        return WithArgument(arg);
    //    }

    //    IMultiRequest IMultiRequest.WithArgument<T>(T arg) {
    //        return WithArgument(arg);
    //    }

    //    private Request WithArgument<T>(T arg) {
    //        _arguments.Add(arg.ToString());
    //        return this;
    //    }

    //    IRequest IRequest.WithData(byte[] data) {
    //        return WithData(data);
    //    }

    //    IMultiRequest IMultiRequest.WithData(byte[] data) {
    //        return WithData(data);
    //    }

    //    private Request WithData(byte[] data) {
    //        _data = data;
    //        return this;
    //    }

    //    public IMultiRequest ExpectMultiple(string status, bool withData) {
    //        return this;
    //    }

    //    public IMultiRequest TerminatedBy(string status) {
    //        return this;
    //    }

    //    public IRequest ExpectData(string status) {
    //        _expectsData.Add(status);
    //        return this;
    //    }

    //    bool IRequestInfo.ExpectsData(string status) {
    //        return _expectsData.Contains(status);
    //    }

    //    public byte[] AsBytes() {
    //        var sb = new StringBuilder();
    //        sb.Append(_command);
    //        if(_arguments.Any()) {
    //            sb.Append(" ");
    //            sb.Append(string.Join(" ", _arguments.ToArray()));
    //        }
    //        if(_data != null) {
    //            sb.Append(" ");
    //            sb.Append(_data.Length);
    //        }
    //        sb.Append(TERMINATOR);
    //        var data = Encoding.ASCII.GetBytes(sb.ToString());
    //        if(_data != null) {
    //            var d2 = new byte[data.Length + _data.Length + 2];
    //            data.CopyTo(d2, 0);
    //            Array.Copy(_data, 0, d2, data.Length, _data.Length);
    //            d2[d2.Length - 2] = (byte)'\r';
    //            d2[d2.Length - 1] = (byte)'\n';
    //            data = d2;
    //        }
    //        return data;
    //    }
    //}

    public abstract class ARequest : IRequestInfo {

        protected const string TERMINATOR = "\r\n";
        private readonly string _command;
        private readonly List<string> _arguments = new List<string>();
        protected readonly HashSet<string> _expectsData = new HashSet<string>();
        private byte[] _data;

        protected ARequest(string command) {
            _command = command;
        }

        public string Command { get { return _command; } }
        public abstract bool IsMultiRequest { get; }
        public abstract bool IsValid { get; }

        protected void InternalWithArgument<T>(T arg) {
            _arguments.Add(arg.ToString());
        }

        protected void InternalWithData(byte[] data) {
            _data = data;
        }

        public void InternalExpectData(string status) {
            _expectsData.Add(status);
        }


        bool IRequestInfo.ExpectsData(string status) {
            return _expectsData.Contains(status);
        }

        public byte[] AsBytes() {
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
    public class Request : ARequest {

        public static Request Create(string command) {
            return new Request(command);
        }

        public Request(string command) : base(command) { }

        public override bool IsMultiRequest { get { return false; } }
        public override bool IsValid { get { return true; } }

        public Request WithArgument<T>(T arg) {
            InternalWithArgument(arg);
            return this;
        }

        public Request WithData(byte[] data) {
            InternalWithData(data);
            return this;
        }

        public Request ExpectData(string status) {
            InternalExpectData(status);
            return this;
        }

    }

    public class MultiRequest : ARequest {

        private const string TERMINATOR = "\r\n";

        private string _terminatedBy;
        private HashSet<string> _expectedResponses = new HashSet<string>();
        public static MultiRequest Create(string command) {
            return new MultiRequest(command);
        }

        public MultiRequest(string command) : base(command) { }

        public override bool IsMultiRequest { get { return true; } }
        public override bool IsValid { get { return !string.IsNullOrEmpty(_terminatedBy); } }
        public string TerminationStatus { get { return _terminatedBy; } }

        public bool IsExpected(string status) {
            return _expectedResponses.Contains(status);
        }

        public MultiRequest WithArgument<T>(T arg) {
            InternalWithArgument(arg);
            return this;
        }

        public MultiRequest WithData(byte[] data) {
            InternalWithData(data);
            return this;
        }

        public MultiRequest ExpectMultiple(string status, bool withData) {
            if(withData) {
                InternalExpectData(status);
            }
            _expectedResponses.Add(status);
            return this;
        }

        public MultiRequest TerminatedBy(string status) {
            _terminatedBy = status;
            return this;
        }
    }
}