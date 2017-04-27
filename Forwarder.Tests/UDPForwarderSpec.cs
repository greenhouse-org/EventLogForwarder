using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using Xunit;

namespace Forwarder.Tests
{
    public class UDPForwarderSpec : IDisposable
    {
        string eventSource;
        string logName;
        EventLog testLog;
        private int port;
        private UDPForwarder syslog;
        private Tailer.EventLogSubscription tailer;
        private SimpleUDPServer server;

        public UDPForwarderSpec()
        {
            CreateTempEventLog();

            port = new Random().Next(5000, 50000);
            syslog = new UDPForwarder("localhost", port);
            tailer = new Tailer.EventLogSubscription(logName, syslog.Write, null, null);
        }

        [Fact]
        public void SingleEventTest()
        {
            const string expected = "Hello, 普通话/普通話!";

            string message = "";
            server = new SimpleUDPServer(port, x =>
            {
                message = x;
            }, Encoding.UTF8);

            using (server)
            {
                server.Start();
                tailer.Start();

                testLog.WriteEntry(expected);

                int limit = 40;
                while (message == "" && limit-- > 0)
                {
                    Thread.Sleep(250);
                }
                Assert.Contains(expected, message);
                Assert.EndsWith("\n", message);
            }
        }

        [Fact]
        public void MultipleEventTest()
        {
            const string expected = "Hello, 普通话/普通話!";
            const int numMsgs = 500;
            const int sendInterval = 20; // 20MS

            List<string> msgs = new List<string>();
            server = new SimpleUDPServer(port, x =>
            {
                msgs.Add(x);
            }, Encoding.UTF8);

            using (server)
            {
                server.Start();
                tailer.Start();

                for (int i = 0; i < numMsgs; i++)
                {
                    testLog.WriteEntry(expected);
                    Thread.Sleep(sendInterval);
                }

                int limit = 40;
                while (msgs.Count < numMsgs && limit-- > 0)
                {
                    Thread.Sleep(250);
                }
                Assert.Equal(numMsgs, msgs.Count);
                foreach (string s in msgs)
                {
                    Assert.Contains(expected, s);
                    Assert.EndsWith("\n", s);
                }
            }
        }

        public void Dispose()
        {
            testLog.Dispose();
            tailer.Dispose();
            DeleteTempEventLog(eventSource, logName);
            if (server != null)
            {
                server.Dispose();
            }
        }

        private void DeleteTempEventLog(string sourceName, string logName)
        {
            if (EventLog.SourceExists(sourceName))
            {
                if (EventLog.LogNameFromSourceName(sourceName, ".") == logName)
                {
                    EventLog.DeleteEventSource(sourceName);
                    EventLog.Delete(logName);
                }
            }
        }

        private void CreateTempEventLog()
        {
            eventSource = "Tailer-EventSource-" + Guid.NewGuid().ToString();
            logName = Guid.NewGuid().ToString() + "-Tailer-EventLog";
            DeleteTempEventLog(eventSource, logName);

            EventLog.CreateEventSource(eventSource, logName);
            testLog = new EventLog(logName);
            testLog.Source = eventSource;
        }
    }
}
