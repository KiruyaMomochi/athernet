using System;
using ToyNet.IpInterface.Header;

namespace ToyNet.IpInterface.Packet
{
    class UdpPacket : ProtocolPacket
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

        /// <summary>
        /// This method builds the byte array representation of the UDP header as it would appear
        /// on the wire. To do this it must build the IPv4 or IPv6 pseudo header in order to
        /// calculate the checksum on the packet. This requires knowledge of the IPv4 or IPv6 header
        /// so one of these must be set before a UDP packet can be set. 
        /// 
        /// The IPv4 pseudo header consists of:
        ///   4-byte source IP address
        ///   4-byte destination address
        ///   1-byte zero field
        ///   1-byte protocol field
        ///   2-byte UDP length
        ///   2-byte source port
        ///   2-byte destination port
        ///   2-byte UDP packet length
        ///   2-byte UDP checksum (zero)
        ///   UDP payload (padded to the next 16-bit boundary)
        /// The IPv6 pseudo header consists of:
        ///   16-byte source address
        ///   16-byte destination address
        ///   4-byte payload length
        ///   3-byte zero pad
        ///   1-byte protocol value
        ///   2-byte source port
        ///   2-byte destination port
        ///   2-byte UDP length
        ///   2-byte UDP checksum (zero)
        ///   UDP payload (padded to the next 16-bit boundary)
        /// </summary>
        /// <param name="payLoad">Payload that follows the UDP header</param>
        /// <returns></returns>
        public override byte[] GetProtocolPacketBytes(byte[] payLoad)
        {
            byte[] udpPacket = new byte[UdpHeader.UdpHeaderLength + payLoad.Length],
                pseudoHeader = null,
                byteValue = null;
            var offset = 0;

            // Build the UDP packet first
            byteValue = BitConverter.GetBytes(_header.SourcePortRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(_header.DestinationPortRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(_header.LengthRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            udpPacket[offset++] = 0;      // Checksum is initially zero
            udpPacket[offset++] = 0;

            // Copy payload to end of packet
            Array.Copy(payLoad, 0, udpPacket, offset, payLoad.Length);

            if (_header.Ipv4PacketHeader != null)
            {
                pseudoHeader = new byte[UdpHeader.UdpHeaderLength + 12 + payLoad.Length];

                // Build the IPv4 pseudo header
                offset = 0;

                // Source address
                byteValue = _header.Ipv4PacketHeader.SourceAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Destination address
                byteValue = _header.Ipv4PacketHeader.DestinationAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // 1 byte zero pad plus next header protocol value
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = _header.Ipv4PacketHeader.Protocol;

                // Packet length
                byteValue = BitConverter.GetBytes(_header.LengthRaw);
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Copy the UDP packet to the end of this
                Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);

            }
            // else if (header.ipv6PacketHeader != null)
            // {
            //     uint ipv6PayloadLength;

            //     pseudoHeader = new byte[UdpHeaderLength + 40 + payLoad.Length];

            //     // Build the IPv6 pseudo header
            //     offset = 0;

            //     // Source address
            //     byteValue = ipv6PacketHeader.SourceAddress.GetAddressBytes();
            //     Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
            //     offset += byteValue.Length;

            //     // Destination address
            //     byteValue = ipv6PacketHeader.DestinationAddress.GetAddressBytes();
            //     Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
            //     offset += byteValue.Length;

            //     ipv6PayloadLength = (uint)IPAddress.HostToNetworkOrder((int)(payLoad.Length + UdpHeaderLength));

            //     // Packet payload: ICMPv6 headers plus payload
            //     byteValue = BitConverter.GetBytes(ipv6PayloadLength);
            //     Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
            //     offset += byteValue.Length;

            //     // 3 bytes zero pad plus next header protocol value
            //     pseudoHeader[offset++] = 0;
            //     pseudoHeader[offset++] = 0;
            //     pseudoHeader[offset++] = 0;
            //     pseudoHeader[offset++] = ipv6PacketHeader.NextHeader;

            //     // Copy the UDP packet to the end of this
            //     Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);
            // }

            if (pseudoHeader != null)
            {
                _header.Checksum = ComputeChecksum(pseudoHeader);
            }

            // Put checksum back into packet
            byteValue = BitConverter.GetBytes(_header.ChecksumRaw);
            Array.Copy(byteValue, 0, udpPacket, 6, byteValue.Length);

            return udpPacket;
        }

    }
}