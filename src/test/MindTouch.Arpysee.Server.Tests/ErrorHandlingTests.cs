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
using System.Net;
using log4net;
using MindTouch.Arpysee.Client;
using NUnit.Framework;

namespace MindTouch.Arpysee.Server.Tests {

    [TestFixture]
    public class ErrorHandlingTests {

        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
        }

        [Test]
        public void Async_Exception_in_response_triggers_default_error_handler() {
            Expect_error_handler("ERROR", ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request, response) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        [Test]
        public void Sync_Exception_in_response_triggers_default_error_handler() {
            Expect_error_handler("ERROR", ServerBuilder
                .CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithDefaultHandler((request) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        [Test]
        public void Async_Exception_in_response_triggers_custom_error_handler() {
            Expect_error_handler("CUSTOMERROR", ServerBuilder
                .CreateAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithErrorHandler((r, e) => Response.Create("CUSTOMERROR"))
                .WithDefaultHandler((request, response) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        [Test]
        public void Sync_Exception_in_response_triggers_custom_error_handler() {
            Expect_error_handler("CUSTOMERROR", ServerBuilder
                .CreateSync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port))
                .WithErrorHandler((r, e) => Response.Create("CUSTOMERROR"))
                .WithDefaultHandler((request) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        private void Expect_error_handler(string errorStatus, ArpyseeServer server) {
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ArpyseeClient("127.0.0.1", _port)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("FAIL"));
                    Console.WriteLine("got response");
                    Assert.AreEqual(errorStatus, response.Status);
                }
            }
        }
    }
}
