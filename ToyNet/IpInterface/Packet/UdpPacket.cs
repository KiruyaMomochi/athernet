using System;
using ToyNet.IpInterface.Header;

namespace ToyNet.IpInterface.Packet
{
    internal class UdpPacket : ProtocolPacket
    {
        private UdpHeader _header;
        private byte[] _payload;
        public UdpPacket()
        {
            _header = new UdpHeader();
            _payload = new byte[128]; // Default to be zero
        }
        public UdpHeader Header
        {
            get => _header;
            set => _header = value;
        }

        public byte[] Payload
        {
            get => _payload;
            set => Buffer.BlockCopy(value, 0, _payload, 0, value.Length); // DeepCopy
        }
        public void SetHeader(ushort sourcePort, ushort destinationPort, int messageSize, Ipv4Header ipv4Header)
        {
            _header.SourcePort = sourcePort;
            _header.DestinationPort = destinationPort;
            _header.Length = (ushort) (UdpHeader.UdpHeaderLength + messageSize);
            // ↓ just initialized to zero, will get meaningful when ipv4 header is prepared.
            _header.Checksum = 0;
            // ↓ Set the ipv4 header in the UDP header since it is required to calculate pseudo-header checksum
            _header.Ipv4PacketHeader = ipv4Header;
        }
    }
}