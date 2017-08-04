using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Forwarder
{

    public class UDPForwarder : Forwarder, IForwarderInterface
    {
        private UdpClient client;
        private string hostname;
        private int port;

        public void Write(EventLogEntry entry)
        {
            lock (client)
            {
                byte[] msg = FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);
                client.Send(msg, msg.Length, hostname, port);
            }
        }

        public UDPForwarder(string hostname, int port)
        {
            this.port = port;
            this.hostname = hostname;
            client = new UdpClient();
        }
    }
}
