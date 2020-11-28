using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RawSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            Send("1.1.1.1", 1454);
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

            while (true){}

            udpClient.Close();
        }

        private static byte[] Receive(string hostname, int port)
        {
            var udpClient = new UdpClient(port);
            var remoteIpEndpoint = new IPEndPoint(IPAddress.Parse(hostname), port);
            var receivedBytes = udpClient.Receive(ref remoteIpEndpoint);
            udpClient.Close();
            return receivedBytes;
        }
    }
}