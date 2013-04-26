using System;
using System.Net;

namespace MindTouch.Clacks.Server {
    public interface IStatsCollector {
        void ClientConnected(Guid clientId, IPEndPoint remoteEndPoint);
        void ClientDisconnected(Guid clientId);
        void CommandCompleted(StatsCommandInfo info);
        void AwaitingCommand(Guid clientId, ulong requestId);
        void ProcessedCommand(StatsCommandInfo statsCommandInfo);
        void ReceivedCommand(StatsCommandInfo statsCommandInfo);
        void ReceivedCommandPayload(StatsCommandInfo statsCommandInfo);
    }
}