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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MindTouch.Clacks.Server {
    public abstract class AClientRequestHandler : IDisposable, IClientHandler {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly Action<IClientHandler> _removeCallback;
        private readonly Guid _clientId;
        private readonly Stopwatch _requestTimer = new Stopwatch();
        private readonly IClacksInstrumentation _instrumentation;

        protected readonly Socket _socket;
        protected readonly IPEndPoint _endPoint;
        protected readonly StringBuilder _commandBuffer = new StringBuilder();
        protected readonly byte[] _buffer = new byte[16 * 1024];

        private ulong _commandCounter;
        private string[] _commandArgs;
        private bool _isDisposed;
        private bool _inCommand = false;

        protected int _bufferPosition;
        protected int _bufferDataLength;
        protected bool _carriageReturn;

        protected AClientRequestHandler(Guid clientId, Socket socket, IClacksInstrumentation instrumentation, Action<IClientHandler> removeCallback) {
            _clientId = clientId;
            _socket = socket;
            _endPoint = _socket.RemoteEndPoint as IPEndPoint;
            _instrumentation = instrumentation;
            _removeCallback = removeCallback;
            _instrumentation.ClientConnected(_clientId, _endPoint);
        }

        public Guid Id { get { return _clientId; } }
        public IPEndPoint EndPoint { get { return _endPoint; } }

        protected abstract ICommandHandler Handler { get; }
        protected bool IsDisposed { get { return _isDisposed; } }

        /* WorkFlow
          * 1. If data in buffer go to 4.
          * 2. Wait for command data
          * 3. Receive command data
          * 4. Build command from data
          * 5. If not enough data for command, go to 2.
          * 6. Get command handler
          * 7. If command doesn't expect data go to 13.
          * 8. If data in buffer go to 11.
          * 9. Wait for payload data
          * 10. Receive payload data
          * 11. Give data to handler
          * 12. If less payload than expected go to 9.
          * 13. End command request phase
          * 14. Get response data from handler
          * 15. If no response data go to 1.
          * 16. Begin send response data
          * 17. Complete send response data
          * 18. go to 14.
          */

        // 2&3, 9&10, Receive buffer trail
        protected abstract void Receive(Action<int, int> continuation);
        protected abstract string InitializeHandler(string[] command);

        public abstract void ProcessRequests();
        protected abstract void CompleteRequest();

        // 1.
        protected void StartCommandRequest() {
            _commandCounter++;
            if(_bufferDataLength != 0) {
                ProcessCommandData(_bufferPosition, _bufferDataLength);
                return;
            }
            if(!CheckSocket()) {
                Dispose();
                return;
            }
            Receive(ProcessCommandData);
        }

        private bool CheckSocket() {
            if(!_socket.Connected) {
                return false;
            }
            return !(_socket.Poll(10, SelectMode.SelectRead) && _socket.Available == 0);

        }

        // 4.
        protected void ProcessCommandData(int position, int length) {
            if(!_inCommand) {
                _inCommand = true;
                _requestTimer.Start();
                _instrumentation.AwaitingCommand(Id, _commandCounter);
            }

            // look for \r\n
            for(var i = 0; i < length; i++) {
                var idx = position + i;
                if(_buffer[idx] == '\r') {
                    _carriageReturn = true;
                } else if(_carriageReturn && _buffer[idx] == '\n') {
                    _carriageReturn = false;
                    _bufferPosition = idx + 1;
                    _bufferDataLength = length - i - 1;
                    _commandBuffer.Append(Encoding.ASCII.GetString(_buffer, position, idx - 1));

                    // 6.
                    _commandArgs = _commandBuffer.ToString().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    _commandBuffer.Length = 0;
                    var command = InitializeHandler(_commandArgs);
                    _log.DebugFormat("[{0}] Received command [{1}] in {2:0.00}ms, expect data: {3} ",
                        _commandCounter,
                        command,
                        _requestTimer.Elapsed.TotalMilliseconds,
                        Handler.ExpectsData
                    );
                    _instrumentation.ReceivedCommand(new StatsCommandInfo(Id, _commandCounter, _requestTimer.Elapsed, _commandArgs, null));
                    if(Handler.ExpectsData) {

                        // 8.
                        if(_bufferPosition != 0) {
                            ProcessPayloadData(_bufferPosition, _bufferDataLength);
                            return;
                        }
                        Receive(ProcessPayloadData);
                    } else {

                        // 7.
                        _instrumentation.ReceivedCommandPayload(new StatsCommandInfo(Id, _commandCounter, _requestTimer.Elapsed, _commandArgs, null));
                        ProcessCommand();
                    }
                    return;
                }
            }
            _commandBuffer.Append(Encoding.ASCII.GetString(_buffer, position, length));

            // 5.
            Receive(ProcessCommandData);
        }

        // 11.
        protected void ProcessPayloadData(int position, int length) {

            var outstanding = Handler.OutstandingBytes;
            var payloadLength = Math.Min(length, outstanding);
            var payload = new byte[payloadLength];
            Array.Copy(_buffer, position, payload, 0, payloadLength);
            Handler.AcceptData(payload);
            if(Handler.OutstandingBytes == 0) {

                // check and consume trailing \r\n
                if(length < payloadLength + 2) {

                    // missing trailing \r\n, let's try to receive those before moving on
                    Receive(CheckBufferTail);
                    return;
                }
                CheckBufferTail(position + outstanding, length - outstanding);
                return;
            }
            Receive(ProcessPayloadData);
        }

        protected void CheckBufferTail(int position, int length) {
            if(_buffer[position] != '\r' || _buffer[position + 1] != '\n') {
                throw new DataTerminatorMissingException();
            }
            _bufferDataLength = length - 2;
            _bufferPosition = _bufferDataLength == 0 ? 0 : position + 2;
            _instrumentation.ReceivedCommandPayload(new StatsCommandInfo(Id, _commandCounter, _requestTimer.Elapsed, _commandArgs, null));
            ProcessCommand();
        }

        /// <summary>
        /// 13/14. -> Calls StartCommandRequest
        /// </summary>
        protected abstract void ProcessCommand();

        protected void PrepareResponse(string status) {
            _instrumentation.ProcessedCommand(new StatsCommandInfo(Id, _commandCounter, _requestTimer.Elapsed, _commandArgs, status));
            SendResponse();
        }

        protected abstract void SendResponse();

        protected void EndCommandRequest(string status) {
            _requestTimer.Stop();
            _log.DebugFormat("[{0}] [{1}] excecuted with status [{2}] in {3:0.00}ms",
                _commandCounter,
                Handler.Command,
                status,
                _requestTimer.Elapsed.TotalMilliseconds
            );
            _instrumentation.CommandCompleted(new StatsCommandInfo(Id, _commandCounter, _requestTimer.Elapsed, _commandArgs, status));
            _requestTimer.Reset();
            _inCommand = false;
            CompleteRequest();
        }

        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            _isDisposed = true;
            _log.DebugFormat("Disposing client '{0}'", EndPoint);
            _instrumentation.ClientDisconnected(Id, EndPoint);
            try {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            } catch { }
            _removeCallback(this);
        }

        protected void FailAndDispose(string reason, Exception e) {
            if(_isDisposed) {
                return;
            }
            _isDisposed = true;
            _log.Warn(string.Format("Disposing client '{0}' due to failure: {1}", EndPoint, reason), e);
            _instrumentation.ClientDisconnected(Id, EndPoint);
            try {

                // we want to make sure that the client sees the failure and doesn't hang out hoping for the request to finish
                _socket.LingerState = new LingerOption(true, 0);
                _socket.Close();
                _socket.Dispose();
            } catch { }
            _removeCallback(this);
        }
    }
}
