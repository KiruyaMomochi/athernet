using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Dns;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Gre;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.Igmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;

namespace SendingASinglePacketWithSendPacket
{
    class Program
    {
        public static void Main(string[] args)
        {
            // Retrieve the device list from the local machine
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;

            if (allDevices.Count == 0)
            {
                Console.WriteLine("No interfaces found! Make sure WinPcap is installed.");
                return;
            }

            // Print the list
            for (int i = 0; i != allDevices.Count; ++i)
            {
                LivePacketDevice device = allDevices[i];
                Console.Write((i + 1) + ". " + device.Name);
                if (device.Description != null)
                    Console.Write(" (" + device.Description + ")");
                else
                    Console.Write(" (No description available)");
                                
                Console.WriteLine();
            }

            int deviceIndex = 0;
            do
            {
                Console.WriteLine("Enter the interface number (1-" + allDevices.Count + "):");
                string deviceIndexString = Console.ReadLine();
                if (!int.TryParse(deviceIndexString, out deviceIndex) ||
                    deviceIndex < 1 || deviceIndex > allDevices.Count)
                {
                    deviceIndex = 0;
                }
            } while (deviceIndex == 0);

            // Take the selected adapter
            PacketDevice selectedDevice = allDevices[deviceIndex - 1];
            var ipv4Address = selectedDevice.Addresses.First(x => x.Address is IpV4SocketAddress);    
            Console.WriteLine(ipv4Address);
            
            // Open the output device
            using (PacketCommunicator communicator = selectedDevice.Open(100, // name of the device
                PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                1000)) // read timeout
            {
                var packet = BuildTcpPacket();
                // communicator.SendPacket(packet);
            }
        }
        
        /// <summary>
        /// This function build an TCP over IPv4 over Ethernet with payload packet.
        /// </summary>
        private static Packet BuildTcpPacket()
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress("c4:9d:ed:2c:bf:44"),
                    Destination = new MacAddress("00:00:5e:00:01:01"),
                    EtherType = EthernetType.None, // Will be filled automatically.
                };

            IpV4Layer ipV4Layer =
                new IpV4Layer
                {
                    Source = new IpV4Address("0.0.0.0"),
                    CurrentDestination = new IpV4Address("10.20.212.86"),
                    Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                    HeaderChecksum = null, // Will be filled automatically.
                    Identification = 123,
                    Protocol = null, // Will be filled automatically.
                    Ttl = 100,
                    TypeOfService = 0,
                };

            TcpLayer tcpLayer =
                new TcpLayer
                {
                    SourcePort = 8964,
                    DestinationPort = 21,
                    Checksum = null, // Will be filled automatically.
                    SequenceNumber = 100,
                    // AcknowledgmentNumber = 50,
                    ControlBits = TcpControlBits.Synchronize,
                    Window = 100,
                    UrgentPointer = 0,
                    Options = TcpOptions.None,
                };

            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(Encoding.ASCII.GetBytes("hello world")),
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, tcpLayer, payloadLayer);

            var packet = builder.Build(DateTime.Now);
            var layer = packet.Ethernet.IpV4.Tcp;
            
            // if (layer is IpV4Layer test)
            // {
                Console.WriteLine(layer.Payload.ToMemoryStream());
            // }

            return packet;
        }

    }
}