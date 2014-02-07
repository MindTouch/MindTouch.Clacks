using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using MindTouch.Clacks.Client;
using MindTouch.Clacks.Client.Net.Helper;
using MindTouch.Clacks.Tester;
using log4net;

namespace MindTouch.Clacks.Server.ConnectionTest {
    internal class Program {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static void Main(string[] args) {
            var runServer = false;
            var runClient = false;
            var async = false;
            var port = 12345;
            var host = "127.0.0.1";
            var options = new Options {
                {"S", "Run Server", x => runServer = x != null},
                {"C", "Run Client", x => runClient = x != null},
                {"a", "Use Async server", x => async = x != null},
                {"p=", "Server port (default: 12345)", x=> port = int.Parse(x)},
                {"h=", "Server host (default: 127.0.0.1)", x=> host = x},
            };
            options.Parse(args).ToArray();
            _log.DebugFormat("Address: {0}:{1}", host, port);
            if(runServer) {
                RunServer(host, port, async);
            }
            if(runClient) {
                RunClient(host, port);
            }
            Console.WriteLine("Press any key");
            Console.ReadKey();
        }

        private static void RunClient(string host, int port) {
            _log.Debug("running Client");
            var i = 0;
            var clients = new List<ClacksClient>();
            while(true) {
                i++;
                _log.DebugFormat("opening connection {0}", i);
                var client = new ClacksClient(SocketAdapter.Open(host, port));
                clients.Add(client);
                var response = client.Exec(new Client.Request("PING"));
                _log.DebugFormat("opened connection {0}: {1}", i, response.Status);
            }
        }

        private static void RunServer(string host, int port, bool async) {
            var instrumentation = new Instrumentation();
            if(async) {
                _log.Debug("running async Server");
                ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse(host), port))
                    .WithCommand("PING")
                    .HandledBy(request => {
                        _log.DebugFormat("Current Connections: {0}", instrumentation.Connections);
                        return Response.Create("OK");
                    })
                    .Register()
                    .Build(instrumentation);
            } else {
                _log.Debug("running sync Server");
                ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse(host), port))
                    .WithCommand("PING")
                    .HandledBy(request => {
                    _log.DebugFormat("Current Connections: {0}", instrumentation.Connections);
                        return Response.Create("OK");
                        })
                    .Register()
                    .Build(instrumentation);
            }
        }
    }

    public class Instrumentation : IClacksInstrumentation {
        public int Connections;
        public void ClientConnected(Guid clientId, IPEndPoint remoteEndPoint) {
            Interlocked.Increment(ref Connections);
        }

        public void ClientDisconnected(Guid clientId) {
            Interlocked.Decrement(ref Connections);
        }

        public void CommandCompleted(StatsCommandInfo info) {
        }

        public void AwaitingCommand(Guid clientId, ulong requestId) {
        }

        public void ProcessedCommand(StatsCommandInfo statsCommandInfo) {
        }

        public void ReceivedCommand(StatsCommandInfo statsCommandInfo) {
        }

        public void ReceivedCommandPayload(StatsCommandInfo statsCommandInfo) {
        }
    }
}
