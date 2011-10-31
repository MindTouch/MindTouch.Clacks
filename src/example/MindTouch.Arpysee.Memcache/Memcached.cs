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
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Server;

namespace MindTouch.Arpysee.Memcache {
    public class Memcached : IDisposable {
        private readonly ArpyseeServer _server;
        private readonly Dictionary<string, MemcacheData> _storage = new Dictionary<string, MemcacheData>();

        public Memcached(IPEndPoint endPoint) {
            _server = ServerBuilder
                .CreateAsync(endPoint)
                .WithCommand("set").HandledBy(Set).ExpectsData().Register()
                .WithCommand("get").HandledBy(Get).ExpectsData().Register()
                .Build();
        }

        private void Set(IRequest request, Action<IResponse> response) {
            var arguments = new StorageArgs(request);
            var expiration = arguments.Exptime == TimeSpan.Zero ? DateTime.MinValue : DateTime.UtcNow.Add(arguments.Exptime);
            lock(_storage) {
                _storage[arguments.Key] = new MemcacheData {
                    Flags = arguments.Flags,
                    Expiration = expiration,
                    Bytes = request.Data
                };
            }
            response(Response.Create("STORED"));
        }

        private void Get(IRequest request, Action<IResponse, Action> responseCallback) {

            // find values for provided keys
            var found = new List<MemcacheData>();
            lock(_storage) {
                foreach(var key in request.Arguments) {
                    MemcacheData data;
                    if(!_storage.TryGetValue(key, out data)) {
                        continue;
                    } 
                    found.Add(data);
                }
            }

            // create the final response that tells the client no more will be coming
            Action completion = () => responseCallback(Response.Create("END"), null);

            // if there were no values found, call the final response and exit
            if(!found.Any()) {
                completion();
                return;
            }

            // create a recursive action that will be called for each value in found
            Action iterator = null;
            var idx = 0;
            iterator = () => {
                var data = found[idx];
                idx++;
                var response = Response.Create("VALUE").WithArgument(data.Flags).WithData(data.Bytes);

                // if there are no more items left to return, set the next callback as the completion callback,
                // instead of ourselves
                responseCallback(response, found.Count == idx ? completion : iterator);
            };

            // kick off recursive iteration
            iterator();
        }

        public void Dispose() {
            _server.Dispose();
        }
    }

    public class MemcacheData {
        public uint Flags;
        public DateTime Expiration;
        public byte[] Bytes;
    }

    public class StorageArgs {
        public string Key;
        public uint Flags;
        public TimeSpan Exptime;
        public StorageArgs(IRequest request) {
            Key = request.Arguments[0];
            Flags = uint.Parse(request.Arguments[1]);
            Exptime = TimeSpan.FromSeconds(uint.Parse(request.Arguments[2]));
        }
    }
}
