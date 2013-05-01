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

using MindTouch.Clacks.Client.Net;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [TestFixture]
    public class ClacksClientTests {

        private FakeSocketFactory _factory;

        [SetUp]
        public void Setup() {
            _factory = new FakeSocketFactory();
        }

        [Test]
        public void Creating_client_gets_socket_from_pool() {
            
            // Arrange
            var pool = new ConnectionPool(_factory.Create);

            // Act
            var client = new ClacksClient(pool);

            // Assert
            Assert.AreEqual(1,_factory.Sockets.Count);
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
    }
}