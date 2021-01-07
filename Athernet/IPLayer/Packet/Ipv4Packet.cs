using System;
using System.Net;
using System.Net.Sockets;
using Athernet.IPLayer.Header;

namespace Athernet.IPLayer.Packet
{
    public class Ipv4Packet : ProtocolPacket
    {
        public Ipv4Packet()
        {
            Header = new Ipv4Header();
            Payload = new byte[128]; // Default to be zero
        }

        public Ipv4Header Header { get; set; }
        public TcpHeader TcpHeader { get; set; }
        public byte[] Payload { get; set; }

        public void SetHeader(IPAddress ipSourceAddress, IPAddress ipDestAddress, int messageSize)
        {
            Header.Version = 4;
            Header.Protocol = ProtocolType.Udp;
            Header.Ttl = 2;
            Header.Offset = 0;
            Header.Length = (byte)Ipv4Header.Ipv4HeaderLength;
            Header.TotalLength = System.Convert.ToUInt16(
                Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength + messageSize);
            Header.SourceAddress = ipSourceAddress;
            Header.DestinationAddress = ipDestAddress;
        }
        
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
                    ret.TcpHeader = icmpPacket.Header;
                    ret.Payload = icmpPacket.Payload;
                    break;
                case ProtocolType.Udp:
                    var udpPacket = UdpPacket.Parse(ipv4Payload);
                    ret.TcpHeader = udpPacket.Header;
                    ret.Payload = udpPacket.Payload;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return ret;
        }

        public byte[] GetProtocolPacketBytes() => Header.GetProtocolPacketBytes(TcpHeader, Payload);
    }
}