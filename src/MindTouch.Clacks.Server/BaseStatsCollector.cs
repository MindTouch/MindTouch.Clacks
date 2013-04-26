using System;
using System.Net;

namespace MindTouch.Clacks.Server {
    public class BaseStatsCollector : IStatsCollector {
        public static readonly IStatsCollector Instance = new BaseStatsCollector();
        private BaseStatsCollector() { }
        public virtual void ClientConnected(Guid clientId, IPEndPoint remoteEndPoint) { }
        public virtual void ClientDisconnected(Guid clientId) { }
        public virtual void CommandCompleted(StatsCommandInfo info) { }
        public virtual void AwaitingCommand(Guid clientId, ulong requestId) { }
        public virtual void ProcessedCommand(StatsCommandInfo statsCommandInfo) { }
        public virtual void ReceivedCommand(StatsCommandInfo statsCommandInfo) { }
        public virtual void ReceivedCommandPayload(StatsCommandInfo statsCommandInfo) { }
    }
}