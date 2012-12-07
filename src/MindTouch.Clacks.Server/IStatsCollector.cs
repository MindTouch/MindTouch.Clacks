using System.Net;

namespace MindTouch.Clacks.Server {
    public interface IStatsCollector {
        void ClientConnected(IPEndPoint remoteEndPoint);
        void ClientDisconnected(IPEndPoint endPoint);
        void CommandCompleted(IPEndPoint endPoint, StatsCommandInfo info);
        void CommandStarted(IPEndPoint endPoint, ulong id);
    }
}