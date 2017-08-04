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

        private object writeLock = new object();

        private TcpClient Connect(string hostname, int port)
        {
            return new TcpClient(hostname, port)
            {
                SendTimeout = 20 * 1000 // 20 seconds
            };
        }

        private void HandleWriteError(Exception cause, byte[] msg)
        {
            if (client == null || !client.Connected)
            {
                client?.Close();
                client = null;
                client = Connect(hostname, port);
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                stream = client.GetStream();
                stream.Write(msg, 0, msg.Length);
            }
        }

        public void Write(EventLogEntry entry)
        {
            byte[] msg = FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);

            lock (writeLock)
            {
                try
                {
                    if (stream == null)
                    {
                        stream = client.GetStream();
                    }
                    stream.Write(msg, 0, msg.Length);
                }
                catch (System.IO.IOException ex)
                {
                    HandleWriteError(ex, msg);
                }
                catch (InvalidOperationException ex)
                {
                    HandleWriteError(ex, msg);
                }
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
            this.port = port;
            this.hostname = hostname;
            client = Connect(hostname, port);
        }
    }
}
