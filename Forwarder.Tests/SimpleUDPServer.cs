using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Forwarder.Tests
{
    class SimpleUDPServer : IDisposable
    {
        public delegate void DatagramReceiveHandler(string data);

        private UdpClient client;
        private DatagramReceiveHandler handler;
        private IPEndPoint localIPEndPoint;
        private Encoding encoding;
        private Thread serverThread;
        private bool running = false;
        private bool disposed = false;

        private void startServer()
        {
            while (running)
            {
                try
                {
                    Byte[] receiveBytes = client.Receive(ref localIPEndPoint);
                    this.handler(encoding.GetString(receiveBytes));
                }
                catch (SocketException)
                {
                    // ignore - client closed - exit loop
                    return;
                }
            }
        }

        public void Start()
        {
            if (running)
            {
                throw new Exception("SimpleUDPServer: multiple calls to start");
            }
            running = true;
            serverThread = new Thread(startServer);
            serverThread.Start();

            // Spin until the thread is alive            
            while (!serverThread.IsAlive) ;
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
                if (client != null)
                {
                    client.Close();
                    client = null;
                }
                if (serverThread != null)
                {
                    serverThread.Join();
                    serverThread = null;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                Stop();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public SimpleUDPServer(int port, DatagramReceiveHandler handler)
            : this(port, handler, Encoding.Unicode)
        {
        }

        public SimpleUDPServer(int port, DatagramReceiveHandler handler, Encoding encoding)
        {
            client = new UdpClient(port);
            localIPEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            this.handler = handler;
            this.encoding = encoding;
        }
    }
}
