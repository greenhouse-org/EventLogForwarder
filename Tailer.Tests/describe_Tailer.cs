using NSpec;
using System;
using System.Diagnostics;
using Shouldly;

namespace Tailer.Tests
{
    class describe_Tailer : nspec
    {
        string eventSource;
        string eventLog;

        void deleteEventSourceAndLog(string sourceName, string logName)
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

        void before_each()
        {
            eventSource = "Tailer-EventSource-" + Guid.NewGuid().ToString();
            eventLog = Guid.NewGuid().ToString() + "-Tailer-EventLog";
            deleteEventSourceAndLog(eventSource, eventLog);

            EventLog.CreateEventSource(eventSource, eventLog);
        }

        void after_each()
        {
            deleteEventSourceAndLog(eventSource, eventLog);
        }

        void given_a_nonexistant_event_source()
        {
            it["throws"] = expect<Tailer.NonexistantEventSourceException>(() => { new Tailer().Start(); });
        }

        void given_a_nonexistant_log_name()
        {
            it["throws"] = expect<Tailer.NonexistantEventLogException>(() => { new Tailer().Start(); });
        }

        void Given_an_existing_event_source_and_log()
        {
            
        }
    }
}
