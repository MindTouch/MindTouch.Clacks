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
using System.IO;
using MindTouch.Clacks.Client.Net;

namespace MindTouch.Clacks.Client {
    public static class Extensions {

        public static void Write(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }

        public static void SendRequest(this ISocket socket, IRequestInfo request, bool retry) {
            var bytes = request.AsBytes();
            socket.SendBuffer(bytes, bytes.Length, retry);
        }

        private static void SendBuffer(this ISocket socket, byte[] buffer, int count, bool retry) {
            var offset = 0;
            while(count > 0) {
                var sent = socket.Send(buffer, offset, count, retry);
                offset += sent;
                count -= sent;
            }
        }
    }
}
