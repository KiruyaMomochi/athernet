using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RawSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            Receive("10.20.84.220", 10087);
        }

        private static void Send(string hostname, int port)
        {
            var udpClient = new UdpClient();
            var bytes = new byte[20];
            var random = new Random();

            var myTimer = new Timer(o =>
            {
                random.NextBytes(bytes);
                Console.WriteLine($"Sending {BitConverter.ToString(bytes)}");
                udpClient.Send(bytes, bytes.Length, hostname, port);
            }, null, 0, 1000);

            while (true) { }

            //udpClient.Close();
        }

        private static void Receive(string hostname, int port)
        {
            var udpClient = new UdpClient(port);
            var remoteIpEndpoint = new IPEndPoint(IPAddress.Parse(hostname), port);
            while (true)
            {
                var receivedBytes = udpClient.Receive(ref remoteIpEndpoint);
                Console.WriteLine($"Received {BitConverter.ToString(receivedBytes)} from {remoteIpEndpoint}");
            }
            //udpClient.Close();
        }
    }
}