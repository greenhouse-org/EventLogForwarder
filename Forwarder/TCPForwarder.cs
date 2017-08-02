using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Forwarder
{
    public class TCPForwarder : Forwarder
    {
        private TcpClient client;
        private NetworkStream stream;
        private string hostname;
        private int port;

        // WARN WARN WARN
        public int CallCount { get; set; } = 0;
        public int ExceptionCount { get; set; } = 0;
        private int missCount = 0;

        private void HandleWriteError(Exception cause, byte[] unsentMsg)
        {
            if (!client.Connected)
            {
                client.Connect(hostname, port);
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                stream = client.GetStream();
                stream.Write(unsentMsg, 0, unsentMsg.Length);
            }
        }

        public void Write(EventLogEntry entry)
        {
            CallCount++;

            byte[] msg = FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);

            try
            {
                if (stream == null)
                {
                    stream = client.GetStream();
                }
                stream.Write(msg, 0, msg.Length);
            }
            catch (System.IO.IOException ex) { HandleWriteError(ex, msg); }
            catch (InvalidOperationException ex) { HandleWriteError(ex, msg); }
            catch (Exception ex)
            {
                ExceptionCount++;
            }
        }

        public void Close()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        public TCPForwarder(string hostname, int port)
        {
            // TODO: Set send timeout
            this.port = port;
            this.hostname = hostname;
            client = new TcpClient(hostname, port);
            client.SendTimeout = 2000; // 2 Seconds
        }
    }
}
