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
            if (args.Length != 2)
                throw new ArgumentException("Usage: url port");
            var url = args[0];
            var port = int.Parse(args[1]);

            string[] logsToListenTo = new[] { "Application", "Security", "System" };
            var forwarder = new UDPForwarder(url, port);
            var stdout = Console.Out;
            var stderr = Console.Error;

            foreach(var logToListenTo in logsToListenTo)
            {
                var subscription = new EventLogSubscription(logToListenTo, forwarder.Write, stdout, stderr);
                subscription.Start();
            }

            WriteAMessageToTheEventLog($"Connected Event Log Forwarding");

            while (true)
            {
                Thread.Yield();
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
