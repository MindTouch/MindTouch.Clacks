using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Clacks.Client;
using MindTouch.Clacks.Server;
using Request = MindTouch.Clacks.Client.Request;
using Response = MindTouch.Clacks.Server.Response;

namespace MindTouch.Clacks.Tester {
    class Program {
        static void Main(string[] args) {
            var endpoint = new IPEndPoint(IPAddress.Parse(args[0]), 12345);
            var type = args[1];
            var useAsync = true;
            if(args.Length > 2) {
                useAsync = args[2] == "sync";
            }
            if(type == "server") {
                if(useAsync) {
                    AsyncServer(endpoint);
                } else {
                    Server(endpoint);
                }
            } else {
                Client(endpoint);
            }
        }

        private static void Client(IPEndPoint ip) {
            using(var client = new ClacksClient(ip)) {
                Console.WriteLine("connected to server");
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var response = client.Exec(new Client.Request("BIN").ExpectData("OK"));
                    var text = Encoding.ASCII.GetString(response.Data);
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        private static void AsyncServer(IPEndPoint ip) {
            Console.WriteLine("starting server to listen on {0}", ip);
            var server = ServerBuilder
                .CreateAsync(ip)
                .WithDefaultHandler((request, response) =>
                    response(Clacks.Server.Response.Create("UNKNOWNCOMMAND"))
                )
                .WithCommand("ECHO")
                    .HandledBy((request, response) =>
                        response(Clacks.Server.Response.Create("ECHO").WithArguments(request.Arguments))
                    )
                    .Register()
                .WithCommand("BIN")
                    .HandledBy((request, response) => {
                        var payload = new StringBuilder();
                        for(var i = 0; i < 20; i++) {
                            payload.Append(Guid.NewGuid().ToString());
                        }
                        response(Clacks.Server.Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payload.ToString())));
                    })
                    .Register()
                .Build();
            Console.ReadLine();
            server.Dispose();
        }

        private static void Server(IPEndPoint ip) {
            Console.WriteLine("starting server to listen on {0}", ip);
            var server = ServerBuilder
                .CreateSync(ip)
                .WithDefaultHandler(request =>
                    Clacks.Server.Response.Create("UNKNOWNCOMMAND")
                )
                .WithCommand("ECHO")
                    .HandledBy(request =>
                        Clacks.Server.Response.Create("ECHO").WithArguments(request.Arguments)
                    )
                    .Register()
                .WithCommand("BIN")
                    .HandledBy(request => {
                        var payload = new StringBuilder();
                        for(var i = 0; i < 20; i++) {
                            payload.Append(Guid.NewGuid().ToString());
                        }
                        return Clacks.Server.Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payload.ToString()));
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
