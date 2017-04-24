using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

        [Fact]
        public void UDPWriteTest()
        {

        }

        [Fact]
        public void TCPWriteTest()
        {

        }
    }

    class SimpleUDPServer
    {
        public delegate void DatagramReceiveHandler(string data);

        private UdpClient client;
        private DatagramReceiveHandler handler;
        private IPEndPoint localIPEndPoint;
        private Thread serverThread;
        private bool running = false;

        public SimpleUDPServer(int port, DatagramReceiveHandler handler)
        {
            client = new UdpClient(port);
            localIPEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            this.handler = handler;
        }

        private void startServer()
        {
            while (running)
            {
                Byte[] receiveBytes = client.Receive(ref localIPEndPoint);
                this.handler(Encoding.Unicode.GetString(receiveBytes));
            }
        }

        public void Start()
        {
            if (!running)
            {
                running = true;
                serverThread = new Thread(Start);
                serverThread.Start();
                while (!serverThread.IsAlive) ;
            }
            else
            {
                throw new Exception("Multiple calls to SimpleUDPServer.Start!");
            }
        }

        public void Stop()
        {
            running = false;
            client.Close();
            if (serverThread != null)
            {
                serverThread.Abort();
                serverThread.Join();
            }
        }
    }
}
