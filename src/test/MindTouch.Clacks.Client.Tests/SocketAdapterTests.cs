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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MindTouch.Clacks.Client.Net;
using MindTouch.Clacks.Client.Net.Helper;
using NUnit.Framework;
using log4net;

namespace MindTouch.Clacks.Client.Tests {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class SocketAdapterTests {

        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;
        private Socket _connectedSocket;
        private Socket _listenSocket;
        private ManualResetEvent _connectSignal;

        [SetUp]
        public void Setup() {
            _log.Debug("priming logger");
            _port = new Random().Next(1000, 30000);
            _connectSignal = new ManualResetEvent(false);
        }


        [Test]
        public void Connect_timeout_with_hostport_is_respected() {
            var t = Stopwatch.StartNew();
            try {
                _log.Debug("connecting");
                var socket = SocketAdapter.Open("1.2.3.4", 1234, TimeSpan.FromSeconds(1));
                Assert.Fail("didn't timeout");
            } catch(SocketException) {
                t.Stop();
                Assert.GreaterOrEqual(1200, t.ElapsedMilliseconds);
            }
        }

        [Test]
        public void Connect_timeout_with_endpoint_is_respected() {
            var t = Stopwatch.StartNew();
            try {
                _log.Debug("connecting");
                var socket = SocketAdapter.Open(new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234), TimeSpan.FromSeconds(1));
                Assert.Fail("didn't timeout");
            } catch(SocketException) {
                t.Stop();
                Assert.GreaterOrEqual(1200, t.ElapsedMilliseconds);
            }
        }

        [Test]
        public void Connected_detects_server_closing_socket() {
            ISocket socket = null;
            try {
                var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port);
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                _listenSocket.Bind(endpoint);
                _listenSocket.Listen(10);
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
                socket = SocketAdapter.Open(endpoint);
                Assert.IsTrue(socket.Connected);
                Assert.IsTrue(_connectSignal.WaitOne(5000));
                _connectedSocket.Shutdown(SocketShutdown.Both);
                _connectedSocket.Close();
                Assert.IsFalse(socket.Connected);
            } finally {
                if(_listenSocket != null) {
                    _listenSocket.Dispose();
                }
                if(socket != null) {
                    socket.Dispose();
                }
            }
        }

        [Test]
        public void Receive_timeout_is_respected() {

            // Arrange
            ISocket socket = null;
            try {
                var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port);
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                _listenSocket.Bind(endpoint);
                _listenSocket.Listen(10);
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
                socket = SocketAdapter.Open(endpoint, receiveTimeout: 200);
                Assert.IsTrue(socket.Connected);
                Assert.IsTrue(_connectSignal.WaitOne(5000));

                // Act
                try {
                    var buffer = new byte[4096];
                    socket.Receive(buffer, 0, 10, false);
                } catch(SocketException) {
                    return;
                }
                Assert.Fail("did not throw a socket exception");
            } finally {
                if(_listenSocket != null) {
                    _listenSocket.Dispose();
                }
                if(socket != null) {
                    socket.Dispose();
                }
            }
        }

        private void OnAccept(IAsyncResult ar) {
            _connectedSocket = _listenSocket.EndAccept(ar);
            _connectSignal.Set();
        }
    }
}
