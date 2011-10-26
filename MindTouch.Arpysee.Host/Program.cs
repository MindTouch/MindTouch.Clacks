using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using MindTouch.Arpysee.Server;

namespace MindTouch.Arpysee.Host {
    class Program {
        static void Main(string[] args) {
            IPAddress ip = null;
            var port = 12345;
            if(args != null && args.Length > 0) {
                ip = IPAddress.Parse(args[0]);
            }
            if(args != null && args.Length > 1) {
                port = int.Parse(args[1]);
            }
            if(ip == null) {
                var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                ip = (
                             from addr in hostEntry.AddressList
                             where addr.AddressFamily.ToString() == "InterNetwork"
                             select addr
                         ).FirstOrDefault();
            }
            var registry = new CommandRepository();
            registry.RegisterDefault(request => ArpyseeResponse.WithStatus("UNKNOWNCOMMAND"));
            registry.RegisterHandler("ECHO", false, request => ArpyseeResponse.WithStatus("ECHO").WithArguments(request.Arguments));
            registry.RegisterHandler("BIN", false, request => {
                var payload = new StringBuilder();
                for(var i = 0; i < 20; i++) {
                    payload.Append(Guid.NewGuid().ToString());
                }
                return ArpyseeResponse.WithStatus("OK").WithPayload(Encoding.ASCII.GetBytes(payload.ToString()));
            });
            Console.WriteLine("starting server to listen on {0}", ip);
            var server = new ArpyseeServer(new IPEndPoint(ip, port), registry);
            Console.ReadLine();
            server.Dispose();
        }
    }
}
