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

    public class WorkerInfo {
        public string Id;
        public WorkerStatus Status;
        public int Requests;
        public Task Task;
    }

    class Program {
        static void Main(string[] args) {
            var workerCount = 50;
            var workerPrefix = "w";
            var faultInterval = 100000;
            var sleepInterval = 50;
            var port = new Random().Next(1000, 30000);
            var host = "127.0.0.1";
            var runServer = true;
            var options = new Options {
                {"w=", "Workers (default: 50)", x => workerCount = int.Parse(x)},
                {"P=", "Worker Prefix (default: w)", x => workerPrefix = x},
                {"f=", "Fault Interval (default: every 100000)", x => faultInterval = int.Parse(x)},
                {"s=", "Sleep Interval (default: 50ms)", x => sleepInterval = int.Parse(x)},
                {"X", "Disable Server (enabled by default)", x => runServer = x == null},
                {"p=", "Server port (default: random)", x=> port = int.Parse(x)},
                {"h=", "Server host (default: 127.0.0.1)", x=> host = x}
            };
            options.Parse(args).ToArray();
            Console.WriteLine("Server: {0}:{1}", host, port);
            var tasks = new List<Task>();
            if(runServer) {
                Console.WriteLine("starting server");
                tasks.Add(RunServer(host, port, faultInterval));
            }

            var workers = new List<WorkerInfo>(workerCount);
            if(workerCount > 0) {
                Console.WriteLine("Starting {0} workers with a {1}ms sleep interval", workerCount, sleepInterval);
                var startSignal = new ManualResetEvent(false);
                var readySignals = new List<WaitHandle>();
                var pool = ConnectionPool.Create(host, port);
                var poolUseCount = 0;
                for(var i = 0; i < workerCount; i++) {
                    var ready = new ManualResetEvent(false);
                    readySignals.Add(ready);
                    var myPool = pool;
                    var workerInfo = new WorkerInfo { Id = workerPrefix + i.ToString("000"), Status = WorkerStatus.PreClient };
                    workers.Add(workerInfo);
                    workerInfo.Task = Task.Factory.StartNew(() => {
                        var r = new Random();
                        ready.Set();
                        startSignal.WaitOne();
                        var localUseCount = 0;
                        var maxPoolUse = r.Next(50, 150);
                        while(true) {
                            workerInfo.Status = WorkerStatus.CheckingPool;
                            localUseCount++;
                            var globalUseCount = Interlocked.Increment(ref poolUseCount);
                            if(globalUseCount > r.Next(200, 300)) {
                                Console.WriteLine("{0}: creating new pool", workerInfo.Id);
                                Interlocked.Exchange(ref poolUseCount, 0);
                                pool = ConnectionPool.Create("127.0.0.1", port);
                                myPool = pool;
                            } else if(myPool != pool && localUseCount > maxPoolUse) {
                                Console.WriteLine("{0}: picking new pool", workerInfo.Id);
                                myPool = pool;
                                localUseCount = 0;
                            }
                            using(var client = new ClacksClient(myPool)) {
                                for(var k = 0; k < 1000; k++) {
                                    workerInfo.Status = WorkerStatus.GeneratingPayload;
                                    var payload = new StringBuilder();
                                    payload.AppendFormat("id:{0},data:", workerInfo.Id);
                                    var size = r.Next(2, 100);
                                    for(var l = 0; l < size; l++) {
                                        payload.Append(Guid.NewGuid().ToString());
                                    }
                                    var data = payload.ToString();
                                    var bytes = Encoding.ASCII.GetBytes(data);
                                    workerInfo.Status = WorkerStatus.MakingRequest;
                                    var response = client.Exec(new Client.Request("BIN").WithArgument(workerInfo.Id).WithData(bytes).ExpectData("OK"));
                                    workerInfo.Status = WorkerStatus.CheckingResponse;
                                    if(response.Status != "OK") {
                                        throw new Exception("wrong status: " + response.Status);
                                    }
                                    if(response.Arguments.Length != 1) {
                                        throw new Exception("wrong arg length: " + response.Arguments.Length);
                                    }
                                    var responseData = Encoding.ASCII.GetString(response.Data);
                                    if(data != responseData) {
                                        lock(workers) {
                                            Console.WriteLine("bad data response for worker {0}", workerInfo.Id);
                                            Console.WriteLine("sent:     {0}", data);
                                            Console.WriteLine("received: {0}", responseData);
                                        }
                                    }
                                    Interlocked.Increment(ref workerInfo.Requests);
                                    workerInfo.Status = WorkerStatus.Sleeping;
                                    Thread.Sleep(sleepInterval);
                                }
                            }
                        }
                    });
                    tasks.Add(workerInfo.Task);
                }
                Console.WriteLine("waiting for workers to get ready");
                WaitHandle.WaitAll(readySignals.ToArray());
                startSignal.Set();
                Console.WriteLine("starting work");
            }
            var allTasks = tasks.ToArray();
            Thread.Sleep(TimeSpan.FromMinutes(1));
            var lastRequests = workers.ToDictionary(k => k.Id, v => v.Requests);
            while(true) {
                foreach(var worker in workers) {
                    if(worker.Requests == lastRequests[worker.Id]) {
                        Console.WriteLine("worker {0} has not made a new request ({1}) and is stuck at state '{2}'",
                            worker.Id,
                            worker.Requests,
                            worker.Status
                        );
                    }
                    lastRequests[worker.Id] = worker.Requests;
                }
                var faulted = false;
                foreach(var t in tasks) {
                    if(t.IsFaulted) {
                        faulted = true;
                        foreach(var exception in t.Exception.Flatten().InnerExceptions) {
                            Console.WriteLine(exception);
                        }
                    }
                }
                if(faulted) {
                    return;
                }
                Thread.Sleep(TimeSpan.FromMinutes(1));
            }
        }


        private static Task RunServer(string host, int port, int faultInterval) {
            return Task.Factory.StartNew(() => {
                var t = Stopwatch.StartNew();
                var instrumentation = new ClacksInstrumentation();
                var dispatcher = new SyncCommandRepository();
                dispatcher.AddCommand("BIN", request => Response.Create("OK").WithData(request.Data), DataExpectation.Auto);
                var clientHandlerFactory = new FaultingSyncClientHandlerFactory(dispatcher, faultInterval);
                using(new ClacksServer(new IPEndPoint(IPAddress.Parse(host), port), instrumentation, clientHandlerFactory)) {
                    Console.WriteLine("created server");
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    while(true) {
                        lock(instrumentation.Connections) {
                            Console.WriteLine("{0} Processed {1} requests with {2} faults via {3}/{4} total/active connections at {5,6:0} requests/second and {6:0.000}ms/request",
                                DateTime.Now,
                                instrumentation.Requests,
                                clientHandlerFactory.Faults,
                                instrumentation.Connected,
                                instrumentation.Connections.Count,
                                instrumentation.Requests / t.Elapsed.TotalSeconds,
                                TimeSpan.FromTicks(instrumentation.RequestTicks).TotalMilliseconds / instrumentation.Requests
                                );
                            foreach(var connection in instrumentation.Connections.OrderBy(x => x.Value.Id)) {
                                Console.WriteLine("{0}: {1}", connection.Value.Id, connection.Value.Status);
                            }
                        }
                        Thread.Sleep(TimeSpan.FromMinutes(1));
                    }
                }
            });
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

            public IClientHandler Create(Guid clientId, Socket socket, IClacksInstrumentation instrumentation, Action<IClientHandler> removeHandler) {
                return new FaultingSyncClientHandler(clientId, socket, _dispatcher, instrumentation, removeHandler, CheckForFault);
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

            public FaultingSyncClientHandler(Guid clientId, Socket socket, ISyncCommandDispatcher dispatcher, IClacksInstrumentation instrumentation, Action<IClientHandler> removeCallback, Func<bool> checkForFault)
                : base(clientId, socket, dispatcher, instrumentation, removeCallback) {
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

        public enum ConnectionStatus {
            Unknown,
            AwaitCommand,
            Connected,
            CommandCompleted,
            ProcessedCommand,
            ReceivedCommand,
            ReceivedCommandPayload
        }

        public class ConnectionInfo {
            public ConnectionStatus Status;
            public string Id;
        }

        private class ClacksInstrumentation : IClacksInstrumentation {
            public readonly Dictionary<Guid, ConnectionInfo> Connections = new Dictionary<Guid, ConnectionInfo>();
            public int Connected;
            public int Requests;
            public long RequestTicks;


            public void ClientConnected(Guid clientId, IPEndPoint remoteEndPoint) {
                Interlocked.Increment(ref Connected);
                CaptureState(clientId, ConnectionStatus.Connected);
            }

            public void ClientDisconnected(Guid clientId) {
                lock(Connections) {
                    Connections.Remove(clientId);
                }
            }

            public void CommandCompleted(StatsCommandInfo info) {
                CaptureState(info, ConnectionStatus.CommandCompleted);
                Interlocked.Increment(ref Requests);
                Interlocked.Add(ref RequestTicks, info.Elapsed.Ticks);
            }

            public void AwaitingCommand(Guid clientId, ulong requestId) {
                CaptureState(clientId, ConnectionStatus.Connected);
            }
            public void ProcessedCommand(StatsCommandInfo statsCommandInfo) {
                CaptureState(statsCommandInfo, ConnectionStatus.ProcessedCommand);
            }

            public void ReceivedCommand(StatsCommandInfo statsCommandInfo) {
                CaptureState(statsCommandInfo, ConnectionStatus.ReceivedCommand);
            }
            public void ReceivedCommandPayload(StatsCommandInfo statsCommandInfo) {
                CaptureState(statsCommandInfo, ConnectionStatus.ReceivedCommandPayload);
            }

            private void CaptureState(Guid clientId, ConnectionStatus status) {
                lock(Connections) {
                    ConnectionInfo info;
                    if(!Connections.TryGetValue(clientId, out info)) {
                        Connections[clientId] = info = new ConnectionInfo();
                    }
                    info.Id = "NA";
                    info.Status = status;
                }
            }

            private void CaptureState(StatsCommandInfo info, ConnectionStatus status) {
                var id = info.Args[1];
                lock(Connections) {
                    var connection = Connections[info.ClientId];
                    connection.Status = status;
                    connection.Id = id;
                }
            }
        }
    }
}
