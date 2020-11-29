using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Athernet.IPLayer;
using Athernet.IPLayer.Header;
using Athernet.IPLayer.Packet;
using Athernet.MacLayer;
using Athernet.PhysicalLayer;

namespace AthernetCLI
{
    class Program
    {
        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();

            // //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var watch = Stopwatch.StartNew();
            //
            // // ReSharper disable twice StringLiteralTypo
            Nat(IPAddress.Parse("10.20.223.177"));
            // // SendUdp(new FileInfo(@"C:\Users\xtyzw\Downloads\A.txt"), IPAddress.Parse("10.19.200.129"));
            SendIcmp(Dns.GetHostEntry("www.baidu.com").AddressList[0]);
            //
            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        public static byte[] Compress(byte[] data)
        {
            var output = new MemoryStream();
            using var deflateStream = new DeflateStream(output, CompressionLevel.Optimal);
            deflateStream.Write(data, 0, data.Length);
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            var input = new MemoryStream(data);
            var output = new MemoryStream();
            using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
            deflateStream.CopyTo(output);
            return output.ToArray();
        }

        private static void Nat(IPAddress selfIpAddress)
        {
            var natTable = new Dictionary<IPEndPoint, IPEndPoint>
            {
                {IPEndPoint.Parse("192.168.1.2:6812"), new IPEndPoint(selfIpAddress, 6011)}
            };
            var node2 = new IP(IPAddress.Parse("192.168.1.1"), 1, 1, 2, 2048 - 7);

            node2.PacketAvailable += (sender, args) =>
            {
                var packet = args.Packet;
                switch (packet.TcpHeader)
                {
                    case UdpHeader udpHeader:
                        var src = new IPEndPoint(packet.Header.SourceAddress, udpHeader.SourcePort);
                        var mp = natTable[src];

                        var dst = new IPEndPoint(packet.Header.DestinationAddress, udpHeader.DestinationPort);

                        var udpClient = new UdpClient(mp);
                        udpClient.Connect(dst);
                        udpClient.Send(packet.Payload, packet.Payload.Length);
                        Console.WriteLine("Sent!");
                        break;
                    case IcmpHeader icmpHeader:
                        //Console.WriteLine("Received ICMP!");
                        var pingDst = packet.Header.DestinationAddress;
                        if (Equals(pingDst, node2.IpAddress))
                        {
                            (packet.Header.DestinationAddress, packet.Header.SourceAddress) = (
                                packet.Header.SourceAddress, packet.Header.DestinationAddress);
                            icmpHeader.Type = IcmpType.EchoReply;
                            Console.WriteLine("Send Back!");
                            node2.SendPacket(new Ipv4Packet()
                            {
                                Header = packet.Header,
                                Payload = packet.Payload,
                                TcpHeader = icmpHeader
                            });
                        }
                        else
                        {
                            packet.Header.SourceAddress = selfIpAddress;

                            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);

                            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                            socket.Connect(new IPEndPoint(packet.Header.DestinationAddress, 0));
                            socket.Send(icmpHeader.GetProtocolPacketBytes(packet.Payload));

                            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            var buffer = new byte[100];
                            var bytesRead = socket.ReceiveFrom(buffer, ref remoteEndPoint);

                            var rec = Ipv4Packet.Parse(buffer.Take(bytesRead).ToArray());

                            (packet.Header.DestinationAddress, packet.Header.SourceAddress) = (
                                packet.Header.SourceAddress, packet.Header.DestinationAddress);

                            node2.SendPacket(new Ipv4Packet()
                            {
                                Header = packet.Header,
                                Payload = rec.Payload,
                                TcpHeader = rec.TcpHeader
                            });

                            //Console.WriteLine($"ICMPListener received {bytesRead} from {remoteEndPoint}");
                            //Console.WriteLine(BitConverter.ToString(buffer));
                        }
                        break;
                    default:
                        throw new NullReferenceException();
                }
            };
            node2.StartReceive();
        }

        private static void SendUdp(FileInfo input, IPAddress ipAddress)
        {
            var node1 = new IP(IPAddress.Parse("192.168.1.2"), 2, 2, 1, 2048 - 7);

            var fs = input.OpenRead();
            var buffer = new byte[fs.Length];
            fs.Read(buffer);

            node1.SendPacket(new Ipv4Packet
            {
                Header = new Ipv4Header
                {
                    SourceAddress = IPAddress.Parse("192.168.1.2"),
                    DestinationAddress = ipAddress,
                    Ttl = 64,
                    Flags = 0x40
                },
                Payload = buffer,
                TcpHeader = new UdpHeader
                {
                    SourcePort = 6812,
                    DestinationPort = 10086
                }
            });
            Thread.Sleep(1000000);
        }

        private static void SendIcmp(IPAddress ipAddress)
        {
            var node1 = new IP(IPAddress.Parse("192.168.1.2"), 2, 2, 1, 2048 - 7);
            node1.StartReceive();
            node1.PacketAvailable += (sender, args) =>
            {
                var packet = args.Packet;

                if (packet.TcpHeader is IcmpHeader icmpHeader && Equals(packet.Header.SourceAddress, ipAddress))
                {
                    Console.WriteLine($"Received ICMP {icmpHeader.Type} from {packet.Header.SourceAddress}, the data is:");
                    Console.WriteLine(BitConverter.ToString(packet.Payload));
                }
            };


            for (int i = 0; i < 10; i++)
            {
                var bytes = new byte[16];
                new Random().NextBytes(bytes);
                node1.SendPacket(new Ipv4Packet
                {
                    Header = new Ipv4Header
                    {
                        SourceAddress = IPAddress.Parse("192.168.1.2"),
                        DestinationAddress = ipAddress,
                        Ttl = 64,
                        Flags = 0x40
                    },
                    Payload = bytes,
                    TcpHeader = new IcmpHeader()
                    {
                        Type = IcmpType.EchoRequest,
                        Sequence = 123,
                        Id = 0x7
                    }
                });
                Thread.Sleep(1000);
            }
            Thread.Sleep(1000);
        }

        private static void ReceiveUdp(string source, FileInfo output)
        {
            var udpClient = new UdpClient(10086);
            var remoteIpEndpoint = new IPEndPoint(IPAddress.Parse(source), 10086);

            var receivedBytes = udpClient.Receive(ref remoteIpEndpoint);
            Console.WriteLine($"Received {receivedBytes.Length} bytes from {remoteIpEndpoint}");
            using var fs = output.OpenWrite();
            fs.Write(receivedBytes);
        }

        private static void DoPhysicalTask(int payloadBytes = 1020)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            var node1 = new Physical(2, 1, payloadBytes);
            var node2 = new Physical(1, 2, payloadBytes);

            node2.StartReceive();

            var buffer = new byte[payloadBytes];
            var rand = new Random();
            rand.NextBytes(buffer);
            //Array.Fill<byte>(buffer, 255);

            node2.DataAvailable += (sender, args) =>
            {
                Console.WriteLine($"Data: {BitConverter.ToString(args.Data)}, CRC: {args.CrcResult}, Validate: {args.Data.SequenceEqual(buffer)}");
                ewh.Set();
            };
            //file.OpenRead().Read(buffer);
            node1.AddPayload(buffer);
            ewh.WaitOne();
        }

        private static void SendUdp(string hostname, int port)
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
    }
}