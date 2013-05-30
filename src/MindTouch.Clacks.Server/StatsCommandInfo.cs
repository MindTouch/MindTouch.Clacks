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

namespace MindTouch.Clacks.Server {
    public class StatsCommandInfo {

        public readonly Guid ClientId;
        public readonly ulong RequestId;
        public readonly TimeSpan Elapsed;
        public readonly string[] Args;
        public readonly string Status;

        public StatsCommandInfo(Guid clientId, ulong requestId, TimeSpan elapsed, string[] args, string status) {
            ClientId = clientId;
            RequestId = requestId;
            Elapsed = elapsed;
            Args = args;
            Status = status;
        }
    }
}