using System;
using System.Net;
using System.Threading;
using Athernet.AppLayer.AthernetFTPClient;
using Athernet.MacLayer;
using Athernet.Nat;
using Athernet.Sockets;
using PcapDotNet.Packets.IpV4;

namespace ToyNet
{
    class Program
    {
        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();

            // var node2 = new Mac(0, 4, 3, 10240);
            // var nat = new Nat(1, node2);
            // nat.Listen();

            // Console.Write("Connecting Host Address:");
            // String Address = Console.ReadLine();
            // var CLI = new UserInterface("ftp.sjtu.edu.cn");
            // var CLI = new UserInterface("140.110.96.68");
            // CLI.Shell();
            var node1 = new Mac(1, 2, 0, 10240);
            var icmpSocket = new AthernetIcmpSocket(node1);
            icmpSocket.Listen();
            icmpSocket.SendIcmpPacket(new IpV4Address("1.1.1.1"), 0,0);
            
            Thread.Sleep(2333333);
        }
    }
}