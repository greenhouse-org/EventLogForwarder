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
        public enum Priority
        {
            LOG_EMERG,
            LOG_ALERT,
            LOG_CRIT,
            LOG_ERR,
            LOG_WARNING,
            LOG_NOTICE,
            LOG_INFO,
            LOG_DEBUG
        }

        // TODO (CEV): Investigate using a buffer
        //
        // private static byte[] buffer;

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

            if (appName == "")
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
