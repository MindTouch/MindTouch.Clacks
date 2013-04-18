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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MindTouch.Clacks.Client;
using NUnit.Framework;
using log4net;

namespace MindTouch.Clacks.Server.PerfTests {
    [TestFixture]
    public class ParallelRequestTests {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
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
            var n = 10000;
            var w = 50;
            var startSignal = new ManualResetEvent(false);
            var readySignals = new List<WaitHandle>();
            var workers = new List<Task>();
            var t = Stopwatch.StartNew();
            for(var j = 0; j < w; j++) {
                var id = j;
                var ready = new ManualResetEvent(false);
                readySignals.Add(ready);
                workers.Add(Task.Factory.StartNew(() => {
                    using(var client = new ClacksClient("127.0.0.1", _port)) {
                        ready.Set();
                        startSignal.WaitOne();
                        for(var i = 0; i < n; i++) {
                            var payload = Guid.NewGuid().ToString();
                            var bytes = Encoding.ASCII.GetBytes(payload);
                            var response = client.Exec(new Client.Request("BIN").WithData(bytes).ExpectData("OK"));
                            Assert.AreEqual("OK", response.Status);
                            Assert.AreEqual(1, response.Arguments.Length);
                            Assert.AreEqual(bytes, response.Data);
                        }
                    }
                }));
            }
            Console.WriteLine("waiting for workers to get ready");
            WaitHandle.WaitAll(readySignals.ToArray());
            startSignal.Set();
            Console.WriteLine("starting work");
            Task.WaitAll(workers.ToArray());
            t.Stop();
            var rate = n * w / t.Elapsed.TotalSeconds;
            Console.WriteLine("Executed {0} commands at {1:0}commands/second", n * w, rate);
        }
    }
}