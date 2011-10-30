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
using System.Net.Sockets;
using System.Text;

namespace MindTouch.Arpysee.Server {
    public abstract class AClientRequestHandler : IDisposable, IClientHandler {

        private static readonly Logger.ILog _log = Logger.CreateLog();

        private readonly Action<IClientHandler> _removeCallback;
        private readonly EndPoint _remote;

        protected readonly Socket _socket;
        protected readonly ICommandDispatcher _dispatcher;
        protected readonly StringBuilder _commandBuffer = new StringBuilder();
        protected readonly byte[] _buffer = new byte[16 * 1024];
        private readonly Stopwatch _requestTimer = new Stopwatch();

        private ulong _commandCounter;
        private bool _isDisposed;

        protected int _bufferPosition;
        protected int _bufferDataLength;
        protected bool _carriageReturn;
        protected ICommandHandler _handler;


        protected AClientRequestHandler(Socket socket, ICommandDispatcher dispatcher, Action<IClientHandler> removeCallback) {
            _socket = socket;
            _remote = _socket.RemoteEndPoint;
            _dispatcher = dispatcher;
            _removeCallback = removeCallback;
        }

        public void ProcessRequests() {
            try {
                StartCommandRequest();
            } catch(Exception e) {
                _log.Warn("starting request processing failed", e);
            }
        }

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

        // 1.
        private void StartCommandRequest() {
            _commandCounter++;
            _requestTimer.Start();
            if(_bufferPosition != 0) {
                ProcessCommandData(_bufferPosition, _bufferDataLength);
                return;
            }
            Receive(ProcessCommandData);
        }

        protected void EndCommandRequest(string status) {
            _requestTimer.Stop();
            _log.DebugFormat("[{0}] [{1}] excecuted with status [{2}] in {3:0.00}ms",
                _commandCounter,
                _handler.Command,
                status,
                _requestTimer.Elapsed.TotalMilliseconds
            );
            _requestTimer.Reset();
            if(_handler.DisconnectOnCompletion) {
                Dispose();
            } else {
                StartCommandRequest();
            }
        }

        // 4.
        protected void ProcessCommandData(int position, int length) {

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
                    var command = _commandBuffer.ToString().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    _commandBuffer.Length = 0;
                    _handler = _dispatcher.GetHandler(command);
                    _log.DebugFormat("[{0}] Received command [{1}] in {2:0.00}ms, expect data: {3} ",
                        _commandCounter,
                        command[0],
                        _requestTimer.Elapsed.TotalMilliseconds,
                        _handler.ExpectsData
                    );
                    if(_handler.ExpectsData) {

                        // 8.
                        if(_bufferPosition != 0) {
                            ProcessPayloadData(_bufferPosition, _bufferDataLength);
                        }
                        Receive(ProcessPayloadData);
                    } else {

                        // 7.
                        ProcessResponse();
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

            var outstanding = _handler.OutstandingBytes;
            var payloadLength = Math.Min(length, outstanding);
            var payload = new byte[payloadLength];
            Array.Copy(_buffer, position, payload, 0, payloadLength);
            _handler.AcceptData(payload);
            if(_handler.OutstandingBytes == 0) {

                // check and consume trailing \r\n
                if(length < payloadLength + 2) {

                    // missing trailing \r\n, let's try to receive those before moving on
                    Receive(CheckBufferTrail);
                    return;
                }
                CheckBufferTrail(position + outstanding, length - outstanding);
                return;
            }
            Receive(ProcessPayloadData);
        }

        protected void CheckBufferTrail(int position, int length) {
            if(_buffer[position] != '\r' || _buffer[position + 1] != '\n') {
                throw new DataTerminatorMissingException();
            }
            _bufferDataLength = length - 2;
            _bufferPosition = _bufferDataLength == 0 ? 0 : position + 2;
            ProcessResponse();
        }

        /// <summary>
        /// 13/14. -> Calls StartCommandRequest
        /// </summary>
        protected abstract void ProcessResponse();

        public void Dispose() {
            if(_isDisposed) {
                return;
            }
            _log.DebugFormat("Disposing client from {0}", _remote);
            try {
                _socket.Close();
            } catch { }
            _removeCallback(this);
        }
    }
}
