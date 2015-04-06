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
using System.Threading;
using MindTouch.Clacks.Client.Net;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [ExcludeFromCodeCoverage]
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
        public void Getting_sockets_quickly_in_sequence_when_the_first_has_closed_will_return_closed_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var s1 = pool.GetSocket();
            s1.Dispose();
            _factory.Sockets[0].Connected = false;
            var s2 = pool.GetSocket();

            // Assert
            Assert.AreEqual(1, _factory.Sockets.Count);
            Assert.IsFalse(s2.Connected);
        }

        [Test]
        public void Getting_sockets_in_sequence_when_the_first_is_disposed_will_return_closed_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var s1 = pool.GetSocket();
            s1.Dispose();
            _factory.Sockets[0].Connected = false;
            _factory.Sockets[0].Dispose();
            var s2 = pool.GetSocket();

            // Assert
            Assert.AreEqual(2, _factory.Sockets.Count);
        }

        [Test]
        public void Getting_sockets_slowly_in_sequence_when_the_first_has_closed_will_return_new_socket() {

            // Arrange
            var pool = new ConnectionPool(_factory.Create);
            pool.CheckInterval = TimeSpan.FromSeconds(1);

            // Act
            var s1 = pool.GetSocket();
            s1.Dispose();
            _factory.Sockets[0].Connected = false;
            Thread.Sleep(3000);
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
    }
}