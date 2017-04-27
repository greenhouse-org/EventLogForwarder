using System;
using System.Diagnostics;
using System.IO;

namespace Tailer
{
    public class Tailer : IDisposable
    {
        public class NonexistentEventLogException : Exception
        {
            public NonexistentEventLogException(string message) : base(message) { }
        }

        public delegate void OnEntryWritten(EventLogEntry entry);

        public string LogName { get; }

        private OnEntryWritten entryWrittenCallback;
        private EventLog log;
        private bool disposed = false;
        private TextWriter stdout;
        private TextWriter stderr;
        private int index = 0;
        private Object entryLock = new Object();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.disposed)
            {
                this.disposed = true;
                log.Dispose();
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Start()
        {
            this.index = log.Entries.Count;
            log.EntryWritten += Callback;
            log.EnableRaisingEvents = true;
        }

        private void Callback(Object o, EntryWrittenEventArgs a)
        {
            // The EventLog.EntryWritten is only triggered if the last write
            // event occured at least six seconds previously.  So we don't
            // actually use the Entry, it only serves as a signal that writes 
            // occured, but we should dispose of it regardless.
            using (a.Entry)
            {
                try
                {
                    lock (entryLock)
                    {
                        while (this.index < this.log.Entries.Count)
                        {
                            using (EventLogEntry entry = log.Entries[index++])
                            {
                                this.entryWrittenCallback(entry);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (stderr != null)
                    {
                        stderr.WriteLine(e.ToString());
                    }
                }
            }
        }

        // WARN (CEV): Do we want this thing to be logging???
        // WARN (CEV): Are we using the stdout and stderr???
        public Tailer(string logName, OnEntryWritten callback, TextWriter stdout, TextWriter stderr)
        {
            this.log = new EventLog(logName);

            try
            {
                // This will trigger an exception if the log does not exist.
                // Otherwise we don't get an exception until Start(), which
                // occurs in a Task.
                int count = log.Entries.Count;
            }
            catch
            {
                throw new NonexistentEventLogException(String.Format("Event log: '{0}' does not exist", logName));
            }

            this.LogName = logName;
            this.entryWrittenCallback = callback;
            this.stdout = stdout;
            this.stderr = stderr;
        }

    }
}
