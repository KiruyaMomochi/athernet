using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Athernet.IPLayer;
using Athernet.IPLayer.Header;
using Athernet.IPLayer.Packet;
using Athernet.MacLayer;
using Athernet.Nat;
using Athernet.PhysicalLayer;
using Athernet.Sockets;
using Athernet.Utils;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace AthernetCLI
{
    class Program
    {
        // public static IP node1, node2;
        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();
            // ewh.WaitOne();
            // var gateway = LivePacketDevice.AllLocalMachine[1].GetNetworkInterface().GetIPProperties().GatewayAddresses.First(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Address;
            // Console.WriteLine(Arp.Lookup(gateway));
            // var socket = new TcpSocket(1);
            // socket.Bind(2333, new IpV4Address("10.20.212.86"), 21);
            // var t = Task.Run(socket.Listen);
            // socket.Open();
            // Thread.Sleep(1000);
            // socket.SendTcpPacket(TcpControlBits.Push | TcpControlBits.Acknowledgment,
            //     new UTF8Encoding().GetBytes("HELP\r\n"));
            // Thread.Sleep(1000);
            // socket.SendTcpPacket(TcpControlBits.Push | TcpControlBits.Acknowledgment,
            //     new UTF8Encoding().GetBytes("QUIT\r\n"));
            // t.Wait();
            // socket.Break();

            TestMac();

            // var node1 = new Mac(1, 2, 0, 2048);
            // var node2 = new Mac(0, 4, 2, 2048);
            //
            // var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            // var athernetTcpSocket = new AthernetTcpSocket(node1);
            // var athernetTcpSocket2 = new AthernetTcpSocket(node1);
            
            // var nat = new Nat(1, node2);
            // nat.Listen();

            // try
            // {
                // athernetTcpSocket.Bind(2333, new IpV4Address("10.20.212.86"), 21);
                // athernetTcpSocket.Listen();
                // athernetTcpSocket.Open();
                
                // athernetTcpSocket2.Bind(2334, new IpV4Address("10.20.212.86"), 21);
                // athernetTcpSocket2.Listen();
                // athernetTcpSocket2.Open();
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine(e);
            //     throw;
            // }

            // athernetTcpSocket.NewDatagram += (sender, eventArgs) =>
            // {
            //     Console.WriteLine(eventArgs.Datagram.Decode(Encoding.UTF8));
            // };
            //
            // athernetTcpSocket.SendPayload(new UTF8Encoding().GetBytes("QUIT\n\r"));
            // athernetTcpSocket2.SendPayload(new UTF8Encoding().GetBytes("QUIT\n\r"));

            Task.Delay(100000).Wait();
        }

        //     public static byte[] Compress(byte[] data)
        //     {
        //         var output = new MemoryStream();
        //         using var deflateStream = new DeflateStream(output, CompressionLevel.Optimal);
        //         deflateStream.Write(data, 0, data.Length);
        //         return output.ToArray();
        //     }
        //
        //     public static byte[] Decompress(byte[] data)
        //     {
        //         var input = new MemoryStream(data);
        //         var output = new MemoryStream();
        //         using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
        //         deflateStream.CopyTo(output);
        //         return output.ToArray();
        //     }
        //
        //     private static void Nat(IPAddress selfIpAddress)
        //     {
        //         var natTable = new Dictionary<IPEndPoint, IPEndPoint>
        //         {
        //             {IPEndPoint.Parse("192.168.1.2:6812"), new IPEndPoint(selfIpAddress, 6011)},
        //             {new IPEndPoint(selfIpAddress, 6011), IPEndPoint.Parse("192.168.1.2:6812")}
        //         };
        //
        //         node2.PacketAvailable += (sender, args) =>
        //         {
        //             var packet = args.Packet;
        //             switch (packet.TcpHeader)
        //             {
        //                 case UdpHeader udpHeader:
        //                     Console.WriteLine("UDP Received!");
        //                     var src = new IPEndPoint(packet.Header.SourceAddress, udpHeader.SourcePort);
        //                     var mp = natTable[src];
        //
        //                     var dst = new IPEndPoint(packet.Header.DestinationAddress, udpHeader.DestinationPort);
        //
        //                     var udpClient = new UdpClient(mp);
        //                     udpClient.Connect(dst);
        //                     udpClient.Send(packet.Payload, packet.Payload.Length);
        //                     
        //                     var remoteIpEndpoint = new IPEndPoint(IPAddress.Any, 6011);
        //
        //                     var udpRec = udpClient.Receive(ref remoteIpEndpoint);
        //                     //ShowBytes(remoteIpEndpoint, (udpRec));
        //
        //                     node2.SendPacket(new Ipv4Packet(){
        //                         Header = new Ipv4Header
        //                         {
        //                             SourceAddress = remoteIpEndpoint.Address,
        //                             DestinationAddress = src.Address,
        //                             Ttl = 64,
        //                             Flags = 0x40
        //                         },
        //                         Payload = udpRec,
        //                         TcpHeader = new UdpHeader
        //                         {
        //                             SourcePort = (ushort)remoteIpEndpoint.Port,
        //                             DestinationPort = (ushort)src.Port
        //                         }
        //                     });
        //
        //                     Console.WriteLine("Sent!");
        //                     break;
        //                 case IcmpHeader icmpHeader:
        //                     //Console.WriteLine("Received ICMP!");
        //                     var pingDst = packet.Header.DestinationAddress;
        //                     if (Equals(pingDst, node2.IpAddress))
        //                     {
        //                         (packet.Header.DestinationAddress, packet.Header.SourceAddress) = (
        //                             packet.Header.SourceAddress, packet.Header.DestinationAddress);
        //                         icmpHeader.Type = IcmpType.EchoReply;
        //                         Console.WriteLine("Send Back!");
        //                         node2.SendPacket(new Ipv4Packet()
        //                         {
        //                             Header = packet.Header,
        //                             Payload = packet.Payload,
        //                             TcpHeader = icmpHeader
        //                         });
        //                     }
        //                     else
        //                     {
        //                         packet.Header.SourceAddress = selfIpAddress;
        //
        //                         var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
        //
        //                         socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        //                         socket.Connect(new IPEndPoint(packet.Header.DestinationAddress, 0));
        //                         socket.Send(icmpHeader.GetProtocolPacketBytes(packet.Payload));
        //
        //                         EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        //                         var buffer = new byte[100];
        //                         var bytesRead = socket.ReceiveFrom(buffer, ref remoteEndPoint);
        //
        //                         var rec = Ipv4Packet.Parse(buffer.Take(bytesRead).ToArray());
        //
        //                         (packet.Header.DestinationAddress, packet.Header.SourceAddress) = (
        //                             packet.Header.SourceAddress, packet.Header.DestinationAddress);
        //
        //                         node2.SendPacket(new Ipv4Packet()
        //                         {
        //                             Header = packet.Header,
        //                             Payload = rec.Payload,
        //                             TcpHeader = rec.TcpHeader
        //                         });
        //
        //                         //Console.WriteLine($"ICMPListener received {bytesRead} from {remoteEndPoint}");
        //                         //Console.WriteLine(BitConverter.ToString(buffer));
        //                     }
        //                     break;
        //                 default:
        //                     throw new NullReferenceException();
        //             }
        //         };
        //         node2.StartReceive();
        //     }
        //
        //     private static void SendUdp(FileInfo input, IPAddress ipAddress)
        //     {
        //         var fs = input.OpenRead();
        //         var buffer = new byte[fs.Length];
        //         fs.Read(buffer);
        //
        //         node1.PacketAvailable += (sender, args) =>
        //         {
        //             if (args.Packet.TcpHeader is UdpHeader udpHeader)
        //             {
        //                 var rm = new IPEndPoint(args.Packet.Header.SourceAddress, udpHeader.SourcePort);
        //                 ShowBytes(rm, args.Packet.Payload);
        //             }
        //         };
        //         node1.StartReceive();
        //
        //         node1.SendPacket(new Ipv4Packet
        //         {
        //             Header = new Ipv4Header
        //             {
        //                 SourceAddress = IPAddress.Parse("192.168.1.2"),
        //                 DestinationAddress = ipAddress,
        //                 Ttl = 64,
        //                 Flags = 0x40
        //             },
        //             Payload = buffer,
        //             TcpHeader = new UdpHeader
        //             {
        //                 SourcePort = 6812,
        //                 DestinationPort = 10086
        //             }
        //         });
        //         Thread.Sleep(1000000);
        //     }
        //
        //     private static void SendIcmp(IPAddress ipAddress)
        //     {
        //         node1.StartReceive();
        //         node1.PacketAvailable += (sender, args) =>
        //         {
        //             var packet = args.Packet;
        //
        //             if (packet.TcpHeader is IcmpHeader icmpHeader && Equals(packet.Header.SourceAddress, ipAddress))
        //             {
        //                 Console.WriteLine($"Received ICMP {icmpHeader.Type} from {packet.Header.SourceAddress}, the data is:");
        //                 Console.WriteLine(BitConverter.ToString(packet.Payload));
        //             }
        //         };
        //
        //
        //         for (int i = 0; i < 10; i++)
        //         {
        //             var bytes = new byte[16];
        //             new Random().NextBytes(bytes);
        //             node1.SendPacket(new Ipv4Packet
        //             {
        //                 Header = new Ipv4Header
        //                 {
        //                     SourceAddress = IPAddress.Parse("192.168.1.2"),
        //                     DestinationAddress = ipAddress,
        //                     Ttl = 64,
        //                     Flags = 0x40
        //                 },
        //                 Payload = bytes,
        //                 TcpHeader = new IcmpHeader()
        //                 {
        //                     Type = IcmpType.EchoRequest,
        //                     Sequence = 123,
        //                     Id = 0x7
        //                 }
        //             });
        //             Thread.Sleep(1000);
        //         }
        //         Thread.Sleep(1000);
        //     }
        //
        //     private static void ReceiveUdp(string source, FileInfo output)
        //     {
        //         var udpClient = new UdpClient(6011);
        //         var remoteIpEndpoint = new IPEndPoint(IPAddress.Any, 6011);
        //
        //         var receivedBytes = udpClient.Receive(ref remoteIpEndpoint);
        //        
        //         node2.SendPacket(new Ipv4Packet
        //         {
        //             Header = new Ipv4Header
        //             {
        //                 SourceAddress = remoteIpEndpoint.Address,
        //                 DestinationAddress = IPAddress.Parse("192.168.1.2"),
        //                 Ttl = 64,
        //                 Flags = 0x40
        //             },
        //             Payload = receivedBytes,
        //             TcpHeader = new UdpHeader
        //             {
        //                 SourcePort = (ushort) remoteIpEndpoint.Port,
        //                 DestinationPort = 6812
        //             }
        //         });
        //     }
        //
        //     private static void SendUdp(string hostname, int port)
        //     {
        //         var udpClient = new UdpClient();
        //         var bytes = new byte[20];
        //         var random = new Random();
        //
        //         var myTimer = new Timer(o =>
        //         {
        //             random.NextBytes(bytes);
        //             Console.WriteLine($"Sending {BitConverter.ToString(bytes)}");
        //             udpClient.Send(bytes, bytes.Length, hostname, port);
        //         }, null, 0, 1000);
        //
        //         while (true) { }
        //
        //         //udpClient.Close();
        //     }
        //
        //     private static void ShowBytes(IPEndPoint endPoint, byte[] b)
        //     {
        //        var x = Encoding.UTF8.GetString(b).Split('\n');
        //        foreach (var s in x)
        //        {
        //            Console.WriteLine($"{endPoint} {s}");
        //        }
        //     }
        private static void TestMac()
        {
            var node1 = new Mac(0, 2, 0, 10240);
            var node2 = new Mac(1, 4, 2, 10240);
            // var node1 = new IP(IPAddress.Parse("192.168.1.2"), 4, 2, 2048);
            // var node2 = new IP(IPAddress.Parse("192.168.1.1"), 2, 0, 2048);
            //
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            node1.StartReceive();
            node2.StartReceive();
            node2.DataAvailable += (sender, eventArgs) =>
            {
                ewh.Set();
                Console.WriteLine(BitConverter.ToString(eventArgs.Data));
            };
            node1.AddPayload(1, new byte[8192]);
        }
    }
}