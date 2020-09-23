using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace athernet.Utils
{
    public static class Maths
    {
        // From https://graphics.stanford.edu/~seander/bithacks.html
        /// <summary>
        /// Round up to next higher power of 2
        /// (return <paramref name="x"/> if already a power of 2)
        /// </summary>
        /// <param name="x">The number to bu rounded</param>
        /// <returns>The rounded number</returns>
        public static int Power2RoundUp(int x)
        {
            if (x < 0)
                return 0;

            // comment out to always take the next biggest power of two, 
            // even if x is already a power of two
            --x;

            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
    }

    public static class Network
    {
        static void ICMPListener()
        {
            var icmpListener = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            icmpListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
            icmpListener.Bind(new IPEndPoint(IPAddress.Any, 0));

            // SIO_RCVALL Control Code
            // https://docs.microsoft.com/en-us/windows/win32/winsock/sio-rcvall
            //icmpListener.IOControl(IOControlCode.ReceiveAll, BitConverter.GetBytes(3), null);

            var buffer = new byte[100];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            //icmpListener.SendTo(new byte[32], (EndPoint)(new IPEndPoint(IPAddress.Parse("10.20.216.184"), 0)));
            Console.WriteLine("Transfered.");

            while (true)
            {
                var bytesRead = icmpListener.ReceiveFrom(buffer, ref remoteEndPoint);

                Console.WriteLine($"ICMPListener received {bytesRead} from {remoteEndPoint}");
                Console.WriteLine(BitConverter.ToString(buffer));
            }
        }
    }
}
