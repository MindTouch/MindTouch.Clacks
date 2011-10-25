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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Client;
using MindTouch.Arpysee.Client.Net.Helper;
using MindTouch.Arpysee.Client.Protocol;
using NUnit.Framework;

namespace MindTouch.Arpysee.Server.Tests {

    [TestFixture]
    public class RountripTests {

        [Test]
        public void Can_echo_data() {
            var registry = new CommandRepository();
            registry.RegisterDefault((request) => ServerResponse.WithStatus("ECHO").WithArguments(request));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345), registry)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", 12345)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Request("foo").AppendArgument("bar"));
                    Console.WriteLine("got response");
                    Assert.AreEqual("ECHO", response.Status);
                    Assert.AreEqual(new[] { "foo", "bar" }, response.Arguments);
                }
            }
        }

        [Test]
        public void Can_receive_binary_payload() {
            var payloadstring = "blahblahblah";
            var payload = Encoding.ASCII.GetBytes(payloadstring);
            var registry = new CommandRepository();
            registry.RegisterDefault(request => ServerResponse.WithStatus("OK").WithPayload(payload));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345), registry)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", 12345)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Request("foo").ExpectData());
                    Console.WriteLine("got response");
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(0, response.Arguments.Length);
                    Assert.AreEqual(payload.Length, response.DataLength);
                    Assert.AreEqual(payloadstring, response.Data.AsText());
                }
            }
        }

        [Test]
        public void Can_receive_many_binary_payload() {
            var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
            string payloadstring = "";
            var registry = new CommandRepository();
            registry.RegisterDefault(request => ServerResponse.WithStatus("OK").WithPayload(Encoding.ASCII.GetBytes(payloadstring)));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345), registry)) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", 12345)) {
                    var n = 10000;
                    var t = Stopwatch.StartNew();
                    for(var i = 0; i < n; i++) {
                        payloadstring = "payload:" + i + ":" + basepayload;
                        var response = client.Exec(new Request("foo").ExpectData());
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

        [Test]
        public void Can_receive_many_binary_payload_with_sockets_from_pool_per_request() {
            var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
            string payloadstring = "";
            var registry = new CommandRepository();
            registry.RegisterDefault(request => ServerResponse.WithStatus("OK").WithPayload(Encoding.ASCII.GetBytes(payloadstring)));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345), registry)) {
                Console.WriteLine("created server");
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    using(var client = new ArpyseeClient("127.0.0.1", 12345)) {
                        payloadstring = "payload:" + i + ":" + basepayload;
                        var response = client.Exec(new Request("foo").ExpectData());
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
        public void Can_receive_many_binary_payload_with_a_socket_per_request() {
            var basepayload = "231-]-023dm0-340-n--023fnu]23]-rumr0-]um]3-92    rujm[cwefcwjopwerunwer90nrwuiowrauioaweuneraucnunciuoaweciouwercairewcaonrwecauncu9032qu9032u90u9023u9023c9un3cun903rcun90;3rvyn90v54y9nv35q9n0;34un3qu0n'3ru0n'4vwau0pn'4wvaunw4ar9un23r]39un2r]-m3ur]nu3v=n0udrx2    m";
            string payloadstring = "";
            var registry = new CommandRepository();
            registry.RegisterDefault(request => ServerResponse.WithStatus("OK").WithPayload(Encoding.ASCII.GetBytes(payloadstring)));
            using(var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345), registry)) {
                Console.WriteLine("created server");
                var n = 10000;
                var t = Stopwatch.StartNew();
                for(var i = 0; i < n; i++) {
                    var socket = SocketAdapter.Open("127.0.0.1", 12345, TimeSpan.FromSeconds(30));
                    using(var client = new ArpyseeClient(socket)) {
                        payloadstring = "payload:" + i + ":" + basepayload;
                        var response = client.Exec(new Request("foo").ExpectData());
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
    }
}

