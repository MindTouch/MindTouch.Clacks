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
using System.Threading;
using log4net;
using MindTouch.Arpysee.Client;
using MindTouch.Arpysee.Client.Net.Helper;
using MindTouch.Arpysee.Server.Async;
using NUnit.Framework;

namespace MindTouch.Arpysee.Server.Tests {

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
        public void Async_Can_disconnect_client_via_command() {
            DisconnectTest(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port)).Build());
        }

        [Test]
        public void Sync_Can_disconnect_client_via_command() {
            DisconnectTest(ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port)).Build());
        }

        private void DisconnectTest(ArpyseeServer server) {
            _log.Debug("creating server");
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
                    Assert.IsFalse(client.Disposed);
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("BYE"));
                    Console.WriteLine("got response");
                    Assert.AreEqual("BYE", response.Status);
                    Thread.Sleep(100);
                    Console.WriteLine("checking disposed");
                    Assert.IsTrue(client.Disposed);
                }
            }
        }

        [Test]
        public void Async_Can_echo_data() {
            EchoData(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request, response) =>
                    response(Response.Create("ECHO").WithArguments(request.Arguments))
                )
                .Build());
        }

        [Test]
        public void Sync_Can_echo_data() {
            EchoData(ServerBuilder.CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler(request =>
                    Response.Create("ECHO").WithArguments(request.Arguments)
                )
                .Build());
        }

        private void EchoData(ArpyseeServer server) {
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
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
            Can_send_binary_payload_with_auto_data_detection(ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request, response) =>
                    response(Response.Create("OK").WithArguments(new[] { request.Data.Length.ToString(), Encoding.ASCII.GetString(request.Data) }))
                )
                .Build());
        }

        [Test]
        public void Sync_Can_send_binary_payload_with_auto_data_detection() {
            Can_send_binary_payload_with_auto_data_detection(ServerBuilder
                .CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request) =>
                     Response.Create("OK").WithArguments(new[] { request.Data.Length.ToString(), Encoding.ASCII.GetString(request.Data) })
                )
                .Build());
        }


        private void Can_send_binary_payload_with_auto_data_detection(ArpyseeServer server) {
            var payloadString = "blahblahblah";
            var payload = Encoding.ASCII.GetBytes(payloadString);
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("foo").WithData(payload));
                    Console.WriteLine("got response");
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(2, response.Arguments.Length);
                    Assert.AreEqual(payload.Length.ToString(), response.Arguments[0]);
                    Assert.AreEqual(payloadString, response.Arguments[1]);
                }
            }
        }

        [Test]
        public void Async_Can_receive_binary_payload() {
            var payloadstring = "blahblahblah";
            var payload = Encoding.ASCII.GetBytes(payloadstring);
            Receive_binary_payload(ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request, response) =>
                    response(Response.Create("OK").WithData(payload))
                )
                .Build(), payloadstring, payload);
        }

        [Test]
        public void Sync_Can_receive_binary_payload() {
            var payloadstring = "blahblahblah";
            var payload = Encoding.ASCII.GetBytes(payloadstring);
            Receive_binary_payload(ServerBuilder
                .CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request) =>
                     Response.Create("OK").WithData(payload)
                )
                .Build(), payloadstring, payload);
        }

        private void Receive_binary_payload(ArpyseeServer server, string payloadstring, byte[] payload) {
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("foo").ExpectData("OK"));
                    Console.WriteLine("got response");
                    Assert.AreEqual("OK", response.Status);
                    Assert.AreEqual(1, response.Arguments.Length);
                    Assert.AreEqual(payload.Length, response.Data.Length);
                    Assert.AreEqual(payload, response.Data);
                }
            }
        }

        [Test]
        public void Async_Can_receive_multi_binary_payload() {
            Receive_multi_binary_payload(ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("MULTI")
                    .HandledBy((request, responseCallback) => {
                        Action completion = () => {
                            _log.Debug("sending END");
                            responseCallback(Response.Create("END"), null);
                        };
                        if(!request.Arguments.Any()) {
                            completion();
                            return;
                        }
                        Action iterator = null;
                        var idx = 0;
                        iterator = () => {
                            var data = request.Arguments[idx];
                            idx++;
                            _log.DebugFormat("sending arg '{0}'", data);
                            var response = Response.Create("VALUE").WithData(Encoding.ASCII.GetBytes(data));
                            responseCallback(response, request.Arguments.Length == idx ? completion : iterator);
                        };
                        iterator();
                    })
                    .Register()
                .Build());
        }

        [Test]
        public void Sync_Can_receive_multi_binary_payload() {
            Receive_multi_binary_payload(ServerBuilder
                .CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("MULTI")
                    .HandledBy(request => {
                        var responses = request.Arguments
                            .Select(data => Response.Create("VALUE").WithData(Encoding.ASCII.GetBytes(data)))
                            .Cast<IResponse>()
                            .ToList();
                        responses.Add(Response.Create("END"));
                        return responses;
                    })
                    .Register()
                .Build());
        }

        private void Receive_multi_binary_payload(ArpyseeServer server) {
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
                    Console.WriteLine("created client");
                    var responses = client.Exec(new MultiRequest("MULTI")
                        .WithArgument("foo")
                        .WithArgument("bar")
                        .ExpectMultiple("VALUE", true)
                        .TerminatedBy("END")
                    );
                    Console.WriteLine("got responses");
                    Assert.AreEqual(3, responses.Count());
                    Assert.AreEqual("VALUE", responses.ElementAt(0).Status);
                    Assert.AreEqual("foo", Encoding.ASCII.GetString(responses.ElementAt(0).Data));
                    Assert.AreEqual("VALUE", responses.ElementAt(1).Status);
                    Assert.AreEqual("bar", Encoding.ASCII.GetString(responses.ElementAt(1).Data));
                    Assert.AreEqual("END", responses.ElementAt(2).Status);
                }
            }
        }

        [Test]
        public void Async_Can_receive_multi_response_with_async_multi_handler() {
            Receive_multi_response(ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("MULTI")
                    .HandledBy((request, responseCallback) => {
                        Action completion = () => {
                            _log.Debug("sending END");
                            responseCallback(Response.Create("END"), null);
                        };
                        if(!request.Arguments.Any()) {
                            completion();
                            return;
                        }
                        Action iterator = null;
                        var idx = 0;
                        iterator = () => {
                            var data = request.Arguments[idx];
                            idx++;
                            _log.DebugFormat("sending arg '{0}'", data);
                            var response = Response.Create("VALUE").WithArgument(data);
                            responseCallback(response, request.Arguments.Length == idx ? completion : iterator);
                        };
                        iterator();
                    })
                    .Register()
                .Build());
        }

        [Test]
        public void Async_Can_receive_multi_response_with_async_sync_handler() {
            Receive_multi_response(ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("MULTI")
                    .HandledBy((request, responseCallback) => {
                        var responses = request.Arguments
                           .Select(data => Response.Create("VALUE").WithArgument(data))
                           .Cast<IResponse>()
                           .ToList();
                        responses.Add(Response.Create("END"));
                        responseCallback(responses);
                    })
                    .Register()
                .Build());
        }

        [Test]
        public void Sync_Can_receive_multi_response() {
            Receive_multi_response(ServerBuilder
                .CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("MULTI")
                    .HandledBy(request => {
                        var responses = request.Arguments
                            .Select(data => Response.Create("VALUE").WithArgument(data))
                            .Cast<IResponse>()
                            .ToList();
                        responses.Add(Response.Create("END"));
                        return responses;
                    })
                    .Register()
                .Build());
        }

        private void Receive_multi_response(ArpyseeServer server) {
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
                    Console.WriteLine("created client");
                    var responses = client.Exec(new MultiRequest("MULTI")
                        .WithArgument("foo")
                        .WithArgument("bar")
                        .ExpectMultiple("VALUE", false)
                        .TerminatedBy("END")
                    );
                    Console.WriteLine("got responses");
                    Assert.AreEqual(3, responses.Count());
                    var r = responses.ToArray();
                    Assert.AreEqual("VALUE", r[0].Status);
                    Assert.AreEqual(1, r[0].Arguments.Length);
                    Assert.AreEqual("foo", r[0].Arguments[0]);
                    Assert.AreEqual("VALUE", r[1].Status);
                    Assert.AreEqual(1, r[1].Arguments.Length, string.Join(",", r[1].Arguments));
                    Assert.AreEqual("bar", r[1].Arguments[0]);
                    Assert.AreEqual("END", r[2].Status);
                    Assert.AreEqual(0, r[2].Arguments.Length, string.Join(",", r[2].Arguments));
                }
            }
        }
    }
}

