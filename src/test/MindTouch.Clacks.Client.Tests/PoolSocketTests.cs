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

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using MindTouch.Clacks.Client.Net.Helper;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class PoolSocketTests {

        [Test]
        public void On_failure_PoolSocket_and_underlying_socket_are_disposed_and_reclaimed() {

            // Arrange
            var fakesocket = new FakeSocket { ReceiveCallback = (buffer, offset, length) => { throw new SocketException(); } };
            var callbacks = new PoolSocketCallbacks();
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim);

            // Act
            try {
                poolsocket.Receive(new byte[0], 0, 0, false);
                Assert.Fail("didn't throw");
            } catch(SocketException) { }

            // Assert
            Assert.AreEqual(1, fakesocket.ReceiveCalled, "send called wrong number of times");
            Assert.IsTrue(fakesocket.IsDisposed, "underlying socket wasn't disposed");
            Assert.IsTrue(poolsocket.IsDisposed, "pool socket wasn't disposed");
            Assert.AreEqual(1, callbacks.ReclaimCalled, "reclaim was called wrong number of times");
            Assert.AreSame(fakesocket, callbacks.ReclaimedSocket, "underlying socket wasn't the one reclaimed");
        }

        [Test]
        public void Throw_SocketException_when_connection_is_closed_and_will_not_retry() {
            
            // Arrange
            var fakesocket = new FakeSocket { Connected = false, ReceiveCallback = (buffer, offset, length) => { throw new SocketException(); } };
            var callbacks = new PoolSocketCallbacks();
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim);

            // Act
            try {
                poolsocket.Receive(new byte[0], 0, 0, false);
                Assert.Fail("didn't throw");
            }
            catch (SocketException ex) {

                // Assert
                Assert.AreEqual((int)SocketError.NotConnected, ex.ErrorCode);
            }            
        }

        [Test]
        public void Do_not_throw_SocketException_when_connection_is_closed_and_will_retry() {

            // Arrange
            var fakesocket = new FakeSocket { Connected = false };
            var callbacks = new PoolSocketCallbacks();
            var poolsocket = new PoolSocket(fakesocket, callbacks.Reclaim);

            // Act
            var response = poolsocket.Receive(new byte[0], 0, 0, true);
                
            // Assert
            Assert.AreEqual(0, response, "Did not receive response.");
            Assert.AreEqual(1, fakesocket.ReceiveCalled, "send called wrong number of times");
            Assert.IsFalse(fakesocket.IsDisposed, "underlying socket was disposed");
            Assert.IsFalse(poolsocket.IsDisposed, "pool socket was disposed");
            Assert.AreEqual(0, callbacks.ReclaimCalled, "reclaim was called wrong number of times");
            Assert.IsNull(callbacks.ReclaimedSocket, "underlying socket wasn't the one reclaimed");
        }
    }
}