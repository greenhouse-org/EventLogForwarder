using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Forwarder.Tests
{
    public class SimpleTCPServer : IDisposable
    {
        public delegate void DatagramReceiveHandler(string data);
        public bool Crashy { get; set; }

        private bool running = false;
        private bool disposed = false;
        private IPEndPoint endPoint;
        private readonly DatagramReceiveHandler handler;
        private readonly Encoding encoding;
        private Thread serverThread;
        private TcpListener server;
        private TcpClient client;
        private NetworkStream stream;

        public SimpleTCPServer(int port, DatagramReceiveHandler handler)
            : this(port, handler, Encoding.Unicode)
        {
        }

        public SimpleTCPServer(int port, DatagramReceiveHandler handler, Encoding encoding)
        {
            endPoint = new IPEndPoint(IPAddress.Loopback, port);
            server = new TcpListener(endPoint);

            this.handler = handler;
            this.encoding = encoding;
        }

        public void Restart()
        {
            server = new TcpListener(endPoint);
            Start();
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
                stream?.Close();
                client?.Close();
                server?.Stop();
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

        private void StreamMessages()
        {
            int crashInterval = 1;
            bool crashed = false;

            client = server.AcceptTcpClient();
            stream = client.GetStream();

            try
            {
                using (StreamReader sr = new StreamReader(stream, encoding))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (Crashy && crashInterval++ % 7 == 0)
                        {
                            crashed = true;
                            stream.Close();
                        }
                        this.handler(line);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                if (!crashed)
                {
                    throw ex;
                }
                return;
            }
            catch (IOException ex)
            {
                // Thrown when we crash/stop the server
                if (!crashed && running)
                {
                    throw ex;
                }
            }
            finally
            {
                if (!crashed)
                {
                    client.Close();
                }
            }
        }

        private void startServer()
        {
            try
            {
                server.Start();
                while (running)
                {
                    StreamMessages();
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
