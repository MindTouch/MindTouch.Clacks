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

using System.Linq;
using System.Net.Sockets;
using MindTouch.Clacks.Client.Net;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [TestFixture]
    public class ConnectionPoolTests {
        private FakeSocketFactory _factory;

        [SetUp]
        public void Setup() {
            _factory = new FakeSocketFactory();
        }

        [Test]
        public void Disposing_pool_socket_does_not_dispose_underlying_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var s1 = pool.GetSocket();
            s1.Dispose();

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            Assert.AreNotSame(_factory.Sockets[0], s1);
            Assert.AreEqual(0, _factory.Sockets[0].DisposeCalled);
        }

        [Test]
        public void Getting_sockets_sequentially_will_not_create_a_new_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var s1 = pool.GetSocket();
            s1.Dispose();
            var s2 = pool.GetSocket();

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
        }

        [Test]
        public void Getting_sockets_in_parallel_will_create_a_new_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var s1 = pool.GetSocket();
            var s2 = pool.GetSocket();

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
        }

        [Test]
        public void Getting_sockets_in_sequence_when_the_first_has_closed_will_create_a_new_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var s1 = pool.GetSocket();
            s1.Dispose();
            _factory.Sockets[0].Connected = false;
            var s2 = pool.GetSocket();

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
        }

        [Test]
        public void Getting_global_pool_via_ip_will_return_same_instance() {

            // Act
            var p1 = ConnectionPool.GetPool("127.0.0.1", 1234);
            var p2 = ConnectionPool.GetPool("127.0.0.1", 1234);

            // Assert
            Assert.AreSame(p1, p2);
        }

        [Test]
        public void Pool_in_reconnect_mode_will_not_call_Connected_on_socket() {

            // Arrange
            var p = new ConnectionPool(_factory.Create, ConnectionTestingBehavior.Reconnect);
            var s = p.GetSocket();
            var f = _factory.Sockets.First();

            // Act
            s.Dispose();
            s = p.GetSocket();

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count, "created more than one socket in pool");
            Assert.AreEqual(0, f.ConnectedCalled, "connected was called");
        }

        [Test]
        public void Pool_in_reconnect_mode_will_reconnect_once_per_send_failure() {

            // Arrange
            _factory.Builder = () => new FakeSocket { SendCallback = () => { throw new SocketException(); } };
            var pool = new ConnectionPool(_factory.Create, ConnectionTestingBehavior.Reconnect);
            var socket = pool.GetSocket();

            // Act
            try {
                socket.Send(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
            var f1 = _factory.Sockets.ElementAt(0);
            Assert.IsTrue(f1.IsDisposed, "first failed socket did not get disposed");
            Assert.AreEqual(1, f1.SendCalled, "send called wrong number of times on first socket");
            var f2 = _factory.Sockets.ElementAt(1);
            Assert.IsTrue(f2.IsDisposed, "second failed socket did not get disposed");
            Assert.AreEqual(1, f2.SendCalled, "send called wrong number of times on second socket");
        }

        [Test]
        public void Pool_in_reconnect_mode_will_reconnect_once_per_receive_failure() {

            // Arrange
            _factory.Builder = () => new FakeSocket { ReceiveCallback = () => { throw new SocketException(); } };
            var pool = new ConnectionPool(_factory.Create, ConnectionTestingBehavior.Reconnect);
            var socket = pool.GetSocket();

            // Act
            try {
                socket.Receive(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
            var f1 = _factory.Sockets.ElementAt(0);
            Assert.IsTrue(f1.IsDisposed, "first failed socket did not get disposed");
            Assert.AreEqual(1, f1.ReceiveCalled, "receive called wrong number of times on first socket");
            var f2 = _factory.Sockets.ElementAt(1);
            Assert.IsTrue(f2.IsDisposed, "second failed socket did not get disposed");
            Assert.AreEqual(1, f2.ReceiveCalled, "receive called wrong number of times on second socket");
        }

        [Test]
        public void Pool_in_poll_mode_will_call_Connected_on_socket() {

            // Arrange
            var p = new ConnectionPool(_factory.Create, ConnectionTestingBehavior.Poll);
            var s = p.GetSocket();
            var f = _factory.Sockets.First();

            // Act
            s.Dispose();
            s = p.GetSocket();

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count, "created more than one socket in pool");
            Assert.AreEqual(2, f.ConnectedCalled, "connected was called");
        }

        [Test]
        public void Pool_in_poll_mode_will_not_reconnect_on_send_failure() {

            // Arrange
            _factory.Builder = () => new FakeSocket { SendCallback = () => { throw new SocketException(); } };
            var pool = new ConnectionPool(_factory.Create, ConnectionTestingBehavior.Poll);
            var socket = pool.GetSocket();

            // Act
            try {
                socket.Send(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            var f1 = _factory.Sockets.First();
            Assert.IsTrue(f1.IsDisposed, "first failed socket did not get disposed");
            Assert.AreEqual(1, f1.SendCalled, "send called wrong number of times on first socket");
        }

        [Test]
        public void Pool_in_poll_mode_will_not_reconnect_on_receive_failure() {

            // Arrange
            _factory.Builder = () => new FakeSocket { ReceiveCallback = () => { throw new SocketException(); } };
            var pool = new ConnectionPool(_factory.Create, ConnectionTestingBehavior.Poll);
            var socket = pool.GetSocket();

            // Act
            try {
                socket.Receive(new byte[0], 0, 0);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            var f1 = _factory.Sockets.First();
            Assert.IsTrue(f1.IsDisposed, "first failed socket did not get disposed");
            Assert.AreEqual(1, f1.ReceiveCalled, "receive called wrong number of times on first socket");
        }

    }
}