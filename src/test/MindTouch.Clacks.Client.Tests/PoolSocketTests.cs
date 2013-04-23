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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MindTouch.Clacks.Client.Net;
using MindTouch.Clacks.Client.Net.Helper;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [TestFixture]
    public class PoolSocketTests {

        private int _port;
        private Socket _connectedSocket;
        private Socket _listenSocket;

        [SetUp]
        public void Setup() {
            _port = new Random().Next(1000, 30000);
        }

        [Test]
        public void On_Send_failure_will_call_reconnect_callback_and_retry() {

            // Arrange
            var fakesocket = new FakeSocket();
            fakesocket.SendCallback = () => {
                fakesocket.SendCallback = () => { };
                throw new SocketException();
            };
            var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => f };
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim, callbacks.Reconnect);

            // Act
            poolsocket.Send(new byte[0], 0, 0);

            // Assert
            Assert.AreEqual(2, fakesocket.SendCalled, "send called wrong number of times");
            Assert.AreEqual(1, callbacks.ReconnectCalled, "reconnect called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.FailedSocket);
        }

        [Test]
        public void On_Send_failure_will_call_reconnect_callback_and_retry_once() {

            // Arrange
            var fakesocket = new FakeSocket { SendCallback = () => { throw new SocketException(); } };
            var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => f };
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim, callbacks.Reconnect);

            // Act
            try {
                poolsocket.Send(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }
            // Assert
            Assert.AreEqual(2, fakesocket.SendCalled, "send called wrong number of times");
            Assert.AreEqual(1, callbacks.ReconnectCalled, "reconnect called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.FailedSocket);
        }

        [Test]
        public void Server_closing_socket_during_receive_is_reconnectable_fault() {
            ISocket socket = null;
            try {
                var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port);
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                _listenSocket.Bind(endpoint);
                _listenSocket.Listen(10);
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
                Console.WriteLine("connecting");
                socket = SocketAdapter.Open(endpoint);
                Console.WriteLine("connected");
                var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => f };
                var poolsocket = new PoolSocket(socket, callbacks.Reclaim, callbacks.Reconnect);
                try {
                    Console.WriteLine("writing to pool socket");
                    poolsocket.Send(new byte[]{1,2,3,4,5}, 0, 5);
                    poolsocket.Receive(new byte[5], 0, 5);
                } catch(Exception) {
                    Assert.AreEqual(1, callbacks.ReconnectCalled, "reconnect called wrong number of times");
                    return;
                }
                Assert.Fail("didn't throw");
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
            Console.WriteLine("socket connected");
            _connectedSocket = _listenSocket.EndAccept(ar);
            Console.WriteLine("waiting for data");
            _connectedSocket.Receive(new byte[10], 0, 1, SocketFlags.None);
            Console.WriteLine("received some, closing socket");
            _connectedSocket.Close(0);
            _listenSocket.BeginAccept(OnAccept, _listenSocket);
        }


        [Test]
        public void On_Receive_failure_will_call_reconnect_callback_and_retry() {

            // Arrange
            var fakesocket = new FakeSocket();
            fakesocket.ReceiveCallback = () => {
                fakesocket.ReceiveCallback = () => { };
                throw new SocketException();
            };
            var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => f };
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim, callbacks.Reconnect);

            // Act
            poolsocket.Receive(new byte[0], 0, 0);

            // Assert
            Assert.AreEqual(2, fakesocket.ReceiveCalled, "send called wrong number of times");
            Assert.AreEqual(1, callbacks.ReconnectCalled, "reconnect called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.FailedSocket);
        }

        [Test]
        public void On_Receive_failure_will_call_reconnect_callback_and_retry_once() {

            // Arrange
            var fakesocket = new FakeSocket { ReceiveCallback = () => { throw new SocketException(); } };
            var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => f };
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim, callbacks.Reconnect);

            // Act
            try {
                poolsocket.Receive(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }
            // Assert
            Assert.AreEqual(2, fakesocket.ReceiveCalled, "send called wrong number of times");
            Assert.AreEqual(1, callbacks.ReconnectCalled, "reconnect called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.FailedSocket);
        }


        [Test]
        public void If_reconnect_returns_null_no_retry_will_be_attempted() {

            // Arrange
            var fakesocket = new FakeSocket { SendCallback = () => { throw new SocketException(); } };
            var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => null };
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim, callbacks.Reconnect);

            // Act
            try {
                poolsocket.Send(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }
            // Assert
            Assert.AreEqual(1, fakesocket.SendCalled, "send called wrong number of times");
            Assert.AreEqual(1, callbacks.ReconnectCalled, "reconnect called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.FailedSocket);
        }



        [Test]
        public void On_failure_PoolSocket_and_underlying_socket_are_disposed_and_reclaimed() {

            // Arrange
            var fakesocket = new FakeSocket { ReceiveCallback = () => { throw new SocketException(); } };
            var callbacks = new PoolSocketCallbacks { ReconnectCallback = (p, f) => null };
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim, callbacks.Reconnect);

            // Act
            try {
                poolsocket.Receive(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }
            // Assert
            Assert.AreEqual(1, fakesocket.ReceiveCalled, "send called wrong number of times");
            Assert.IsTrue(fakesocket.IsDisposed, "underlying socket wasn't disposed");
            Assert.IsTrue(poolsocket.IsDisposed, "pool socket wasn't disposed");
            Assert.AreEqual(1, callbacks.ReclaimCalled, "reclaim was called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.ReclaimedSocket, "underlying socket wasn't the one reclaimed");
            Assert.AreSame(fakesocket, callbacks.FailedSocket);
        }

    }
}