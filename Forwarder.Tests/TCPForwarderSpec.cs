using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tailer;
using Xunit;

namespace Forwarder.Tests
{
    public class TCPForwarderSpec
    {
        string eventSource;
        string logName;
        EventLog testLog;
        private int port;
        private TCPForwarder syslog;
        private Tailer.EventLogSubscription tailer;
        private SimpleTCPServer server;

        public TCPForwarderSpec()
        {
            CreateTempEventLog();
            port = new Random().Next(5000, 50000);
        }

        [Fact]
        public void SingleEventTest()
        {
            const string expected = "Hello, 普通话/普通話!";

            string message = "";
            server = new SimpleTCPServer(port, x =>
            {
                message = x;
            }, Encoding.UTF8);
            server.Start();
            syslog = new TCPForwarder("localhost", port);
            tailer = new Tailer.EventLogSubscription(logName, syslog.Write, null, null);

            using (server)
            {
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
            server = new SimpleTCPServer(port, x =>
            {
                msgs.Add(x);
            }, Encoding.UTF8);
            server.Start();
            syslog = new TCPForwarder("localhost", port);
            tailer = new Tailer.EventLogSubscription(logName, syslog.Write, null, null);

            using (server)
            {
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

        [Fact]
        public void ReconnectTest()
        {
            const string expected = "Hello, 普通话/普通話!";
            const int numMsgs = 500;
            const int sendInterval = 20; // 20MS

            List<string> msgs = new List<string>();
            server = new SimpleTCPServer(port, x =>
            {
                msgs.Add(x);
            }, Encoding.UTF8);
            server.Start();

            syslog = new TCPForwarder("localhost", port);
            tailer = new Tailer.EventLogSubscription(logName, syslog.Write, null, null);

            using (server)
            {
                tailer.Start();

                server.crashy = true;
                for (int i = 0; i < numMsgs; i++)
                {
                    testLog.WriteEntry(expected);
                    Thread.Sleep(sendInterval);
                }

                int limit = 20;
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
