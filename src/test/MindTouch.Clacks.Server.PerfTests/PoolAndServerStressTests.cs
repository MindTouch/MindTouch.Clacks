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
using MindTouch.Clacks.Client;
using MindTouch.Clacks.Client.Net;
using MindTouch.Clacks.Client.Net.Helper;
using NUnit.Framework;
using log4net;

namespace MindTouch.Clacks.Server.PerfTests {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    [Category("LongRunning")]
    public class PoolAndServerStressTests {


        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
        }

        [Test]
        public void Get_many_sockets_from_pool_sequentially() {
            using(ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port)).Build()) {
                var pool = ConnectionPool.Create("127.0.0.1", _port);
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    using(var s = pool.GetSocket()) { }
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} connect/commmands at {1:0}commands/second", n, rate);
            }
        }

        [Test]
        public void Sync_can_create_many_clients_with_pool() {
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
                var pool = ConnectionPool.Create("127.0.0.1", _port);
                var n = 20000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    using(var client = new ClacksClient(pool)) {
                        var response = client.Exec(new Client.Request("BIN").ExpectData("OK"));
                        Assert.AreEqual("OK", response.Status);
                        Assert.AreEqual(1, response.Arguments.Length);
                        Assert.AreEqual(payloadstring, Encoding.ASCII.GetString(response.Data));
                    }
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} connect/commmands at {1:0}commands/second", n, rate);
            }
        }

        [Test]
        public void Sync_can_create_many_clients_with_own_sockets() {
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
                var n = 1000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    using(var client = new ClacksClient(SocketAdapter.Open("127.0.0.1", _port))) {
                        var response = client.Exec(new Client.Request("BIN").ExpectData("OK"));
                        Assert.AreEqual("OK", response.Status);
                        Assert.AreEqual(1, response.Arguments.Length);
                        Assert.AreEqual(payloadstring, Encoding.ASCII.GetString(response.Data));
                    }
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} connect/commmands at {1:0}commands/second", n, rate);
            }
        }
    }
}
