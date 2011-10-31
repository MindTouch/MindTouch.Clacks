using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Client;

namespace MindTouch.Arpysee.Memcache {
    public class MemcacheClient : IDisposable {
        private readonly ArpyseeClient _client;

        public MemcacheClient(IPEndPoint endPoint) {
            _client = new ArpyseeClient(endPoint);
        }

        public IDictionary<string, KeyData> Gets(IEnumerable<string> keys) {
            var responses = _client.Exec(MultiRequest.Create("gets").ExpectMultiple("VALUE", true).TerminatedBy("END"));
            var values = new Dictionary<string, KeyData>();
            foreach(var response in responses) {
                if(response.Status != "VALUE") {
                    throw new Exception(string.Format("received bad response status '{0}", response.Status));
                }
                values[response.Arguments[0]] = new KeyData {
                    Data = response.Data,
                    DataLength = response.DataLength,
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
        public Stream Data;
        public long DataLength;
        public uint Flags;
    }
}
