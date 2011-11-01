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
using System.Text;

namespace MindTouch.Arpysee.Server {
    public static class Extensions {
        private const string TERMINATOR = "\r\n";
        
        public static byte[] GetBytes(this IResponse response) {
            var sb = new StringBuilder();
            sb.Append(response.Status);
            foreach(var arg in response.Arguments) {
                sb.Append(" ");
                sb.Append(arg);
            }
            if(response.Data != null) {
                sb.Append(" ");
                sb.Append(response.Data.Length);
            }
            sb.Append(TERMINATOR);
            var data = Encoding.ASCII.GetBytes(sb.ToString());
            if(response.Data != null) {
                var d2 = new byte[data.Length + response.Data.Length + 2];
                data.CopyTo(d2, 0);
                Array.Copy(response.Data, 0, d2, data.Length, response.Data.Length);
                d2[d2.Length - 2] = (byte)'\r';
                d2[d2.Length - 1] = (byte)'\n';
                data = d2;
            }
            return data;
        }
    }
}