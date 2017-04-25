using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using Xunit;

namespace Forwarder.Tests
{
    public class ForwarderSpec : IDisposable
    {


        public ForwarderSpec() { }

        public void Dispose() { }

        //[Fact]
        //public void 

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

        /*
    [Fact]
    public void UDPWriteTest()
    {

    }
    */

        /* [Fact]
        public void TCPWriteTest()
        {

        } */

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

        [Fact]
        public void EndsWithNewlineTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            Assert.EndsWith("\n", actual);
        }

        [Fact]
        public void PriorityAndSyslogVersionTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            Assert.StartsWith("<7>1", actual);
        }

        [Fact]
        public void TimestampTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
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
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
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
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, AppName, "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal(AppName, msgParts[3]);
        }

        [Fact]
        public void EmptyAppNameTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal("-", msgParts[3]);
        }

        [Fact]
        public void PidTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal(Process.GetCurrentProcess().Id, int.Parse(msgParts[4]));
        }

        [Fact]
        public void MessageIdTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal("-", msgParts[5]);
        }

        [Fact]
        public void StructuredDataTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "app", "foo");
            string actual = Encoding.UTF8.GetString(formattedMsg);
            string[] msgParts = actual.Split(' ');

            Assert.Equal("-", msgParts[6]);
        }

        [Fact]
        public void BOMTest()
        {
            const string message = "foo";
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", message);
            byte[] BOM = { 0xEF, 0xBB, 0xBF };

            int count = 0;
            byte[] b = formattedMsg;
            for (int i = 0; i < b.Length - BOM.Length; i++)
            {
                if (b[i + 0] == BOM[0] && b[i + 1] == BOM[1] && b[i + 2] == BOM[2])
                {
                    count++;
                    continue;
                }
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public void EmptyMessageTest()
        {
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", "");
            string actual = Encoding.UTF8.GetString(formattedMsg);

            // TODO: make sure BOM is not present if the message is empty
            Assert.True(false);
        }

        [Fact]
        public void MessageTest()
        {
            const string message = "hello, world!";
            byte[] formattedMsg = Forwarder.FormatMessage(Forwarder.Priority.LOG_DEBUG, "", message);
            string actual = Encoding.UTF8.GetString(formattedMsg);

            Assert.EndsWith($"{message}\n", actual);
        }

        /*
        [Fact]
        public void ForwardedMessageFormatTest()
        {
            const string messageText = "syslog message text";

            byte[] formattedMsg = Forwarder.FormatMessage(messageText, Forwarder.Priority.LOG_DEBUG);

            SyslogEntry actual = ParseSyslogMessage(formattedMsg);

            //Assert.Equal()
        }
        */
    }
}
