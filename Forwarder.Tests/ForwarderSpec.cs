using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using Xunit;
using Newtonsoft.Json;

namespace Forwarder.Tests
{
    public class ForwarderSpec : IDisposable
    {
        public Forwarder forwarder;

        public ForwarderSpec()
        {
            forwarder = new Forwarder();
        }

        public void Dispose() { }

        // **TESTS TO IMPLEMENT**
        // 
        // Formatter
        // 
        // Critical:
        // - Multiline messages (this may require discusion)
        //   - Potential workaround (encode the message in JSON)
        //
        // Important
        // - Header is ASCII - no unicode - no control chars
        // - Truncate Length based on transport
        // - Include source and timestamp in structured data
        // - Enforce length limits on header elements
        // - Set Syslog Level/Priority based on EventLogEvent Information level (DONE)
        //
        // Forwarder
        // 
        // - Rate limit
        //

        // FormatTest
        //  no extra newline
        //  timestamp, etc. formatted correctly (see config in rsyslog format)

        // UDP

        // TCP:
        //  Reconnect
        //  Concurrent reconnect
        //  Concurrent write

        // TLS

        // Fallback servers

        // syslog.resume_interval (bad connections)

        // Perf

        [Fact]
        public void EndsWithNewlineTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            Assert.EndsWith("\n", actual);
        }

        [Fact]
        public void PriorityAndSyslogVersionTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            Assert.StartsWith("<7>1", actual);
        }

        [Fact]
        public void TimestampTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            actual = actual.Substring("<0>1 ".Length);
            string timestamp = actual.Substring(0, actual.IndexOf(" "));

            DateTime now = DateTime.UtcNow;
            DateTime dt = XmlConvert.ToDateTime(timestamp, XmlDateTimeSerializationMode.Utc);

            TimeSpan delta = now.Subtract(dt);
            Assert.True(delta.Seconds < 10);
        }

        [Fact]
        public void HostnameTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            string ipAddr = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddr = ip.ToString();
                    break;
                }
            }

            Assert.Equal(ipAddr, msgParts[2]);
        }

        [Fact]
        public void AppNameTest()
        {
            const string AppName = "MyApp";
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, AppName, "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal(AppName, msgParts[3]);
        }

        [Fact]
        public void EmptyAppNameTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal("-", msgParts[3]);
        }

        [Fact]
        public void PidTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal(Process.GetCurrentProcess().Id, int.Parse(msgParts[4]));
        }

        [Fact]
        public void MessageIdTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal("-", msgParts[5]);
        }

        [Fact]
        public void StructuredDataTest()
        {
            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal("-", msgParts[6]);
        }

        [Fact]
        public void UTF8BOMTest()
        {
            // Ensure the UTF-8 BOM precedes the message section of the log record.
            byte[] b = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", "foo");

            Assert.Equal(1, BOMCount(b));
        }

        [Fact]
        public void EmptyMessageTest()
        {
            // The UTF-8 BOM should not be present when there is no message.
            byte[] b = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", "");
            Assert.Equal(0, BOMCount(b));
        }

        [Fact]
        public void SimpleMessageTest()
        {
            const string message = "hello, world!";

            ByteBuffer buf = new ByteBuffer(Encoding.UTF8);
            using (JsonTextWriter w = new JsonTextWriter(buf))
            {
                w.WriteStartObject();
                w.WritePropertyName("message");
                w.WriteValue(message);
                w.WriteEndObject();
            }
            buf.Write('\n');

            byte[] formattedMsg = forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", message);
            string actual = Encoding.UTF8.GetString(formattedMsg);

            Assert.EndsWith(buf.ToString(), actual);
        }

        /// <summary>
        /// Returns the number of UTF-8 Byte Order Masks (BOM) in byte[] b. 
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private int BOMCount(byte[] b)
        {
            byte[] BOM = { 0xEF, 0xBB, 0xBF };

            int count = 0;
            for (int i = 0; i < b.Length - BOM.Length; i++)
            {
                if (b[i + 0] == BOM[0] && b[i + 1] == BOM[1] && b[i + 2] == BOM[2])
                {
                    count++;
                    continue;
                }
            }
            return count;
        }

        /// <summary>
        /// WARN (CEV): Remove if not used
        /// Also, structs aren't very idiomatic... 
        /// </summary>
        struct SyslogEntry
        {
            public Forwarder.Priority Priority;
            public char Version;
            public DateTime Timestamp;
            public string Hostname;
            public string AppName;
            public string MessageID;
            public int Pid;
            public string Message;
        }

        // WARN (CEV): Remove if not used
        private SyslogEntry ParseSyslogMessage(byte[] msg)
        {
            string s = Encoding.UTF8.GetString(msg);
            SyslogEntry entry = new SyslogEntry { };

            // PRI
            if (s[0] != '<')
            {
                throw new Exception("Invalid message: " + s);
            }
            s = s.Substring(1);
            int n = s.IndexOf('>');
            entry.Priority = (Forwarder.Priority)int.Parse(s.Substring(0, n));

            // Skip Syslog version if any "<PRI>VERSION"
            s = s.Substring(n + 1);
            entry.Version = s[0];
            s = s.Substring(1).TrimStart();

            // Timestamp
            n = s.IndexOf(" ");
            string timestamp = s.Substring(0, n);
            entry.Timestamp = XmlConvert.ToDateTime(timestamp, XmlDateTimeSerializationMode.Utc);
            s = s.Substring(n + 1).TrimStart();

            // HOSTNAME
            n = s.IndexOf(" ");
            entry.Hostname = s.Substring(0, n);
            s = s.Substring(n + 1).TrimStart();

            // hostname "app name"

            // App-Name
            n = s.IndexOf(" ");
            entry.AppName = s.Substring(0, n);
            s = s.Substring(n + 1).TrimStart();

            // PID
            n = s.IndexOf(" ");
            entry.Pid = int.Parse(s.Substring(0, n));
            s = s.Substring(n + 1).TrimStart();

            // MessageID
            n = s.IndexOf(" ");
            entry.MessageID = s.Substring(0, n);
            s = s.Substring(n + 1).TrimStart();

            entry.Message = s.TrimStart();

            return entry;
        }
    }
}
