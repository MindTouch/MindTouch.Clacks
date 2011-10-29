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
using System.Diagnostics;
using System.Net;
using System.Text;
using log4net;
using MindTouch.Arpysee.Client;
using MindTouch.Arpysee.Client.Net.Helper;
using NUnit.Framework;

namespace MindTouch.Arpysee.Server.Tests {

    [TestFixture]
    public class RountripTests {

        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Test]
        public void Async_Can_echo_data() {
            EchoData(true);
        }

        [Test]
        public void Sync_Can_echo_data() {
            EchoData(false);
        }

        private static void EchoData(bool useAsync) {
            var port = RandomPort;
            var registry = new CommandRepository();
            registry.Default((request, response) => 
                response(Response.Create("ECHO").WithArguments(request.Arguments)));
            _log.Debug("creating server");
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, useAsync)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", port)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("ECHO").WithArgument("foo").WithArgument("bar"));
                    Console.WriteLine("got response");
                    Assert.AreEqual("ECHO", response.Status);
                    Assert.AreEqual(new[] { "foo", "bar" }, response.Arguments);
                }
            }
        }

        [Test]
        public void Async_Can_send_binary_payload_with_auto_data_detection() {
            Can_send_binary_payload_with_auto_data_detection(new AsyncClientHandler());
        }

        [Test]
        public void Sync_Can_send_binary_payload_with_auto_data_detection() {
            Can_send_binary_payload_with_auto_data_detection(new SyncClientHandler());
        }


        private static void Can_send_binary_payload_with_auto_data_detection(IClientHandler clientHandler) {
            var port = RandomPort;
            var payloadstring = "blahblahblah";
            var payload = Encoding.ASCII.GetBytes(payloadstring);
            var registry = new CommandRepository();
            registry.Default((request, response) => response(Response.Create("OK").WithArguments(new[] { request.Data.Length.ToString(), Encoding.ASCII.GetString(request.Data) })));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, clientHandler)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", port)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("foo").WithData(payload));
                    Console.WriteLine("got response");
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(2, response.Arguments.Length);
                    Assert.AreEqual(payload.Length.ToString(), response.Arguments[0]);
                    Assert.AreEqual(payloadstring, response.Arguments[1]);
                }
            }
        }

        [Test]
        public void Async_Can_receive_binary_payload() {
            Receive_binary_payload(true);
        }

        [Test]
        public void Sync_Can_receive_binary_payload() {
            Receive_binary_payload(false);
        }

        private static void Receive_binary_payload(bool useAsync) {
            var port = RandomPort;
            var payloadstring = "blahblahblah";
            var payload = Encoding.ASCII.GetBytes(payloadstring);
            var registry = new CommandRepository();
            registry.Default((request, response) => response(Response.Create("OK").WithData(payload)));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, useAsync)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", port)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
                    Console.WriteLine("got response");
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(0, response.Arguments.Length);
                    Assert.AreEqual(payload.Length, response.DataLength);
                    Assert.AreEqual(payloadstring, response.Data.AsText());
                }
            }
        }

        [Test]
        public void Async_Can_receive_many_binary_payload() {
            Receive_many_binary_payloads(new AsyncClientHandler());
        }

        [Test]
        public void Sync_Can_receive_many_binary_payload() {
            Receive_many_binary_payloads(new SyncClientHandler());
        }

        private static void Receive_many_binary_payloads(IClientHandler clientHandler) {
            var port = RandomPort;
            string payloadstring = "";
            var registry = new CommandRepository();
            registry.Default((request, response) => {
                var payload = new StringBuilder();
                for(var i = 0; i < 10; i++) {
                    payload.Append(Guid.NewGuid().ToString());
                }
                payloadstring = payload.ToString();
                response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring)));
            });
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, clientHandler)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", port)) {
                    var n = 20000;
                    var t = Stopwatch.StartNew();
                    for(var i = 0; i < n; i++) {
                        var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
                        Assert.AreEqual("OK", response.Status);
                        Assert.AreEqual(0, response.Arguments.Length);
                        Assert.AreEqual(payloadstring, response.Data.AsText());
                    }
                    t.Stop();
                    var rate = n / t.Elapsed.TotalSeconds;
                    Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
                }
            }
        }

        [Test, Ignore]
        public void Can_receive_many_binary_payload_from_remote() {
            using(var client = new ArpyseeClient("192.168.168.190", 12345)) {
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var response = client.Exec(new Client.Request("BIN").ExpectData("OK"));
                    var text = response.Data.AsText();
                    //Console.WriteLine(text);
                    //return;
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        [Test]
        public void Async_Can_receive_many_binary_payload_with_sockets_from_pool_per_request() {
            Receive_many_binary_payload_with_sockets_from_pool_per_request(true);
        }

        [Test]
        public void Sync_Can_receive_many_binary_payload_with_sockets_from_pool_per_request() {
            Receive_many_binary_payload_with_sockets_from_pool_per_request(false);
        }

        private static void Receive_many_binary_payload_with_sockets_from_pool_per_request(bool useAsync) {
            var port = RandomPort;
            var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
            string payloadstring = "";
            var registry = new CommandRepository();
            registry.Default((request, response) => response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring))));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, useAsync)) {
                Console.WriteLine("created server");
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    using(var client = new ArpyseeClient("127.0.0.1", port)) {
                        payloadstring = "payload:" + i + ":" + basepayload;
                        var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
                        Assert.AreEqual("OK", response.Status);
                        Assert.AreEqual(0, response.Arguments.Length);
                        Assert.AreEqual(payloadstring, response.Data.AsText());
                    }
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        [Test]
        public void Async_Can_receive_many_binary_payload_with_a_socket_per_request() {
            Receive_many_binary_payload_with_a_socket_per_request(true);
        }

        [Test]
        public void Sync_Can_receive_many_binary_payload_with_a_socket_per_request() {
            Receive_many_binary_payload_with_a_socket_per_request(true);
        }

        private static void Receive_many_binary_payload_with_a_socket_per_request(bool useAsync) {
            var port = RandomPort;
            var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
            string payloadstring = "";
            var registry = new CommandRepository();
            registry.Default((request, response) => response(Response.Create("OK").WithData(Encoding.ASCII.GetBytes(payloadstring))));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), registry, useAsync)) {
                Console.WriteLine("created server");
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var socket = SocketAdapter.Open("127.0.0.1", port, TimeSpan.FromSeconds(30));
                    using(var client = new ArpyseeClient(socket)) {
                        payloadstring = "payload:" + i + ":" + basepayload;
                        var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
                        Assert.AreEqual("OK", response.Status);
                        Assert.AreEqual(0, response.Arguments.Length);
                        Assert.AreEqual(payloadstring, response.Data.AsText());
                    }
                }
                t.Stop();
                var rate = n / t.Elapsed.TotalSeconds;
                Console.WriteLine("Executed {0} commands at {1:0}commands/second", n, rate);
            }
        }

        private static int RandomPort {
            get { return new Random().Next(1000, 30000); }
        }
    }
}

