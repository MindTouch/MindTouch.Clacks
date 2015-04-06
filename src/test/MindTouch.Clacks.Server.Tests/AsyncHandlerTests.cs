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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using MindTouch.Clacks.Client;
using NUnit.Framework;
using log4net;

namespace MindTouch.Clacks.Server.Tests {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class AsyncHandlerTests {

        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
        }

        [Test]
        public void Sync_Single_Response_Handler_is_called_exactly_once_per_request() {
            var requestCount = 0;
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("PING")
                    .ExpectsNoData()
                        .HandledBy(request => {
                            requestCount++;
                            return Response.Create("PONG");
                        })
                    .Register()
                .Build()
                ) {
                _log.Debug("created server");
                using(var client = new ClacksClient("127.0.0.1", _port)) {
                    _log.Debug("created client");
                    client.Exec(new Client.Request("PING"));
                    _log.Debug("got response");
                    Assert.AreEqual(1, requestCount);
                }
            }
        }

        [Test]
        public void Async_Single_Response_Handler_is_called_exactly_once_per_request() {
            var requestCount = 0;
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("PING")
                    .ExpectsNoData()
                        .HandledBy((request, responseCallback) => {
                            requestCount++;
                            responseCallback(Response.Create("PONG"));
                        })
                    .Register()
                .Build()
                ) {
                _log.Debug("created server");
                using(var client = new ClacksClient("127.0.0.1", _port)) {
                    _log.Debug("created client");
                    client.Exec(new Client.Request("PING"));
                    _log.Debug("got response");
                    Assert.AreEqual(1, requestCount);
                }
            }
        }

        [Test]
        public void Sync_Multi_Response_Handler_is_called_exactly_once_per_request() {
            var requestCount = 0;
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("PING")
                    .ExpectsNoData()
                        .HandledBy(request => {
                            requestCount++;
                            return new[] { Response.Create("PONG"), Response.Create("END") };
                        })
                    .Register()
                .Build()
                ) {
                _log.Debug("created server");
                using(var client = new ClacksClient("127.0.0.1", _port)) {
                    _log.Debug("created client");
                    client.Exec(new MultiRequest("PING")
                        .ExpectMultiple("PONG", false)
                        .TerminatedBy("END"));
                    _log.Debug("got response");
                    Assert.AreEqual(1, requestCount);
                }
            }
        }


        [Test]
        public void Async_Multi_Response_Handler_with_many_callbacks_is_called_exactly_once_per_request() {
            var requestCount = 0;
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("PING")
                    .ExpectsNoData()
                        .HandledBy((request, responseCallback) => {
                            requestCount++;
                            responseCallback(Response.Create("PONG"), () => responseCallback(Response.Create("END"), null));
                        })
                    .Register()
                .Build()
                ) {
                _log.Debug("created server");
                using(var client = new ClacksClient("127.0.0.1", _port)) {
                    _log.Debug("created client");
                    client.Exec(new MultiRequest("PING")
                        .ExpectMultiple("PONG", false)
                        .TerminatedBy("END"));
                    _log.Debug("got response");
                    Assert.AreEqual(1, requestCount);
                }
            }
        }

        [Test]
        public void Async_Multi_Response_Handler_with_single_callback_is_called_exactly_once_per_request() {
            var requestCount = 0;
            using(ServerBuilder.CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithCommand("PING")
                    .ExpectsNoData()
                        .HandledBy((request, responseCallback) => {
                            requestCount++;
                            responseCallback(new[] { Response.Create("PONG"), Response.Create("END") });
                        })
                    .Register()
                .Build()
                ) {
                _log.Debug("created server");
                using(var client = new ClacksClient("127.0.0.1", _port)) {
                    _log.Debug("created client");
                    client.Exec(new MultiRequest("PING")
                        .ExpectMultiple("PONG", false)
                        .TerminatedBy("END"));
                    _log.Debug("got response");
                    Assert.AreEqual(1, requestCount);
                }
            }
        }
    }
}