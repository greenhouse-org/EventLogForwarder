using System.Diagnostics;
using System.Net.Sockets;

namespace Forwarder
{
    public class TCPForwarder : Forwarder
    {
        private TcpClient client;
        private string hostname;
        private int port;

        // WARN WARN WARN
        private int missCount = 0;

        public void Write(EventLogEntry entry)
        {
            byte[] msg = FormatMessage(Priority.LOG_DEBUG, entry.Source, entry.Message);

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                   stream.Write(msg, 0, msg.Length);
                }
            }
            catch (SocketException ex)
            {

                if (client.Connected)
                {
                    throw ex;
                }
                client.Connect(hostname, port);
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(msg, 0, msg.Length);
                }
            }
        }

        public TCPForwarder(string hostname, int port)
        {
            // TODO: Set send timeout
            this.port = port;
            this.hostname = hostname;
            client = new TcpClient(hostname, port);
        }
    }
}
