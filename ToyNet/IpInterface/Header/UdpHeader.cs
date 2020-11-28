using System;
using System.Net;

namespace ToyNet.IpInterface.Header
{
    /// <summary>
    /// Class that represents the UDP protocol.
    /// </summary>
    internal class UdpHeader : ProtocolHeader
    {
        private ushort _srcPort;
        private ushort _destPort;
        private ushort _udpLength;
        private ushort _udpChecksum;

        public Ipv4Header Ipv4PacketHeader;

        public static int UdpHeaderLength = 8;

        /// <summary>
        /// Simple constructor for the UDP header.
        /// </summary>
        public UdpHeader()
            : base()
        {
            _srcPort = 0;
            _destPort = 0;
            _udpLength = 0;
            _udpChecksum = 0;

            Ipv4PacketHeader = null;
        }

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort SourcePort
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _srcPort);
            set => _srcPort = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        /// <summary>
        /// Gets the destination port. Performs NO byte order conversion.
        /// </summary>
        public ushort SourcePortRaw => _srcPort;

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort DestinationPort
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _destPort);
            set => _destPort = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort DestinationPortRaw => _destPort;

        /// <summary>
        /// Gets and sets the UDP payload length. This is the length of the payload
        /// plus the size of the UDP header itself.
        /// </summary>
        public ushort Length
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _udpLength);
            set => _udpLength = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort LengthRaw => _udpLength;

        /// <summary>
        /// Gets and sets the checksum value. It performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _udpChecksum);
            set => _udpChecksum = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort ChecksumRaw => _udpChecksum;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpData"></param>
        /// <param name="bytesCopied"></param>
        /// <returns></returns>
        public static UdpHeader Create(byte[] udpData, ref int bytesCopied)
        {
            var udpPacketHeader = new UdpHeader();

            udpPacketHeader._srcPort = BitConverter.ToUInt16(udpData, 0);
            udpPacketHeader._destPort = BitConverter.ToUInt16(udpData, 2);
            udpPacketHeader._udpLength = BitConverter.ToUInt16(udpData, 4);
            udpPacketHeader._udpChecksum = BitConverter.ToUInt16(udpData, 6);

            return udpPacketHeader;
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
            byteValue = BitConverter.GetBytes(SourcePortRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(DestinationPortRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(LengthRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            udpPacket[offset++] = 0; // Checksum is initially zero
            udpPacket[offset++] = 0;

            // Copy payload to end of packet
            Array.Copy(payLoad, 0, udpPacket, offset, payLoad.Length);

            if (Ipv4PacketHeader != null)
            {
                pseudoHeader = new byte[UdpHeader.UdpHeaderLength + 12 + payLoad.Length];

                // Build the IPv4 pseudo header
                offset = 0;

                // Source address
                byteValue = Ipv4PacketHeader.SourceAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Destination address
                byteValue = Ipv4PacketHeader.DestinationAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // 1 byte zero pad plus next header protocol value
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = Ipv4PacketHeader.Protocol;

                // Packet length
                byteValue = BitConverter.GetBytes(LengthRaw);
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Copy the UDP packet to the end of this
                Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);
            }

            if (pseudoHeader != null)
            {
                Checksum = ComputeChecksum(pseudoHeader);
            }

            // Put checksum back into packet
            byteValue = BitConverter.GetBytes(ChecksumRaw);
            Array.Copy(byteValue, 0, udpPacket, 6, byteValue.Length);

            return udpPacket;
        }
    }
}