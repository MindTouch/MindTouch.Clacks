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

namespace MindTouch.Clacks.Client {
    public class MultiRequest : ARequest, IMultiRequestInfo {
        private string _terminatedBy;
        private readonly HashSet<string> _expectedResponses = new HashSet<string>();

        public static MultiRequest Create(string command) {
            return new MultiRequest(command);
        }

        public MultiRequest(string command) : base(command) { }

        bool IMultiRequestInfo.IsValid { get { return !string.IsNullOrEmpty(_terminatedBy); } }
        string IMultiRequestInfo.TerminationStatus { get { return _terminatedBy; } }

        bool IMultiRequestInfo.IsExpected(string status) {
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
            _expectedResponses.Add(status);
            _terminatedBy = status;
            return this;
        }

        int IRequestInfo.ExpectedBytes(Response response) {
            return InternalExpectedBytes(response);
        }

        byte[] IRequestInfo.AsBytes() {
            return GetBytes();
        }
    }
}