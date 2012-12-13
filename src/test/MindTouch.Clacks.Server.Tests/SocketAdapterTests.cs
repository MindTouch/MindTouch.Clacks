using System;
using System.Diagnostics;
using System.Net;
using MindTouch.Clacks.Client.Net.Helper;
using NUnit.Framework;
using log4net;

namespace MindTouch.Clacks.Server.Tests {

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
