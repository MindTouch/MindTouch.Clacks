/*
 * MindTouch.Clacks
 * 
 * Copyright (C) 2011 Arne F. Claassen
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

namespace MindTouch.Clacks.Client {
    public abstract class ClacksClientException : Exception {
        protected ClacksClientException() { }
        protected ClacksClientException(string message) : base(message) { }
        protected ClacksClientException(string message, Exception exception) : base(message, exception) { }
    }

    public abstract class ConnectionException : ClacksClientException {
        protected ConnectionException() { }
        protected ConnectionException(string message) : base(message) { }
    }

    public class ReadException : ConnectionException {
        public ReadException(string message) : base(message) { }

    }
    public class WriteException : ConnectionException { }

    public class ConnectException : ClacksClientException {
        public ConnectException(Exception exception)
            : base("Unable to Connect to server", exception) {
        }
    }

    public class EmptyResponseException : ConnectionException { }
    public class TimedoutException : ClacksClientException { }
    public class ShouldNeverHappenException : ClacksClientException { }
    public class InitException : ClacksClientException { }

    public class UnknowResponseException : ClacksClientException {
        public readonly string Response;
        public UnknowResponseException(string response)
            : base(string.Format("Response '{0}' is not supported by this client", response)) {
            Response = response;
        }
    }
}
