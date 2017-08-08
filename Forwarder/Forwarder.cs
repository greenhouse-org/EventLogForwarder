using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace Forwarder
{
    public class Forwarder
    {
        private ByteBuffer buf = new ByteBuffer(Encoding.UTF8);
        private string hostname;
        private int pid = 0;

        public string LogName { get; }
        public Priority Facility { get; } = 0;

        public enum Priority
        {
            // Severity
            LOG_EMERG = 0,
            LOG_ALERT = 1,
            LOG_CRIT = 2,
            LOG_ERR = 3,
            LOG_WARNING = 4,
            LOG_NOTICE = 5,
            LOG_INFO = 6,
            LOG_DEBUG = 7,

            // Facility
            LOG_KERN = 0,
            LOG_USER = 8,
            LOG_MAIL = 16,
            LOG_DAEMON = 24,
            LOG_AUTH = 32,
            LOG_SYSLOG = 40,
            LOG_LPR = 48,
            LOG_NEWS = 56,
            LOG_UUCP = 64,
            LOG_CRON = 72,
            LOG_AUTHPRIV = 80,
            LOG_FTP = 88,
            LOG_LOCAL0 = 128,
            LOG_LOCAL1 = 136,
            LOG_LOCAL2 = 144,
            LOG_LOCAL3 = 152,
            LOG_LOCAL4 = 160,
            LOG_LOCAL5 = 168,
            LOG_LOCAL6 = 176,
            LOG_LOCAL7 = 184
        }

        private static int PRI(Priority severity, Priority facility)
        {
            const int severityMask = 0x07;
            const int facilityMask = 0xf8;

            return ((int)severity & severityMask) | ((int)facility & facilityMask);
        }

        public static int ParseEntryPRI(EventLogEntry entry, Priority facility = 0)
        {
            Priority severity = 0;
            switch (entry.EntryType)
            {
                case EventLogEntryType.Information:
                    severity = Priority.LOG_INFO;
                    break;
                case EventLogEntryType.Warning:
                    severity = Priority.LOG_WARNING;
                    break;
                case EventLogEntryType.Error:
                    severity = Priority.LOG_ERR;
                    break;
                case EventLogEntryType.SuccessAudit:
                    severity = Priority.LOG_WARNING;
                    break;
                case EventLogEntryType.FailureAudit:
                    severity = Priority.LOG_ERR;
                    break;
                default:
                    // This should never happen!
                    throw new ArgumentOutOfRangeException("entry");
            }
            return PRI(severity, facility);
        }

        private static string FormatAppName(string source)
        {
            const int MaxLength = 48;

            if (string.IsNullOrEmpty(source))
            {
                return "-";
            }
            if (source.Contains(" "))
            {
                source = source.Replace(' ', '_');
            }
            if (source.Length > MaxLength)
            {
                source = source.Substring(0, MaxLength);
            }
            return source;
        }

        private string GetHostname()
        {
            if (!string.IsNullOrEmpty(this.hostname))
            {
                return this.hostname;
            }

            string hostname = Dns.GetHostName();
            IPHostEntry host = Dns.GetHostEntry(hostname);

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    hostname = ip.ToString();
                    break;
                }
            }

            this.hostname = hostname;
            return hostname;
        }

        private int GetPID()
        {
            if (pid != 0)
            {
                return pid;
            }
            pid = Process.GetCurrentProcess().Id;
            return pid;
        }

        public byte[] FormatMessage(EventLogEntry entry)
        {
            int pri = ParseEntryPRI(entry, Facility);
            return FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);
        }

        public byte[] FormatMessage(Priority p, string source, string message)
        {
            const string messageID = "-"; // TODO
            const string structuredData = "-"; // TODO

            // UTF-8 Byte Order Mask: %xEF.BB.BF
            byte[] BOM = { 0xEF, 0xBB, 0xBF };

            string utcNow = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

            string hostname = GetHostname();

            string appName = FormatAppName(source);

            int pid = GetPID();

            buf.Reset();
            buf.Write($"<{(int)p}>1 {utcNow} {hostname} {appName} {pid} {messageID} {structuredData} ", Encoding.ASCII);

            if (!string.IsNullOrEmpty(message))
            {
                buf.Write(BOM);
                using (JsonTextWriter w = new JsonTextWriter(buf))
                {
                    w.WriteStartObject();
                    w.WritePropertyName("message");
                    w.WriteValue(message);
                    w.WritePropertyName("source");
                    w.WriteValue(source);
                    w.WriteEndObject();
                }
            }

            // Newline
            buf.Write('\n');
            return buf.GetBytes();
        }
    }
}
