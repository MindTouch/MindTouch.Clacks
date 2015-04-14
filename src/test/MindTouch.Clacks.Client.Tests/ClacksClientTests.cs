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
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MindTouch.Clacks.Client.Net;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class ClacksClientTests {

        private FakeSocketFactory _factory;

        [SetUp]
        public void Setup() {
            _factory = new FakeSocketFactory();
        }

        [Test]
        public void Creating_client_does_not_get_socket_from_pool() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var client = new ClacksClient(pool);

            // Assert
            Assert.AreEqual(0, _factory.Sockets.Count);
        }

        [Test]
        public void Making_client_request_gets_socket_from_pool() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            var socket = new FakeSocket {
                ReceiveCallback = (buffer, size, offset) => {
                    var bytes = Encoding.ASCII.GetBytes("OK\r\n");
                    Array.Copy(bytes, buffer, bytes.Length);
                    return bytes.Length;
                }
            };
            _factory.Builder = () => socket;

            // Act
            var client = new ClacksClient(pool);
            client.Exec(Request.Create("HI"));

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
        }

        [Test]
        public void Disposing_client_returns_socket_to_pool() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var client = new ClacksClient(pool);
            client.Dispose();
            var s = pool.GetSocket();

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
        }

        [Test]
        public void Reconnecting_client_retries_request_on_failure() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            var failSocket = new FakeSocket();
            failSocket.ReceiveCallback = (buffer, size, offset) => {
                failSocket.Connected = false;
                throw new SocketException(42);
            };
            var successSocket = new FakeSocket {
                ReceiveCallback = (buffer, size, offset) => {
                    var bytes = Encoding.ASCII.GetBytes("OK\r\n");
                    Array.Copy(bytes, buffer, bytes.Length);
                    return bytes.Length;
                }
            };
            _factory.Builder = () => _factory.Sockets.Any() ? successSocket : failSocket;

            // Act
            var client = new ClacksClient(pool);
            var response = client.Exec(Request.Create("HI"));

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
            Assert.AreEqual(1, failSocket.ReceiveCalled);
            Assert.AreEqual(1, successSocket.ReceiveCalled);
            Assert.AreEqual("OK", response.Status);
        }

        [Test]
        public void Reconnecting_client_retries_request_only_once_on_failure() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            var failSocket = new FakeSocket();
            failSocket.ReceiveCallback = (buffer, size, offset) => {
                failSocket.Connected = false;
                throw new SocketException(42);
            };
            var successSocket = new FakeSocket {
                ReceiveCallback = (buffer, size, offset) => {
                    var bytes = Encoding.ASCII.GetBytes("OK\r\n");
                    Array.Copy(bytes, buffer, bytes.Length);
                    return bytes.Length;
                }
            };
            _factory.Builder = () => _factory.Sockets.Count < 2 ? failSocket : successSocket;

            // Act
            var client = new ClacksClient(pool);
            try {
                client.Exec(Request.Create("HI"));
                Assert.Fail("call should not have succeeded");
            } catch { }

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
            Assert.AreEqual(1, failSocket.ReceiveCalled);
            Assert.AreEqual(0, successSocket.ReceiveCalled);
        }

        [Test]
        public void Reconnecting_client_only_issues_request_once_if_not_failing() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            var successSocket = new FakeSocket {
                ReceiveCallback = (buffer, size, offset) => {
                    var bytes = Encoding.ASCII.GetBytes("OK\r\n");
                    Array.Copy(bytes, buffer, bytes.Length);
                    return bytes.Length;
                }
            };
            _factory.Builder = () => successSocket;

            // Act
            var client = new ClacksClient(pool);
            var response = client.Exec(Request.Create("HI"));

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            Assert.AreEqual(1, successSocket.ReceiveCalled);
            Assert.AreEqual("OK", response.Status);
        }

        [Test]
        public void Non_reconnecting_client_does_not_retry_failed_request() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            var failSocket = new FakeSocket();
            failSocket.ReceiveCallback = (buffer, size, offset) => {
                failSocket.Connected = false;
                throw new SocketException(42);
            };
            var successSocket = new FakeSocket {
                ReceiveCallback = (buffer, size, offset) => {
                    var bytes = Encoding.ASCII.GetBytes("OK\r\n");
                    Array.Copy(bytes, buffer, bytes.Length);
                    return bytes.Length;
                }
            };
            _factory.Builder = () => _factory.Sockets.Any() ? successSocket : failSocket;

            // Act
            var client = new ClacksClient(pool, attemptReconnect: false);
            try {
                client.Exec(Request.Create("HI"));
                Assert.Fail("call should not have succeeded");
            } catch { }

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            Assert.AreEqual(1, failSocket.ReceiveCalled);
            Assert.AreEqual(0, successSocket.ReceiveCalled);
        }

        [Test]
        public void Non_reconnecting_client_only_issues_request_once_if_not_failing() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            var successSocket = new FakeSocket {
                ReceiveCallback = (buffer, size, offset) => {
                    var bytes = Encoding.ASCII.GetBytes("OK\r\n");
                    Array.Copy(bytes, buffer, bytes.Length);
                    return bytes.Length;
                }
            };
            _factory.Builder = () => successSocket;

            // Act
            var client = new ClacksClient(pool, attemptReconnect: false);
            var response = client.Exec(Request.Create("HI"));

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            Assert.AreEqual(1, successSocket.ReceiveCalled);
            Assert.AreEqual("OK", response.Status);
        }

    }
}