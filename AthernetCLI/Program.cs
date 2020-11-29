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
        static IPAddress node3IPAddress = IPAddress.Parse("10.12.34.56");
        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();

            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var watch = Stopwatch.StartNew();

            // ReSharper disable twice StringLiteralTypo
            // Nat();
            // DoMacTask(new FileInfo(@"C:\Users\xtyzw\Downloads\A.txt"));

            IcmpTest();
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

        private static void Nat()
        {
            var NatTable = new Dictionary<IPEndPoint, IPEndPoint>
            {
                {IPEndPoint.Parse("192.168.1.2:6812"), IPEndPoint.Parse("10.20.223.177:6811")}
            };
            var node2 = new IP(IPAddress.Parse("192.168.1.1"), 1, 1, 2, 2048 - 7);
            
            node2.PacketAvailable += (sender, args) =>
            {
                var packet = args.Packet;
                if (packet.TcpHeader is UdpHeader udpHeader)
                {
                    var endpoint = new IPEndPoint(packet.Header.SourceAddress, udpHeader.SourcePort);
                    var mp = NatTable[endpoint];

                    var dst = new IPEndPoint(packet.Header.DestinationAddress, udpHeader.DestinationPort);

                    var udpClient = new UdpClient(mp);
                    udpClient.Connect(dst);
                    udpClient.Send(packet.Payload, packet.Payload.Length);
                    Console.WriteLine("Sent!");
                    // var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Udp);
                }
            };
            node2.StartReceive();
        }

        private static void DoMacTask(FileInfo input)
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
                    DestinationAddress = IPAddress.Parse("10.19.126.155"),
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
        
        private static void DoPhysicalTask(int payloadBytes = 1020)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            var node1 = new Physical(2, 1, payloadBytes);
            var node2 = new Physical(0, 0, payloadBytes);

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
        
        private static void IcmpTest()
        {
            var IcmpEchoPacket = new IcmpPacket() {Header = new IcmpHeader()};
            IcmpEchoPacket.SetHeader(IcmpType.EchoRequest, 0, 114);
            IcmpEchoPacket.Payload = new byte[32] {1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6 ,7, 7, 7, 7, 8, 8, 8, 8};
            
            byte[] IcmpEchoByte = IcmpEchoPacket.Header.GetProtocolPacketBytes(IcmpEchoPacket.Payload);
            var IcmpIpv4Packet = new Ipv4Packet();
            IcmpIpv4Packet.SetHeader(IPAddress.Parse("10.20.194.230"), IPAddress.Parse("220.181.38.150"), IcmpEchoByte.Length);
            IcmpIpv4Packet.Header.Ttl = 233;
            byte[] BuiltPacket = PacketBuilder.BuildPacket(IcmpIpv4Packet.Header, IcmpEchoPacket.Header, IcmpEchoPacket.Payload);
            
            int j = 0;
            foreach (byte i in BuiltPacket)
            {
                Console.Write("{0:X2} ", i);
                j++;
                if (j % 2 == 0) Console.Write(" ");
                if (j % 16 == 0) Console.Write("\n");
            }

            ReceiveAndSendSignal(BuiltPacket, IPAddress.Parse("220.181.38.150"), 10086);
            
        }

        private static void SendSignal(byte [] builtPacket, IPAddress destAddress, int destPort)
        {
            // Create connection
            var rawSocket = new Socket(
                destAddress.AddressFamily,
                SocketType.Raw,
                ProtocolType.Raw
            );

            // Bind the socket to the interface specified
            IPAddress bindAddress = IPAddress.Any;
            rawSocket.Bind( new IPEndPoint( bindAddress, 0 ) );

            // Set the HeaderIncluded option since we include the IP header
            SocketOptionLevel socketLevel = SocketOptionLevel.IP;
            rawSocket.SetSocketOption( socketLevel, SocketOptionName.HeaderIncluded, 1 );
            try
            {
                // Send the packet!
                int sendCount = 114514; // 自己指定
                for (int i = 0; i < sendCount; i++)
                {
                    int rc = rawSocket.SendTo(builtPacket, new IPEndPoint(destAddress, destPort));
                    Thread.Sleep(1000);
                    Console.WriteLine($"Sent {i} packets!");
                }
            }
            catch (SocketException err)
            {
                Console.WriteLine("Socket error occured: {0}", err.Message);
                // http://msdn.microsoft.com/en-us/library/ms740668.aspx
            }
            finally
            {
                // Close the socket
                Console.WriteLine("Closing the socket...");
                rawSocket.Close();
            }

        }
        private static void ReceiveAndSendSignal(byte [] builtPacket, IPAddress destAddress, int destPort)
        {
            var rawSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Raw,
                ProtocolType.Icmp
            );

            // Bind the socket to the interface specified
            IPAddress bindAddress = IPAddress.Any;
            rawSocket.Bind( new IPEndPoint( bindAddress, 0 ) );
            

            // Set the HeaderIncluded option since we include the IP header
            SocketOptionLevel socketLevel = SocketOptionLevel.IP;
            rawSocket.SetSocketOption( socketLevel, SocketOptionName.HeaderIncluded, 1 );

            var ReceiveBuffer = new Byte[114514];
            EndPoint remote = new IPEndPoint(destAddress, destPort);
            EndPoint remoteAny = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
            
            rawSocket.Connect(remote);
            try
            {
                // Send the packet!
                int sendCount = 114514; // 自己指定
                for (int i = 0; i < sendCount; i++)
                {

                    int rc = rawSocket.Send(builtPacket);
                    Console.WriteLine($"Sent {rc/60} packets!");
                    var byteCount = rawSocket.ReceiveFrom(ReceiveBuffer, ref remoteAny); // block
                    Console.WriteLine($"Receive {byteCount/60} packets!");
                    Thread.Sleep(1000);
                    // var j = 0;
                    // foreach (byte k in ReceiveBuffer)
                    // {
                    //     Console.Write("{0:X2} ", k);
                    //     j++;
                    //     if (j % 2 == 0) Console.Write(" ");
                    //     if (j % 16 == 0) Console.Write("\n");
                    // }
                }
            }
            catch (SocketException err)
            {
                Console.WriteLine("Socket error occured: {0}", err.Message);
                // http://msdn.microsoft.com/en-us/library/ms740668.aspx
            }
            finally
            {
                // Close the socket
                Console.WriteLine("Closing the socket...");
                rawSocket.Close();
            }
        }
    }
}