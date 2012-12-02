using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace MindTouch.Clacks.Memcache {
    public class MemcacheProgram {

        public static void Main(string[] args) {
            var ip = "127.0.0.1";
            var port = 11211;
            if(args.Length > 0 ) {
                ip = args[0];
            }
            if(args.Length > 1) {
                port = int.Parse(args[1]);
            }
            var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            using(var server = new Memcached(endPoint)) {
                Console.WriteLine("press any key to exit");
                Console.ReadKey();
            }
        }
    }
}
