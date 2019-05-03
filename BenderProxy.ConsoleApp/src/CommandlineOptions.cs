using System;
using CommandLine;
using CommandLine.Text;

namespace BenderProxy.ConsoleApp
{
    public class CommandlineOptions {

        [Option('h', "host", Default = "localhost", HelpText = "Hostname on to listen")]
        public String Host { get; set; }

        [Option("httpport", HelpText = "Port HTTP proxy will be listening to", Required = false)]
        public Int32 HttpPort { get; set; } 

        [Option("sslport", HelpText = "Port HTTPS proxy will be listening to", Required = false)]
        public Int32 SslPort { get; set; }
    }
}
