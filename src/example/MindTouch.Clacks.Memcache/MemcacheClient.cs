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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Clacks.Client;

namespace MindTouch.Clacks.Memcache {
    public class MemcacheClient : IDisposable {
        private readonly ClacksClient _client;

        public MemcacheClient(IPEndPoint endPoint) {
            _client = new ClacksClient(endPoint);
        }

        public void Put(string key, uint flags, TimeSpan ttl, byte[] data) {
            _client.Exec(Request
                .Create("set")
                .WithArgument(key)
                .WithArgument(flags)
                .WithArgument(Math.Floor(ttl.TotalSeconds))
                .WithData(data)
            );
        }

        public KeyData Get(string key) {
            return Gets(new[] { key }).Values.FirstOrDefault();
        }

        public IDictionary<string, KeyData> Gets(IEnumerable<string> keys) {
            var request = MultiRequest
                .Create("get")
                .ExpectMultiple("VALUE", true)
                .TerminatedBy("END");
            foreach(var key in keys) {
                request.WithArgument(key);
            }
            var responses = _client.Exec(request);
            var values = new Dictionary<string, KeyData>();
            foreach(var response in responses) {
                if(response.Status == "END") {
                    break;
                }
                if(response.Status != "VALUE") {
                    throw new Exception(string.Format("received bad response status '{0}'", response.Status));
                }
                values[response.Arguments[0]] = new KeyData {
                    Data = response.Data,
                    Flags = uint.Parse(response.Arguments[1])
                };
            }
            return values;
        }

        public void Dispose() {
            _client.Dispose();
        }
    }

    public class KeyData {
        public byte[] Data;
        public uint Flags;
    }
}
