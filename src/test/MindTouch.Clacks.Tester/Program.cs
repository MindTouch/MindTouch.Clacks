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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MindTouch.Clacks.Client;
using MindTouch.Clacks.Client.Net;
using MindTouch.Clacks.Server;
using MindTouch.Clacks.Server.Sync;
using Response = MindTouch.Clacks.Server.Response;

namespace MindTouch.Clacks.Tester {

    public enum WorkerStatus {
        PreClient,
        PreRequest,
        PostRequest,
        Sleeping,
        CheckingPool,
        GeneratingPayload,
        MakingRequest,
        CheckingResponse
    }

    class Program {
        static void Main(string[] args) {
            var workerCount = 50;
            var faultInterval = 100000;
            var sleepInterval = 50;
            if(args != null && args.Length > 0) {
                workerCount = int.Parse(args[0]);
                if(args.Length > 1) {
                    sleepInterval = int.Parse(args[1]);
                }
                if(args.Length > 2) {
                    faultInterval = int.Parse(args[2]);
                }
            }
            var port = new Random().Next(1000, 30000);
            var statsCollector = new StatsCollector();
            var dispatcher = new SyncCommandRepository();
            dispatcher.AddCommand("BIN", request => Response.Create("OK").WithData(request.Data), DataExpectation.Auto);
            var clientHandlerFactory = new FaultingSyncClientHandlerFactory(dispatcher, faultInterval);
            var workerStatus = new WorkerStatus[workerCount];
            using(new ClacksServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port), statsCollector, clientHandlerFactory)) {
                Console.WriteLine("created server");
                Console.WriteLine("Starting {0} workers with a {1}ms sleep interval", workerCount, sleepInterval);
                var startSignal = new ManualResetEvent(false);
                var readySignals = new List<WaitHandle>();
                var workers = new List<Task>();
                var workerRequests = new int[workerCount];
                var pool = ConnectionPool.Create("127.0.0.1", port);
                var poolUseCount = 0;
                for(var i = 0; i < workerCount; i++) {
                    var ready = new ManualResetEvent(false);
                    readySignals.Add(ready);
                    var myPool = pool;
                    var workerId = i;
                    workers.Add(Task.Factory.StartNew(() => {
                        var r = new Random();
                        ready.Set();
                        startSignal.WaitOne();
                        var localUseCount = 0;
                        var maxPoolUse = r.Next(50, 150);
                        while(true) {
                            workerStatus[workerId] = WorkerStatus.CheckingPool;
                            localUseCount++;
                            var globalUseCount = Interlocked.Increment(ref poolUseCount);
                            if(globalUseCount > r.Next(200, 300)) {
                                Console.WriteLine("{0}: creating new pool", workerId);
                                Interlocked.Exchange(ref poolUseCount, 0);
                                pool = ConnectionPool.Create("127.0.0.1", port);
                                myPool = pool;
                            } else if(myPool != pool && localUseCount > maxPoolUse) {
                                Console.WriteLine("{0}: picking new pool", workerId);
                                myPool = pool;
                                localUseCount = 0;
                            }
                            using(var client = new ClacksClient(myPool)) {
                                for(var k = 0; k < 1000; k++) {
                                    workerStatus[workerId] = WorkerStatus.GeneratingPayload;
                                    var payload = new StringBuilder();
                                    payload.AppendFormat("id:{0},data:", workerId);
                                    var size = r.Next(2, 100);
                                    for(var l = 0; l < size; l++) {
                                        payload.Append(Guid.NewGuid().ToString());
                                    }
                                    var data = payload.ToString();
                                    var bytes = Encoding.ASCII.GetBytes(data);
                                    workerStatus[workerId] = WorkerStatus.MakingRequest;
                                    var response = client.Exec(new Client.Request("BIN").WithArgument(workerId).WithData(bytes).ExpectData("OK"));
                                    workerStatus[workerId] = WorkerStatus.CheckingResponse;
                                    if(response.Status != "OK") {
                                        throw new Exception("wrong status: " + response.Status);
                                    }
                                    if(response.Arguments.Length != 1) {
                                        throw new Exception("wrong arg length: " + response.Arguments.Length);
                                    }
                                    var responseData = Encoding.ASCII.GetString(response.Data);
                                    if(data != responseData) {
                                        lock(workers) {
                                            Console.WriteLine("bad data response for worker {0}", workerId);
                                            Console.WriteLine("sent:     {0}", data);
                                            Console.WriteLine("received: {0}", responseData);
                                        }
                                    }
                                    workerRequests[workerId]++;
                                    workerStatus[workerId] = WorkerStatus.Sleeping;
                                    Thread.Sleep(sleepInterval);
                                }
                            }
                        }
                    }));
                }
                Console.WriteLine("waiting for workers to get ready");
                WaitHandle.WaitAll(readySignals.ToArray());
                startSignal.Set();
                Console.WriteLine("starting work");
                var t = Stopwatch.StartNew();
                Task.Factory.StartNew(() => {
                    var lastRequests = new int[workerCount];
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    while(true) {
                        lock(statsCollector) {
                            Console.WriteLine("{0} Processed {1} requests with {2} faults via {3}/{4} total/active connections at {5,6:0} requests/second and {6:0.000}ms/request",
                                DateTime.Now,
                                statsCollector.Requests,
                                clientHandlerFactory.Faults,
                                statsCollector.Connected,
                                statsCollector.Connected - statsCollector.Disconnected,
                                statsCollector.Requests / t.Elapsed.TotalSeconds,
                                TimeSpan.FromTicks(statsCollector.RequestTicks).TotalMilliseconds / statsCollector.Requests
                                );
                            var currentRequests = new int[workerCount];
                            workerRequests.CopyTo(currentRequests, 0);
                            for(var i = 0; i < workerCount; i++) {
                                if(currentRequests[i] == lastRequests[i]) {
                                    Console.WriteLine("worker {0} has not made a new request ({1}) and is stuck at '{2}'",
                                        i,
                                        currentRequests[i],
                                        workerStatus[i]
                                    );
                                }
                            }
                            lastRequests = currentRequests;
                        }
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                    }
                });
                Task.WaitAny(workers.ToArray());
                foreach(var worker in workers) {
                    if(worker.IsFaulted) {
                        foreach(var exception in worker.Exception.Flatten().InnerExceptions)
                            Console.WriteLine(exception);
                    }
                }
                t.Stop();
            }
        }

        public class FaultingSyncClientHandlerFactory : IClientHandlerFactory {
            private readonly ISyncCommandDispatcher _dispatcher;
            private readonly int _faultInterval;
            private int _requestsTilFault;
            private int _faults;

            public FaultingSyncClientHandlerFactory(ISyncCommandDispatcher dispatcher, int faultInterval) {
                _dispatcher = dispatcher;
                _requestsTilFault = _faultInterval = faultInterval;
            }

            public int Faults { get { return _faults; } }

            public IClientHandler Create(Socket socket, IStatsCollector statsCollector, Action<IClientHandler> removeHandler) {
                return new FaultingSyncClientHandler(socket, _dispatcher, statsCollector, removeHandler, CheckForFault);
            }

            private bool CheckForFault() {
                var fault = Interlocked.Decrement(ref _requestsTilFault);
                if(fault == 0) {
                    Interlocked.Increment(ref _faults);
                    Interlocked.Exchange(ref _requestsTilFault, _faultInterval);
                    return true;
                }
                return false;
            }
        }

        public class FaultingSyncClientHandler : SyncClientHandler {
            private readonly Func<bool> _checkForFault;

            public FaultingSyncClientHandler(Socket socket, ISyncCommandDispatcher dispatcher, IStatsCollector statsCollector, Action<IClientHandler> removeCallback, Func<bool> checkForFault)
                : base(socket, dispatcher, statsCollector, removeCallback) {
                _checkForFault = checkForFault;
            }

            protected override void Receive(Action<int, int> continuation) {
                if(_checkForFault()) {
                    Console.WriteLine("faulting request");
                    _socket.Close(0);
                    Dispose();
                    return;
                }
                base.Receive(continuation);
            }
        }

        private class StatsCollector : IStatsCollector {
            public int Connected;
            public int Disconnected;
            public int Requests;
            public long RequestTicks;

            public void ClientConnected(IPEndPoint remoteEndPoint) {
                Interlocked.Increment(ref Connected);
            }

            public void ClientDisconnected(IPEndPoint endPoint) {
                Interlocked.Increment(ref Disconnected);
            }

            public void CommandCompleted(IPEndPoint endPoint, StatsCommandInfo info) {
                Interlocked.Increment(ref Requests);
                Interlocked.Add(ref RequestTicks, info.Elapsed.Ticks);
            }

            public void CommandStarted(IPEndPoint endPoint, ulong id) {
            }
        }
    }
}
