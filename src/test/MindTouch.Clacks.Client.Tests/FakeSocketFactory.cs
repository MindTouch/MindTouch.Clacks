using System.Collections.Generic;
using MindTouch.Clacks.Client.Net;

namespace MindTouch.Clacks.Client.Tests {
    public class FakeSocketFactory {
        public class FakeSocket : ISocket {
            public int DisposeCalled;

            public FakeSocket() {
                Connected = true;
            }

            public void Dispose() {
                DisposeCalled++;
                Connected = false;
            }

            public bool Connected { get; set; }
            public int Send(byte[] buffer, int offset, int size) {
                throw new System.NotImplementedException();
            }

            public int Receive(byte[] buffer, int offset, int size) {
                throw new System.NotImplementedException();
            }
        }

        public readonly List<FakeSocket> Sockets = new List<FakeSocket>();

        public ISocket Create() {
            var socket = new FakeSocket();
            lock(Sockets) {
                Sockets.Add(socket);
            }
            return socket;
        }
    }
}