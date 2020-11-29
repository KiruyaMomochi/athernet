using System;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PacketCapture
{
    class Program
    {
        private static Socket icmpSocket;
        private static byte[] receiveBuffer = new byte[256];
        private static EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            CreateIcmpSocket();
            while (true) { Thread.Sleep(10); }
        }

        private static void CreateIcmpSocket()
        {
            icmpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            icmpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            icmpSocket.Connect(IPAddress.Parse("10.20.200.129"), 0);

            icmpSocket.Send(new byte[10]);

            // Uncomment to receive all ICMP message (including destination unreachable).
            // Requires that the socket is bound to a particular interface. With mono,
            // fails on any OS but Windows.
            //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            //{
            //    icmpSocket.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });
            //}
            
            BeginReceiveFrom();
        }

        private static void BeginReceiveFrom()
        {
            icmpSocket.BeginReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None,
                ref remoteEndPoint, ReceiveCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            int len = icmpSocket.EndReceiveFrom(ar, ref remoteEndPoint);
            Console.WriteLine(string.Format("{0} Received {1} bytes from {2}",
                DateTime.Now, len, remoteEndPoint));
            LogIcmp(receiveBuffer, len);
            BeginReceiveFrom();
        }

        private static void LogIcmp(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Console.Write(String.Format("{0:X2} ", buffer[i]));
            }
            Console.WriteLine("");
        }
    }
    public static class Network
    {
        public static void ICMPListener()
        {
            var icmpListener = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            icmpListener.Bind(new IPEndPoint(IPAddress.Any, 0));
            //icmpListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);

            // SIO_RCVALL Control Code
            // https://docs.microsoft.com/en-us/windows/win32/winsock/sio-rcvall
            //icmpListener.IOControl(IOControlCode.ReceiveAll, BitConverter.GetBytes(3), null);
            //icmpListener.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });

            var buffer = new byte[100];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            //icmpListener.SendTo(new byte[32], new IPEndPoint(IPAddress.Parse("10.20.200.129"), 0));
            //Console.WriteLine("Transferred.");

            while (true)
            {
                var bytesRead = icmpListener.ReceiveFrom(buffer, 32, 0, ref remoteEndPoint);

                Console.WriteLine($"ICMPListener received {bytesRead} from {remoteEndPoint}");
                Console.WriteLine(BitConverter.ToString(buffer));
            }
        }
        private static readonly Random random = new Random();

        public static byte[] GeneratePayload(int payloadBytes)
        {
            var ret = new byte[payloadBytes]; 
            random.NextBytes(ret);
            return ret;
        }
    }
}
