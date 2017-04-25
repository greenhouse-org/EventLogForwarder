using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;

namespace Forwarder.Tests
{
    public class SimpleUDPServerSpec
    {
        [Fact]
        public void CallsHandlerTest()
        {
            const string expectedMsg = "Is anybody there?";

            string actualMsg = "";
            bool called = false;
            SimpleUDPServer.DatagramReceiveHandler handler = (udpMsgStr) =>
            {
                actualMsg = udpMsgStr;
                called = true;
            };

            const int port = 43431;

            using (SimpleUDPServer server = new SimpleUDPServer(port, handler))
            {
                server.Start();

                using (UdpClient client = new UdpClient(0))
                {
                    Byte[] msg = Encoding.Unicode.GetBytes(expectedMsg);
                    IPEndPoint localIPEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                    client.Send(msg, msg.Length, localIPEndPoint);
                    client.Close();
                }

                int limit = 10;
                while (!called && --limit > 0)
                {
                    Thread.Sleep(100);
                }
                Assert.True(limit > 0);
                Assert.True(called);
                Assert.Equal(expectedMsg, actualMsg);
            }
        }
    }
}
