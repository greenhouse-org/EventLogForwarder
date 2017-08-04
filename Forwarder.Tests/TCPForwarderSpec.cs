using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
                Thread.Sleep(20);

                int limit = 40;
                while (message == "" && limit-- > 0)
                {
                    Thread.Sleep(250);
                }
                Assert.Contains(expected, message);
            }
        }

        [Fact]
        public void MultipleEventTest()
        {
            const string expected = "MultipleEventTest: ᠮᠣᠩᠭᠣᠯ"; // Mongolian script
            const int numMsgs = 500;
            const int sendInterval = 10; // 10MS

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
                    testLog.WriteEntry(string.Format("{0}: {1}", expected, i));
                    Thread.Sleep(sendInterval);
                }

                for (int i = 0; i < 40 && msgs.Count < numMsgs; i++)
                {
                    Thread.Sleep(250);
                }
                Assert.NotEqual(0, msgs.Count);
                Assert.NotEqual(0, msgs.Count(s => s.Contains(expected)));

                for (int i = 0; i < numMsgs; i++)
                {
                    string suffix = string.Format("{0}: {1}", expected, i);
                    int count = msgs.Count(s => s.EndsWith(suffix));
                    Assert.Equal(1, count);
                }
            }
        }

        [Fact]
        public void ReconnectTest()
        {
            string Message = "";
            server = new SimpleTCPServer(port, x =>
            {
                Message = x;
            }, Encoding.UTF8);
            server.Start();

            syslog = new TCPForwarder("localhost", port);
            tailer = new Tailer.EventLogSubscription(logName, syslog.Write, null, null);

            using (server)
            {
                tailer.Start();

                testLog.WriteEntry("msg: 1");
                int limit = 20000;
                while (Message == "" && limit-- > 0)
                {
                    Thread.Sleep(100);
                }
                Assert.Contains("msg: 1", Message);

                server.Stop();

                testLog.WriteEntry("msg: 2");
                Thread.Sleep(20);

                // Make sure stop worked
                Assert.Contains("msg: 1", Message);
                Assert.DoesNotContain("msg: 2", Message);

                server.Restart();

                Message = "";
                testLog.WriteEntry("msg: 3");
                limit = 20000;
                while (Message == "" && limit-- > 0)
                {
                    Thread.Sleep(100);
                }
                Thread.Sleep(20);
                Assert.Contains("msg: 3", Message);
            }
        }

        [Fact]
        public void MultipleReconnectTest()
        {
            const string CrashyMessage = "Crashy 普通话 / 普通話!";
            const string StableMessage = "ReconnectTest";

            const int numMsgs = 500;
            const int sendInterval = 10; // 10MS

            List<string> msgs = new List<string>();
            server = new SimpleTCPServer(port, x =>
            {
                lock (msgs)
                {
                    msgs.Add(x);
                }
            }, Encoding.UTF8);
            server.Start();

            syslog = new TCPForwarder("localhost", port);
            tailer = new Tailer.EventLogSubscription(logName, syslog.Write, null, null);

            using (server)
            {
                tailer.Start();
                server.Crashy = true;

                for (int i = 0; i < numMsgs; i++)
                {
                    testLog.WriteEntry(String.Format("{0}: {1}", CrashyMessage, i));
                    Thread.Sleep(sendInterval);
                }

                // Wait for at least 10 messages
                for (int i = 0; i < 40 && msgs.Count < 10; i++)
                {
                    Thread.Sleep(250);
                }
                Assert.NotEqual(0, msgs.Count);

                // Stop crashing
                server.Crashy = false;

                // Wait for things to stabilize
                int n;
                do
                {
                    n = msgs.Count;
                    Thread.Sleep(500);
                } while (n != msgs.Count);

                // Test that after a crashy period we can still send messages

                for (int i = 0; i < numMsgs; i++)
                {
                    testLog.WriteEntry(String.Format("{0}: {1}", StableMessage, i));
                    Thread.Sleep(sendInterval);
                }

                // Wait for all the non-crashy messages to arrive
                for (int i = 0; i < 50; i++)
                {
                    lock (msgs)
                    {
                        if (msgs.Count(s => s.Contains(StableMessage)) == numMsgs)
                        {
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }

                // Lock here in case the above loop timed out.
                lock (msgs)
                {
                    Assert.Equal(numMsgs, msgs.Count(s => s.Contains(StableMessage)));
                }

                // Make sure we got some crashy messages in there
                Assert.NotEqual(0, msgs.Count(s => s.Contains(CrashyMessage)));

                for (int i = 0; i < numMsgs; i++)
                {
                    string suffix = String.Format("{0}: {1}", StableMessage, i);
                    int count = msgs.Count(s => s.EndsWith(suffix));
                    Assert.Equal(1, count);
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
