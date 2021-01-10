using System;
using Athernet.IPLayer.Header;

namespace Athernet.IPLayer.Packet
{
    public class UdpPacket : ProtocolPacket
    {
        public UdpPacket()
        {
            Header = new UdpHeader();
            Payload = new byte[128]; // Default to be zero
        }
        public UdpHeader Header { get; set; }

        public byte[] Payload { get; set; }

        public void SetHeader(ushort sourcePort, ushort destinationPort, int messageSize, Ipv4Header ipv4Header)
        {
            Header.SourcePort = sourcePort;
            Header.DestinationPort = destinationPort;
            Header.Length = (ushort) (UdpHeader.UdpHeaderLength + messageSize);
            // ↓ just initialized to zero, will get meaningful when ipv4 header is prepared.
            Header.Checksum = 0;
            // ↓ Set the ipv4 header in the UDP header since it is required to calculate pseudo-header checksum
            Header.Ipv4PacketHeader = ipv4Header;
        }

        public static UdpPacket Parse(byte[] packet)
        {
            var header = packet[..UdpHeader.UdpHeaderLength];
            return new UdpPacket
            {
                Header = UdpHeader.Create(header),
                Payload = packet[UdpHeader.UdpHeaderLength..]
            };
        }
    }
}