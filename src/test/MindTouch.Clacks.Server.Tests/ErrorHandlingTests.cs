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
using System.Net;
using System.Text;
using MindTouch.Clacks.Client.Net;
using MindTouch.Clacks.Client.Net.Helper;
using log4net;
using MindTouch.Clacks.Client;
using NUnit.Framework;

namespace MindTouch.Clacks.Server.Tests {

    [TestFixture]
    public class ErrorHandlingTests {

        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;
        private IPEndPoint _endpoint;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
            _endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port);
        }

        [Test]
        public void Async_Exception_in_response_triggers_default_error_handler() {
            Expect_error_handler("ERROR", ServerBuilder
                .CreateAsync(_endpoint)
                .WithDefaultHandler((request, response) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        [Test]
        public void Sync_Exception_in_response_triggers_default_error_handler() {
            Expect_error_handler("ERROR", ServerBuilder
                .CreateSync(_endpoint)
                .WithDefaultHandler((request) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        [Test]
        public void Async_Exception_in_response_triggers_custom_error_handler() {
            Expect_error_handler("CUSTOMERROR", ServerBuilder
                .CreateAsync(_endpoint)
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
                .CreateSync(_endpoint)
                .WithErrorHandler((r, e) => Response.Create("CUSTOMERROR"))
                .WithDefaultHandler((request) => {
                    throw new Exception("failz");
                })
                .Build()
            );
        }

        [Test]
        public void Missing_data_terminator_returns_error_response_does_not_terminate_server() {
            using(ServerBuilder
                .CreateAsync(_endpoint)
                .WithDefaultHandler(request => Response.Create("OK"))
                .Build()
            ) {
                for(int i = 0; i < 2; i++) {

                    var socket = SocketAdapter.Open(_endpoint);
                    var request = Encoding.ASCII.GetBytes("HELLO 4\r\n0123456789\r\n");
                    socket.Send(request, 0, request.Length);
                    var response = new byte[1024];
                    try {
                        var read = socket.Receive(response, 0, 1024);
                    } catch { }
                }
            }

        }

        private void Expect_error_handler(string errorStatus, ClacksServer server) {
            using(server) {
                Console.WriteLine("created server");
                using(var client = new ClacksClient(_endpoint)) {
                    Console.WriteLine("created client");
                    var response = client.Exec(new Client.Request("FAIL"));
                    Console.WriteLine("got response");
                    Assert.AreEqual(errorStatus, response.Status);
                }
            }
        }
    }
}
