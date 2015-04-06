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

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using log4net;
using MindTouch.Clacks.Client;
using NUnit.Framework;

namespace MindTouch.Clacks.Server.PerfTests {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class RountripTests {

        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
        }

        [Test]
        public void Async_Can_receive_many_binary_payload() {
            var payloadstring = "";
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
               .WithCommand("BIN")
                   .HandledBy((request, response) => {
                       var payload = new StringBuilder();
                       for(var i = 0; i < 10; i++) {
                           payload.Append(Guid.NewGuid().ToString());
                       }
                       payloadstring = payload.ToString();
                       response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring)));
                   })
                   .Register()
               .Build()
           ) {
                Console.WriteLine("created server");
                QueryServer(ref payloadstring);
            }
        }

        [Test]
        public void Sync_Can_receive_many_binary_payload() {
            var payloadstring = "";
            using(ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
               .WithCommand("BIN")
                   .HandledBy(request => {
                       var payload = new StringBuilder();
                       for(var i = 0; i < 10; i++) {
                           payload.Append(Guid.NewGuid().ToString());
                       }
                       payloadstring = payload.ToString();
                       return Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring));
                   })
                   .Register()
               .Build()
           ) {
                Console.WriteLine("created server");
                QueryServer(ref payloadstring);
            }
        }

        private void QueryServer(ref string payloadstring) {
            using(var client = new ClacksClient("127.0.0.1", _port)) {
                var n = 50000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var response = client.Exec(new Client.Request("BIN").ExpectData("OK"));
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(1, response.Arguments.Length);
                    Assert.AreEqual(payloadstring, Encoding.ASCII.GetString(response.Data));
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        [Test]
        public void Async_Can_make_many_requests() {
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
               .WithCommand("PING")
                   .HandledBy((request, response) => response(Response.Create("OK")))
                   .Register()
               .Build()
           ) {
                Console.WriteLine("created server");
                PingServer();
            }
        }

        [Test]
        public void Sync_Can_make_many_requests() {
            using(ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
               .WithCommand("PING")
                   .HandledBy(request => {
                       return Response.Create("OK");
                   })
                   .Register()
               .Build()
           ) {
                Console.WriteLine("created server");
                PingServer();
            }
        }

        private void PingServer() {
            using(var client = new ClacksClient("127.0.0.1", _port)) {
                var n = 50000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var response = client.Exec(new Client.Request("PING"));
                    Assert.AreEqual("OK", response.Status);
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        [Test]
        public void Async_Roundtrip_many_binary_payloads() {
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
               .WithCommand("BIN")
                   .HandledBy((request, response) => response(Response.Create("OK").WithData(request.Data)))
                   .Register()
               .Build()
           ) {
                Console.WriteLine("created server");
                EchoServer();
            }
        }

        [Test]
        public void Sync_Roundtrip_many_binary_payloads() {
            using(ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
               .WithCommand("BIN")
                   .HandledBy(request => Response.Create("OK").WithData(request.Data))
                   .Register()
               .Build()
           ) {
                Console.WriteLine("created server");
                EchoServer();
            }
        }

        private void EchoServer() {
            using(var client = new ClacksClient("127.0.0.1", _port)) {
                var n = 50000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var payload = new StringBuilder();
                    for(var j = 0; j < 10; j++) {
                        payload.Append(Guid.NewGuid().ToString());
                    }
                    var bytes = Encoding.ASCII.GetBytes(payload.ToString());
                    var response = client.Exec(new Client.Request("BIN").WithData(bytes).ExpectData("OK"));
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(1, response.Arguments.Length);
                    Assert.AreEqual(bytes, response.Data);
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }
    }
}

