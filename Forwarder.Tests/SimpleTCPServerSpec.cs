using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Forwarder.Tests
{
    public class SimpleTCPServerSpec
    {
        [Fact]
        public void CallsHandlerTest()
        {
            const string expectedMsg = "Is 普通话/普通話 there?";
            
            string actualMsg = "";
            SimpleTCPServer.DatagramReceiveHandler handler = (udpMsgStr) =>
            {
                actualMsg = udpMsgStr;
            };

            const int port = 43431;

            using (SimpleTCPServer server = new SimpleTCPServer(port, handler, Encoding.UTF8))
            {
                server.Start();
                IPEndPoint localIPEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                using (TcpClient client = new TcpClient())
                {
                    byte[] msg = Encoding.UTF8.GetBytes(expectedMsg);
                    client.Connect(localIPEndPoint);
                    var stream = client.GetStream();
                    stream.WriteTimeout = 5000; // 5 Seconds
                    stream.Write(msg, 0, msg.Length);
                    client.Close();
                }

                int limit = 10;
                while (actualMsg == "" && --limit > 0)
                {
                    Thread.Sleep(1000);
                }
                Assert.True(limit > 0);
                Assert.Equal(expectedMsg, actualMsg);
            }
        }
    }
}
