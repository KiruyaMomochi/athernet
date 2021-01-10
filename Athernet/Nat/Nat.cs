using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Athernet.IPLayer;
using Athernet.MacLayer;
using Athernet.Utils;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using AddressFamily = System.Net.Sockets.AddressFamily;
using PacketBuilder = PcapDotNet.Packets.PacketBuilder;

namespace Athernet.Nat
{
    public class Nat
    {
        // private NatTable _natTable;

        // binding: athernet mac, interface number
        private Random _random = new Random();

        private readonly PacketCommunicator _communicator;

        private IpV4Address _localAddress;
        // private IpV4Address _remoteAddress;

        private readonly Mac _athernetMac;
        private readonly EthernetLayer _ethernetLayer;

        public Nat(int deviceIndex, Mac athernetMac)
        {
            MacAddress remoteMacAddress;
            MacAddress localMacAddress;

            _athernetMac = athernetMac;
            _natTable = new Dictionary<NatEntry, NatEntry>();
            var device = LivePacketDevice.AllLocalMachine[deviceIndex];
            _communicator = device.Open(65536,
                PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal,
                1000);
            var address = device.Addresses.First(x => x.Address is IpV4SocketAddress).Address;
            if (address is IpV4SocketAddress ipV4SocketAddress)
            {
                _localAddress = ipV4SocketAddress.Address;
                localMacAddress = device.GetMacAddress();
                var gateway = device.GetNetworkInterface().GetIPProperties().GatewayAddresses
                    .First(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Address;
                remoteMacAddress = Arp.Lookup(gateway);
            }
            else
            {
                throw new InvalidOperationException("Only IpV4 address is supported.");
            }

            _ethernetLayer = new EthernetLayer()
            {
                Source = localMacAddress,
                Destination = remoteMacAddress,
                EtherType = EthernetType.None
            };
        }

        private string FilterString
        {
            get
            {
                if (_filterPorts.Count == 0)
                {
                    return "icmp";
                }

                // return "tcp and (" + string.Join(" or ", _filterPorts.Select(x => $"(dst port {x})")) + ")";
                var tcp_str = string.Join(" or ", _filterPorts.Select(x => $"(dst port {x})"));
                return "(icmp) or (" + tcp_str + ")";
            }
        }

        private List<ushort> _filterPorts = new();

        // onNewAthernetPacketAvailable 
        // onNewEthernetPacketAvailable
        // SendAthernetPacket
        // SendEthernetPacket

        private readonly Dictionary<NatEntry, NatEntry> _natTable;

        // receive only tcp & ports in table
        private void OnAthernetPacketAvailable(byte[] data)
        {
            var ipV4Datagram = new Packet(data, DateTime.Now, DataLinkKind.IpV4).IpV4;
            var ipV4Layer = (IpV4Layer) ipV4Datagram.ExtractLayer();

            if (ipV4Layer.Protocol == IpV4Protocol.InternetControlMessageProtocol)
            {
                var athernetEntry = new NatEntry(ProtocolType.Icmp, ipV4Layer.Source.ToString(), 0);

                _natTable.TryGetValue(athernetEntry, out var ethernetEntry);
                if (ethernetEntry == null)
                {
                    ethernetEntry = new NatEntry(ProtocolType.Icmp, _localAddress.ToString(), 0);
                    _natTable.Add(athernetEntry, ethernetEntry);
                    _natTable.Add(ethernetEntry, athernetEntry);
                }

                ipV4Layer.Source = new IpV4Address(ethernetEntry.Ip);
                ipV4Layer.HeaderChecksum = null;
                
                Console.WriteLine(
                    $"-> [NAT] ICMP");
                
                var modifiedPacket =
                    PacketBuilder.Build(DateTime.Now, ipV4Layer, ipV4Datagram.Payload.ExtractLayer());
                SendEthernetPacket(modifiedPacket);
            }
            else
            {
                var tcpDatagram = ipV4Datagram.Tcp;
                var tcpLayer = (TcpLayer) tcpDatagram.ExtractLayer();

                var athernetEntry = new NatEntry(ProtocolType.Tcp, ipV4Layer.Source.ToString(), tcpLayer.SourcePort);

                _natTable.TryGetValue(athernetEntry, out var ethernetEntry);
                if (ethernetEntry == null)
                {
                    ethernetEntry = new NatEntry(ProtocolType.Tcp, _localAddress.ToString(),
                        (ushort) _random.Next(8192, 65535));
                    _natTable.Add(athernetEntry, ethernetEntry);
                    _natTable.Add(ethernetEntry, athernetEntry);
                    _filterPorts.Add(ethernetEntry.Id);
                    _communicator.SetFilter(FilterString);
                    Console.WriteLine(FilterString);
                }
                //
                // if (ethernetEntry == null)
                // {
                //     ethernetEntry = new NatEntry(ProtocolType.Tcp, _localAddress.ToString(), (ushort) _random.Next(8192, 65535));
                //     _natTable.Add(athernetEntry, ethernetEntry);
                //     _filterPorts.Add(ethernetEntry.Id);
                //     _communicator.SetFilter(FilterString);
                // }


                tcpLayer.SourcePort = ethernetEntry.Id;
                ipV4Layer.Source = new IpV4Address(ethernetEntry.Ip);
                ipV4Layer.HeaderChecksum = null;
                tcpLayer.Checksum = null;

                Console.WriteLine(
                    $"-> [NAT] {tcpLayer.ControlBits} Seq={tcpLayer.SequenceNumber} Win={tcpLayer.Window} Ack={tcpLayer.AcknowledgmentNumber} PayloadLen={tcpDatagram.PayloadLength}");

                var modifiedPacket =
                    PacketBuilder.Build(DateTime.Now, ipV4Layer, tcpLayer, tcpDatagram.Payload.ExtractLayer());
                SendEthernetPacket(modifiedPacket);
            }
        }

        private void SendEthernetPacket(Packet packet)
        {
            var ethernetPacket = PacketBuilder.Build(DateTime.Now, _ethernetLayer, packet.IpV4.ExtractLayer(),
                packet.IpV4.Payload.ExtractLayer());
            _communicator.SendPacket(ethernetPacket);
        }

        private void OnEthernetPacketAvailable(Packet packet)
        {
            var ipV4Datagram = packet.Ethernet.IpV4;
            var ipV4Layer = (IpV4Layer) ipV4Datagram.ExtractLayer();
            
            if (ipV4Layer.Protocol == IpV4Protocol.InternetControlMessageProtocol)
            {
                var ethernetEntry =
                    new NatEntry(ProtocolType.Icmp, ipV4Layer.Destination.ToString(), 0);

                _natTable.TryGetValue(ethernetEntry, out var athernetEntry);

                if (athernetEntry == null)
                {
                    throw new KeyNotFoundException("athernet entry not exist");
                }

                Console.WriteLine(
                    $"<- [NAT] ICMP");

                ipV4Layer.CurrentDestination = new IpV4Address(athernetEntry.Ip);
                ipV4Layer.HeaderChecksum = null;
                var modifiedPacket =
                    PacketBuilder.Build(DateTime.Now, ipV4Layer, ipV4Datagram.Payload.ExtractLayer());
                SendAthernetPacket(modifiedPacket);
            }
            else
            {
                var tcpDatagram = ipV4Datagram.Tcp;
                var tcpLayer = (TcpLayer) tcpDatagram.ExtractLayer();

                var ethernetEntry =
                    new NatEntry(ProtocolType.Tcp, ipV4Layer.Destination.ToString(), tcpLayer.DestinationPort);

                _natTable.TryGetValue(ethernetEntry, out var athernetEntry);

                if (athernetEntry == null)
                {
                    throw new KeyNotFoundException("athernet entry not exist");
                }

                tcpLayer.DestinationPort = athernetEntry.Id;
                ipV4Layer.CurrentDestination = new IpV4Address(athernetEntry.Ip);
                tcpLayer.Checksum = ipV4Layer.HeaderChecksum = null;

                Console.WriteLine(
                    $"<- [NAT] {tcpLayer.ControlBits} Seq={tcpLayer.SequenceNumber} Win={tcpLayer.Window} Ack={tcpLayer.AcknowledgmentNumber} PayloadLen={tcpDatagram.PayloadLength}");

                var modifiedPacket =
                    PacketBuilder.Build(DateTime.Now, ipV4Layer, tcpLayer, tcpDatagram.Payload.ExtractLayer());
                SendAthernetPacket(modifiedPacket);
            }
        }

        private void SendAthernetPacket(Packet packet)
        {
            _athernetMac.AddPayload(1, packet.Buffer);
        }

        public void Listen()
        {
            _communicator.SetFilter(FilterString);
            Task.Run(() => _communicator.ReceivePackets(0, OnEthernetPacketAvailable));
            _athernetMac.DataAvailable += (sender, args) => OnAthernetPacketAvailable(args.Data);
            _athernetMac.StartReceive();
        }

        public void Break()
        {
            _communicator.Break();
            _athernetMac.StopReceive();
        }
    }
}