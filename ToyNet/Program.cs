using System;
using System.Net;
using System.Net.Sockets;
using ToyNet.IpInterface.Header;
// using System.Data;
// using System.Diagnostics;
namespace ToyNet
{
    class Program
    {
        static void Main(string[] args)
        { 
            int AddressType = 4;
            string HostIpv4Address = Dns.GetHostEntry(Dns.GetHostName()).AddressList[AddressType].ToString();
            
            string SrcIpv4Address = HostIpv4Address;
            string DestIpv4Address = Dns.GetHostEntry("www.baidu.com").AddressList[0].ToString();
            Console.WriteLine($"{SrcIpv4Address}->{DestIpv4Address}");
            
        }
    }
}
