using System;

namespace MindTouch.Clacks.Server {
    public class StatsCommandInfo {
        public readonly ulong Id;
        public readonly TimeSpan Elapsed;
        public readonly string Cmd;
        public readonly string Status;

        public StatsCommandInfo(ulong id, TimeSpan elapsed, string cmd, string status) {
            Id = id;
            Elapsed = elapsed;
            Cmd = cmd;
            Status = status;
        }
    }
}