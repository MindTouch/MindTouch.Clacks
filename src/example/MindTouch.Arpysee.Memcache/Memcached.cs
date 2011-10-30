using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Server;

namespace MindTouch.Arpysee.Memcache {
    public class Memcached : IDisposable{
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
            var arguments = GetStorageArguments(request);
            var expiration = arguments.Exptime == TimeSpan.Zero ? DateTime.MinValue : DateTime.UtcNow.Add(arguments.Exptime);
            lock(_storage) {
                _storage[arguments.Key] =  new MemcacheData {
                    Flags = arguments.Flags,
                    Expiration = expiration,
                    Bytes = request.Data
                };
            }
            response(Response.Create("STORED"));
        }

        private void Get(IRequest request, Action<IResponse> response) {
            throw new NotImplementedException();
        }
        private StorageArgs GetStorageArguments(IRequest request) {
            return new StorageArgs(request);
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
