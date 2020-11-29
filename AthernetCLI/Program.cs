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
            Nat();
            DoMacTask(new FileInfo(@"C:\Users\xtyzw\Downloads\A.txt"));

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
    }
}