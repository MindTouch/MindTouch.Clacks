using System;
using System.Net;

namespace MindTouch.Clacks.Server {
    public class NullStatsCollector : IStatsCollector {
        public static readonly IStatsCollector Instance = new NullStatsCollector();

        private NullStatsCollector() { }

        public void ClientConnected(IPEndPoint remoteEndPoint) { }
        public void ClientDisconnected(IPEndPoint endPoint) { }
        public void CommandCompleted(IPEndPoint endPoint, StatsCommandInfo info) { }
        public void CommandStarted(IPEndPoint endPoint, ulong id) { }
    }
}