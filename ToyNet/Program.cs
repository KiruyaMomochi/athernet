using System;
using System.Net;
using System.Net.Sockets;
using ToyNet.IpInterface.Packet;
// using System.Data;
// using System.Diagnostics;
namespace ToyNet
{
    internal class Program
    {
        private static void Main(string[] args)
        { 
            var addressType = 4;
            var hostIpv4Address = Dns.GetHostEntry(Dns.GetHostName()).AddressList[addressType].ToString();
            
            var srcIpv4Address = hostIpv4Address;
            var destIpv4Address = Dns.GetHostEntry("www.baidu.com").AddressList[0].ToString();
            Console.WriteLine($"{srcIpv4Address}->{destIpv4Address}");
            
        }
    }
}
