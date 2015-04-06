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
using System.Net;
using System.Text;
using log4net;
using NUnit.Framework;

namespace MindTouch.Clacks.Memcache {

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class MemcacheTests {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IPEndPoint _endPoint;
        private Memcached _server;


        [SetUp]
        public void Setup() {
            _endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), new Random().Next(1000, 30000));
            _server = new Memcached(_endPoint);
            _log.Debug("created server");
        }

        [TearDown]
        public void Teardown() {
            _server.Dispose();
        }

        [Test]
        public void Roundtrip_with_single_key() {
            using(var client = new MemcacheClient(_endPoint)) {
                _log.Debug("created client");
                client.Put("foo", 123, TimeSpan.Zero, Encoding.UTF8.GetBytes("hello"));
                _log.Debug("put data");
                var data = client.Get("foo");
                _log.Debug("got data");
                Assert.AreEqual(123, data.Flags);
                Assert.AreEqual("hello", Encoding.UTF8.GetString(data.Data));
            }
        }

        [Test]
        public void Roundtrip_with_multiple_keys() {
            using(var client = new MemcacheClient(_endPoint)) {
                _log.Debug("created client");
                client.Put("foo", 123, TimeSpan.Zero, Encoding.UTF8.GetBytes("hello1"));
                client.Put("bar", 456, TimeSpan.Zero, Encoding.UTF8.GetBytes("hello2"));
                client.Put("baz", 789, TimeSpan.Zero, Encoding.UTF8.GetBytes("hello3"));
                _log.Debug("put data");
                var data = client.Gets(new[] { "foo", "bar", "bek", "baz" });
                _log.Debug("got data");
                Assert.AreEqual(3, data.Count);
                Assert.IsTrue(data.ContainsKey("foo"));
                Assert.AreEqual(123, data["foo"].Flags);
                Assert.AreEqual("hello1", Encoding.UTF8.GetString(data["foo"].Data));
                Assert.IsTrue(data.ContainsKey("bar"));
                Assert.AreEqual(456, data["bar"].Flags);
                Assert.AreEqual("hello2", Encoding.UTF8.GetString(data["bar"].Data));
                Assert.IsTrue(data.ContainsKey("baz"));
                Assert.AreEqual(789, data["baz"].Flags);
                Assert.AreEqual("hello3", Encoding.UTF8.GetString(data["baz"].Data));
            }
        }
    }
}
