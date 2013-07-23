using System;
using System.Net;

namespace MindTouch.Clacks.Server.Async {
    public interface IServerBuilder<out T> {
        ClacksServer Build();
        ClacksServer Build(IClacksInstrumentation instrumentation);
        T OnClientConnected(Action<Guid, IPEndPoint> clientConnected);
        T OnClientDisconnected(Action<Guid, IPEndPoint> clientDisconnected);
        T OnCommandCompleted(Action<StatsCommandInfo> commandCompleted);
        T OnAwaitingCommand(Action<Guid, ulong> awaitingCommand);
        T OnProcessedCommand(Action<StatsCommandInfo> processedCommand);
        T OnReceivedCommand(Action<StatsCommandInfo> receivedCommand);
        T OnReceivedCommandPayload(Action<StatsCommandInfo> receivedCommandPayload);
    }
}