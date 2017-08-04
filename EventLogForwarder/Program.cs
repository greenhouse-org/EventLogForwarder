using Forwarder;
using System;
using System.Diagnostics;
using System.Threading;
using Tailer;

namespace EventLogForwarder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Usage: [tcp|udp] url port");
            }

            var protocol = args[0];
            var url = args[1];
            var port = int.Parse(args[2]);

            IForwarderInterface forwarder;
            if (protocol == "udp")
            {
                forwarder = new UDPForwarder(url, port);
            }
            else if (protocol == "tcp")
            {
                forwarder = new TCPForwarder(url, port);
            }
            else
            {
                throw new ArgumentException("Protocol must be either 'tcp' or 'udp' got: {0}", protocol);
            }

            var stdout = Console.Out;
            var stderr = Console.Error;

            string[] logsToListenTo = new[] { "Application", "Security", "System" };

            foreach (var logToListenTo in logsToListenTo)
            {
                var subscription = new EventLogSubscription(logToListenTo, forwarder.Write, stdout, stderr);
                subscription.Start();
            }

            WriteAMessageToTheEventLog($"Connected Event Log Forwarding");

            while (true)
            {
                Thread.Sleep(3600000); // 1 Hour
            }
        }

        private static void WriteAMessageToTheEventLog(string message)
        {
            short category = 0;
            EventLogEntryType type = EventLogEntryType.Error;
            int eventID = 1000;
            var sourceName = AppDomain.CurrentDomain.FriendlyName;

            EventLog.WriteEntry(sourceName, message, type, eventID, category);
        }
    }
}
