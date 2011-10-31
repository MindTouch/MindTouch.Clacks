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
        public byte[] Data;

        public Response(string[] response) {
            if(response.Length == 0) {
                throw new EmptyResponseException();
            }
            Status = response[0];
            Arguments = new string[response.Length - 1];
            if(Arguments.Length > 0) {
                Array.Copy(response, 1, Arguments, 0, Arguments.Length);
            }
       }
    }
}