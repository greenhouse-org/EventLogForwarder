using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace Forwarder
{

    public class Forwarder
    {
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

        // WARN: Implement
        // private string StructeredData() { return null; }

        public byte[] FormatMessage(EventLogEntry entry)
        {
            int pri = ParseEntryPRI(entry, Facility);
            return FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);
        }

        public static byte[] FormatMessage(Priority p, string appName, string message)
        {
            string utcNow = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc);

            // TODO: Store the hostname - so we don't have to keep getting it.
            // Also, only the BOSH-Agent has the canonical IP address - see if
            // we can get it from there.
            string hostname = Dns.GetHostName();
            var host = Dns.GetHostEntry(hostname);
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    hostname = ip.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(appName))
            {
                appName = "-";
            }

            const string messageID = "-"; // TODO

            const string structuredData = "-"; // TODO

            // TODO: save this value, only retrieve once
            int pid = Process.GetCurrentProcess().Id;

            string suffix = "\n";
            if (message.EndsWith("\n"))
            {
                suffix = "";
            }

            // UTF-8 Byte Order Mask: %xEF.BB.BF
            byte[] BOM = { 0xEF, 0xBB, 0xBF };

            byte[] header = Encoding.UTF8.GetBytes($"<{(int)p}>1 {utcNow} {hostname} {appName} {pid} {messageID} {structuredData} ");
            if (string.IsNullOrEmpty(message))
            {
                // WARN (CEV): Don't really do this.
                header[header.Length - 1] = (byte)'\n';
                return header;
            }

            byte[] msgbuf = Encoding.UTF8.GetBytes($"{message}{suffix}");
            byte[] buffer = new byte[header.Length + msgbuf.Length + BOM.Length];
            header.CopyTo(buffer, 0);
            BOM.CopyTo(buffer, header.Length);
            msgbuf.CopyTo(buffer, header.Length + BOM.Length);
            return buffer;

            //string buf = $"<{(int)p}>1 {utcNow} {hostname} {appName} {pid} {messageID} {structuredData} {BOM}{message}{suffix}";

            //return Encoding.UTF8.GetBytes(buf);
        }
    }
}
