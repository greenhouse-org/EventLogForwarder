using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Forwarder.Tests
{
    public class SimpleTCPServer : IDisposable
    {
        public delegate void DatagramReceiveHandler(string data);
        public bool crashy { get; set; }
        private bool running = false;
        private bool disposed = false;
        private readonly DatagramReceiveHandler handler;
        private readonly Encoding encoding;
        private Thread serverThread;
        private TcpListener server;

        public SimpleTCPServer(int port, DatagramReceiveHandler handler)
            : this(port, handler, Encoding.Unicode)
        {
        }

        public SimpleTCPServer(int port, DatagramReceiveHandler handler, Encoding encoding)
        {
            var localIPEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            server = new TcpListener(localIPEndPoint);

            this.handler = handler;
            this.encoding = encoding;
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

            while (!serverThread.IsAlive) ;
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
                if (server != null)
                {
                    server.Stop();
                }
                if (serverThread != null)
                {
                    serverThread.Join();
                    serverThread = null;
                    server = null;
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

        private void startServer()
        {
            try
            {
                int crashInterval = 0;
                server.Start();

                byte[] buffer = new byte[4096];
                List<byte> message = new List<byte>();

                while (running)
                {
                    TcpClient client = server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    // Close the connection - crash
                    if (crashy && crashInterval++ % 7 == 0)
                    {
                        stream.Close();
                        client.Close();
                        continue;
                    }

                    message.Clear();
                    int i;
                    while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        message.AddRange(buffer.Take(i).ToArray());
                    }
                    this.handler(encoding.GetString(message.ToArray()));
                    client.Close();
                }
            }
            catch (SocketException)
            {
                // ignore - we closed the connection
                return;
            }
        }
    }
}
