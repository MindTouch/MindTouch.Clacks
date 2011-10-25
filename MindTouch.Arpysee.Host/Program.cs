using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Server;

namespace MindTouch.Arpysee.Host {
    class Program {
        static void Main(string[] args) {
            var registry = new CommandRepository();
            registry.RegisterDefault(request => ServerResponse.WithStatus("UNKNOWNCOMMAND"));
            registry.Register("ECHO", request => ServerResponse.WithStatus("ECHO").WithArguments(request));
            var server = new ArpyseeServer(new IPEndPoint(IPAddress.Parse("192.168.0.99"), 12345), registry);
            Console.ReadLine();
            server.Dispose();
        }
    }
}
