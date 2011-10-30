using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Client;
using MindTouch.Arpysee.Server;
using Request = MindTouch.Arpysee.Client.Request;
using Response = MindTouch.Arpysee.Server.Response;

namespace MindTouch.Arpysee.Tester {
    class Program {
        static void Main(string[] args) {
            var port = 12345;
            var ip = IPAddress.Parse(args[0]);
            var type = args[1];
            var useAsync = true;
            if(args.Length > 2) {
                useAsync = args[2] == "sync";
            }
            if(type == "server") {
                if(useAsync) {
                    AsyncServer(ip);
                } else {
                    Server(ip);
                }
            } else {
                Client(ip);
            }
        }

        private static void Client(IPAddress ip) {
            using(var client = new ArpyseeClient(ip, 12345)) {
                Console.WriteLine("connected to server");
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var response = client.Exec(new Request("BIN").ExpectData("OK"));
                    var text = response.Data.AsText();
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        private static void AsyncServer(IPAddress ip) {
            Console.WriteLine("starting server to listen on {0}", ip);
            var server = ServerBuilder
                .CreateAsync(new IPEndPoint(ip, 12345))
                .WithDefaultHandler((request, response) =>
                    response(Response.Create("UNKNOWNCOMMAND"))
                )
                .WithCommand("ECHO")
                    .HandledBy((request, response) =>
                        response(Response.Create("ECHO").WithArguments(request.Arguments))
                    )
                    .Register()
                .WithCommand("BIN")
                    .HandledBy((request, response) => {
                        var payload = new StringBuilder();
                        for(var i = 0; i < 20; i++) {
                            payload.Append(Guid.NewGuid().ToString());
                        }
                        response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payload.ToString())));
                    })
                    .Register()
                .Build();
            Console.ReadLine();
            server.Dispose();
        }

        private static void Server(IPAddress ip) {
            Console.WriteLine("starting server to listen on {0}", ip);
            var server = ServerBuilder
                .CreateSync(new IPEndPoint(ip, 12345))
                .WithDefaultHandler(request =>
                    Response.Create("UNKNOWNCOMMAND")
                )
                .WithCommand("ECHO")
                    .HandledBy(request =>
                        Response.Create("ECHO").WithArguments(request.Arguments)
                    )
                    .Register()
                .WithCommand("BIN")
                    .HandledBy(request => {
                        var payload = new StringBuilder();
                        for(var i = 0; i < 20; i++) {
                            payload.Append(Guid.NewGuid().ToString());
                        }
                        return Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payload.ToString()));
                    })
                    .Register()
                .Build();
            Console.ReadLine();
            server.Dispose();
        }
    }
    public static class TestExtensions {
        public static MemoryStream AsStream(this string data) {
            return new MemoryStream(Encoding.UTF8.GetBytes(data)) { Position = 0 };
        }

        public static string AsText(this Stream stream) {
            stream.Position = 0;
            using(var reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }

    }
}
