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
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using log4net;
using MindTouch.Clacks.Client.Net.Helper;
using MindTouch.Clacks.Client;
using NUnit.Framework;

namespace MindTouch.Clacks.Server.Tests {

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
                var n = 30000;
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

        //[Test]
        //public void Async_Can_receive_many_binary_payload_with_sockets_from_pool_per_request() {
        //    Receive_many_binary_payload_with_sockets_from_pool_per_request(true);
        //}

        //[Test]
        //public void Sync_Can_receive_many_binary_payload_with_sockets_from_pool_per_request() {
        //    Receive_many_binary_payload_with_sockets_from_pool_per_request(false);
        //}

        //private static void Receive_many_binary_payload_with_sockets_from_pool_per_request(bool useAsync) {
        //    var port = RandomPort;
        //    var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
        //    string payloadstring = "";
        //    var registry = new AsyncCommandRepository();
        //    registry.Default((request, response) => response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring))));
        //    using(var server = new ClacksServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, useAsync)) {
        //        Console.WriteLine("created server");
        //        var n = 10000;
        //        var t = Stopwatch.StartNew();
        //        for(var i = 0; i < n; i++) {
        //            using(var client = new ClacksClient("127.0.0.1", port)) {
        //                payloadstring = "payload:" + i + ":" + basepayload;
        //                var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
        //                Assert.AreEqual("OK", response.Status);
        //                Assert.AreEqual(0, response.Arguments.Length);
        //                Assert.AreEqual(payloadstring, response.Data.AsText());
        //            }
        //        }
        //        t.Stop();
        //        var rate = n / t.Elapsed.TotalSeconds;
        //        Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
        //    }
        //}

        //[Test]
        //public void Async_Can_receive_many_binary_payload_with_a_socket_per_request() {
        //    Receive_many_binary_payload_with_a_socket_per_request(true);
        //}

        //[Test]
        //public void Sync_Can_receive_many_binary_payload_with_a_socket_per_request() {
        //    Receive_many_binary_payload_with_a_socket_per_request(true);
        //}

        //private static void Receive_many_binary_payload_with_a_socket_per_request(bool useAsync) {
        //    var port = RandomPort;
        //    var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
        //    string payloadstring = "";
        //    var registry = new AsyncCommandRepository();
        //    registry.Default((request, response) => response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring))));
        //    using(var server = new ClacksServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, useAsync)) {
        //        Console.WriteLine("created server");
        //        var n = 10000;
        //        var t = Stopwatch.StartNew();
        //        for(var i = 0; i < n; i++) {
        //            var socket = SocketAdapter.Open("127.0.0.1", port, TimeSpan.FromSeconds(30));
        //            using(var client = new ClacksClient(socket)) {
        //                payloadstring = "payload:" + i + ":" + basepayload;
        //                var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
        //                Assert.AreEqual("OK", response.Status);
        //                Assert.AreEqual(0, response.Arguments.Length);
        //                Assert.AreEqual(payloadstring, response.Data.AsText());
        //            }
        //        }
        //        t.Stop();
        //        var rate = n / t.Elapsed.TotalSeconds;
        //        Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
        //    }
        //}

        //private static int RandomPort {
        //    get { return new Random().Next(1000, 30000); }
        //}
    }
}

