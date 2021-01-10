using System;
using System.Net;
using System.Net.Sockets;
using Athernet.IPLayer.Header;

namespace Athernet.IPLayer.Packet
{
    public class Ipv4Packet : ProtocolPacket
    {
        private Ipv4Header _header;

        public Ipv4Packet()
        {
            Header = new Ipv4Header();
            Payload = new byte[128]; // Default to be zero
        }

        public static Ipv4Packet Create(IPEndPoint sourceEndpoint, IPEndPoint destEndPoint, ProtocolType protocolType,
            byte[] payload)
        {
            return new()
            {
                Header = new Ipv4Header
                {
                    SourceAddress = sourceEndpoint.Address,
                    DestinationAddress = destEndPoint.Address
                },
                TransportHeader = protocolType switch
                {
                    ProtocolType.Udp => new UdpHeader
                    {
                        SourcePort = (ushort) sourceEndpoint.Port,
                        DestinationPort = (ushort) destEndPoint.Port
                    },
                    _ => throw new NotImplementedException()
                },
                Payload = payload
            };
        }

        public Ipv4Header Header
        {
            get => _header;
            set
            {
                if (value.Version != 4)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The version in header must be 4!");
                }

                _header = value;
            }
        }

        public TransportHeader TransportHeader { get; set; }
        public byte[] Payload { get; set; }

        public static Ipv4Packet Parse(byte[] packet)
        {
            var ret = new Ipv4Packet();
            var header = packet[..Ipv4Header.Ipv4HeaderLength];

            ret.Header = Ipv4Header.Create(header);

            var ipv4Payload = packet[ret.Header.HeaderLength..ret.Header.TotalLength];

            switch (ret.Header.Protocol)
            {
                case ProtocolType.Icmp:
                    var icmpPacket = IcmpPacket.Parse(ipv4Payload);
                    ret.TransportHeader = icmpPacket.Header;
                    ret.Payload = icmpPacket.Payload;
                    break;
                case ProtocolType.Udp:
                    var udpPacket = UdpPacket.Parse(ipv4Payload);
                    ret.TransportHeader = udpPacket.Header;
                    ret.Payload = udpPacket.Payload;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return ret;
        }

        public byte[] GetProtocolPacketBytes() => Header.GetProtocolPacketBytes(TransportHeader, Payload);

        public override string ToString()
        {
            return $"[IPv4] {Header}\n" +
                   $"       {TransportHeader}";
        }
    }
}