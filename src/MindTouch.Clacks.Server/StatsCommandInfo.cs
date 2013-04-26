using System;

namespace MindTouch.Clacks.Server {
    public class StatsCommandInfo {
        public readonly Guid ClientId;
        public readonly ulong RequestId;
        public readonly TimeSpan Elapsed;
        public readonly string[] Args;
        public readonly string Status;

        public StatsCommandInfo(Guid clientId, ulong requestId, TimeSpan elapsed, string[] args, string status) {
            ClientId = clientId;
            RequestId = requestId;
            Elapsed = elapsed;
            Args = args;
            Status = status;
        }
    }
}