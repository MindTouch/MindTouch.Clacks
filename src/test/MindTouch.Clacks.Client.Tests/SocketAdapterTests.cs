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
 */using System;
using System.Diagnostics;
using System.Net;
using MindTouch.Clacks.Client.Net.Helper;
using NUnit.Framework;
using log4net;

namespace MindTouch.Clacks.Client.Tests {

    [TestFixture]
    public class SocketAdapterTests {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Test]
        public void Connect_timeout_with_hostport_is_respected() {
            var t = Stopwatch.StartNew();
            try {
                _log.Debug("connecting");
                var socket = SocketAdapter.Open("1.2.3.4", 1234, TimeSpan.FromSeconds(1));
                Assert.Fail("didn't timeout");
            } catch(TimeoutException) {
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
            } catch(TimeoutException) {
                t.Stop();
                Assert.GreaterOrEqual(1200, t.ElapsedMilliseconds);
            }
        }
    }
}
