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
using NUnit.Framework;

namespace MindTouch.Clacks.Server.Tests {

    [TestFixture]
    public class MiscTest {

        [Test]
        public void Can_copy_array_to_itself() {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Array.Copy(buffer, 2, buffer, 0, 8);
            Assert.AreEqual(
                new byte[] { 3, 4, 5, 6, 7, 8, 9, 10, 9, 10 },
                buffer
            );
        }

        [Test]
        public void Can_call_action_recursively() {
            var i = 0;
            Action action = null;
            action = () => {
                Console.WriteLine("iteration {0}", i);
                i++;
                if(i == 10) {
                    return;
                }
                action();
            };
            action();
            Assert.AreEqual(10, i);
        }
    }
}
