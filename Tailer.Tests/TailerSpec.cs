using Xunit;
using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace Tailer.Tests
{
    public class TailerSpec : IDisposable
    {
        string eventSource;
        string logName;
        EventLog testLog;

        public TailerSpec()
        {
            eventSource = "Tailer-EventSource-" + Guid.NewGuid().ToString();
            logName = Guid.NewGuid().ToString() + "-Tailer-EventLog";
            deleteEventSourceAndLog(eventSource, logName);

            EventLog.CreateEventSource(eventSource, logName);
            this.testLog = new EventLog(logName);
            this.testLog.Source = eventSource;
        }

        public void Dispose()
        {
            testLog.Dispose();
            deleteEventSourceAndLog(eventSource, logName);
        }

        private void deleteEventSourceAndLog(string sourceName, string logName)
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

        [Fact]
        public void NonexistentEventLogTest()
        {
            Assert.Throws<EventLogSubscription.NonexistentEventLogException>(() => { new EventLogSubscription("nonexistentlog", (e) => { }, null, null); });
        }

        [Fact]
        public void ReadFromEventLogTest()
        {
            string actualLogMessage = "";
            bool called = false;
            EventLogSubscription.OnEntryWritten callback = (e) =>
            {
                called = true;
                actualLogMessage = e.Message;
            };
            EventLogSubscription tailer = new EventLogSubscription(logName, callback, null, null);
            tailer.Start();

            string expectedLogMessage = Guid.NewGuid().ToString();
            testLog.WriteEntry(expectedLogMessage);

            int count = 0;
            while (!called && count++ < 10)
            {
                Thread.Sleep(250);
            }
            Assert.True(count < 10);

            Assert.True(called);
            Assert.Equal(expectedLogMessage, actualLogMessage);
        }

        [Fact]
        public void HandlesCallbackExceptionTest()
        {
            bool called = false;
            EventLogSubscription.OnEntryWritten callback = (e) =>
            {
                called = true;
                throw new Exception("My Exception");
            };

            StringWriter stdout = new StringWriter();
            StringWriter stderr = new StringWriter();
            EventLogSubscription tailer = new EventLogSubscription(logName, callback, stdout, stderr);
            tailer.Start();

            testLog.WriteEntry(Guid.NewGuid().ToString());

            int count = 0;
            while (!called && count++ < 10)
            {
                Thread.Sleep(250);
            }
            Assert.True(count < 10);

            Assert.Contains("My Exception", stderr.ToString());
        }

        [Fact]
        public void OnlyReadsNewEventsTest()
        {
            const string lastMessage = "last message";

            bool called = false;
            List<string> msgs = new List<string>();
            EventLogSubscription.OnEntryWritten callback = (e) =>
            {
                msgs.Add(e.Message);
                if (e.Message == lastMessage)
                {
                    called = true;
                }
            };

            using (EventLogSubscription tailer = new EventLogSubscription(logName, callback, null, null))
            {
                string oldLogMessage = Guid.NewGuid().ToString();
                for (int i = 0; i < 5; i++)
                {
                    testLog.WriteEntry(oldLogMessage);
                }

                Tailer tailer = new Tailer(logName, callback, null, null);
                tailer.Start();

                testLog.WriteEntry(lastMessage);

                int count = 0;
                while (!called && count++ < 40)
                {
                    Thread.Sleep(250);
                }
                Assert.True(count < 10);

                Assert.Equal(1, msgs.Count);
            }
        }
    }
}
