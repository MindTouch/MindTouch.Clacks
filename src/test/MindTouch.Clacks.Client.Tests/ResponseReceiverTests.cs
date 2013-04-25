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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MindTouch.Clacks.Client.Tests {

    [TestFixture]
    public class ResponseReceiverTests {

        [Test]
        public void Can_read_single_response_with_data_from_single_receive() {

            // Arrange
            var data = "blahblahblah";
            var fakeSocket = new FakeSocket();
            fakeSocket.ReceiveCallback = Responses(
                string.Format("OK FOO {0}\r\n{1}\r\n", data.Length, data)
            );

            // Act
            var receiver = new ResponseReceiver(fakeSocket);
            receiver.Reset(new Request("OK").ExpectData("OK"));
            var response = receiver.GetResponse();
            Assert.AreEqual("OK", response.Status);

            // Assert
            Assert.AreEqual(new[] { "FOO", data.Length.ToString() }, response.Arguments);
            Assert.AreEqual(data, Encoding.ASCII.GetString(response.Data));
        }

        [Test]
        public void Can_read_single_response_from_receives_broken_at_terminator() {

            // Arrange
            var data = "blahblahblah";
            var fakeSocket = new FakeSocket();
            fakeSocket.ReceiveCallback = Responses(
                string.Format("OK FOO {0}\r\n", data.Length),
                string.Format("{0}\r\n", data)
            );

            // Act
            var receiver = new ResponseReceiver(fakeSocket);
            receiver.Reset(new Request("OK").ExpectData("OK"));
            var response = receiver.GetResponse();
            Assert.AreEqual("OK", response.Status);

            // Assert
            Assert.AreEqual(new[] { "FOO", data.Length.ToString() }, response.Arguments);
            Assert.AreEqual(data, Encoding.ASCII.GetString(response.Data));
        }

        [Test]
        public void Can_read_single_response_from_receives_broken_in_terminators() {

            // Arrange
            var data = "blahblahblah";
            var fakeSocket = new FakeSocket();
            fakeSocket.ReceiveCallback = Responses(
                string.Format("OK FOO {0}\r", data.Length),
                string.Format("\n{0}\r", data),
                "\n"
            );

            // Act
            var receiver = new ResponseReceiver(fakeSocket);
            receiver.Reset(new Request("OK").ExpectData("OK"));
            var response = receiver.GetResponse();
            Assert.AreEqual("OK", response.Status);

            // Assert
            Assert.AreEqual(new[] { "FOO", data.Length.ToString() }, response.Arguments);
            Assert.AreEqual(data, Encoding.ASCII.GetString(response.Data));
        }

        private Func<byte[], int, int, int> Responses(params string[] chunks) {
            var responses = new Queue<byte[]>(chunks.Select(Encoding.ASCII.GetBytes));
            return (buffer, offset, length) => {
                var bytes = responses.Dequeue();
                Array.Copy(bytes, 0, buffer, offset, bytes.Length);
                return bytes.Length;
            };
        }
    }
}
