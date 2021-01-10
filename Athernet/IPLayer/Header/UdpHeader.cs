using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Athernet.IPLayer.Header
{
    /// <summary>
    /// Class that represents the UDP protocol.
    /// </summary>
    public class UdpHeader : TransportHeader
    {
        internal Ipv4Header Ipv4PacketHeader;

        public static readonly int UdpHeaderLength = 8;
        public override int HeaderLength => UdpHeaderLength;

        /// <summary>
        /// Simple constructor for the UDP header.
        /// </summary>
        public UdpHeader()
        {
            _sourcePortRaw = 0;
            _destinationPortRaw = 0;
            _lengthRaw = 0;
            _udpChecksum = 0;

            Ipv4PacketHeader = null;
        }

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort SourcePort
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _sourcePortRaw);
            set => _sourcePortRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        /// <summary>
        /// Gets the destination port. Performs NO byte order conversion.
        /// </summary>
        private ushort _sourcePortRaw;

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort DestinationPort
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _destinationPortRaw);
            set => _destinationPortRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        private ushort _destinationPortRaw;

        /// <summary>
        /// Gets and sets the UDP payload length. This is the length of the payload
        /// plus the size of the UDP header itself.
        /// </summary>
        public ushort Length
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _lengthRaw);
            set => _lengthRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        private ushort _lengthRaw;

        /// <summary>
        /// Gets and sets the checksum value. It performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _udpChecksum);
            set => _udpChecksum = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        private ushort _udpChecksum;

        /// <summary>
        /// Create a UDP Header from the Packet
        /// </summary>
        public static UdpHeader Create(byte[] udpPacket)
        {
            var udpPacketHeader = new UdpHeader
            {
                _sourcePortRaw = BitConverter.ToUInt16(udpPacket, 0),
                _destinationPortRaw = BitConverter.ToUInt16(udpPacket, 2),
                _lengthRaw = BitConverter.ToUInt16(udpPacket, 4),
                _udpChecksum = BitConverter.ToUInt16(udpPacket, 6)
            };

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
        public override byte[] GetProtocolPacketBytes(ReadOnlySpan<byte> payLoad)
        {
            Length = (ushort) (HeaderLength + payLoad.Length);
            
            byte[] pseudoHeader;
            
            if (Ipv4PacketHeader != null)
            {
                pseudoHeader = new byte[12];
                var pseudoStream = new MemoryStream(pseudoHeader);
                pseudoStream.Write(Ipv4PacketHeader.SourceAddress.GetAddressBytes());
                pseudoStream.Write(Ipv4PacketHeader.DestinationAddress.GetAddressBytes());
                pseudoStream.WriteByte(0);
                pseudoStream.WriteByte((byte)Ipv4PacketHeader.Protocol);
                pseudoStream.Write(BitConverter.GetBytes(_lengthRaw));
            }
            else
            {
                throw new NullReferenceException("Ipv4PacketHeader");
            }

            var udpPacket = new byte[pseudoHeader.Length + HeaderLength + payLoad.Length];
            var memoryStream = new MemoryStream(udpPacket);

            memoryStream.Write(pseudoHeader);
            memoryStream.Write(BitConverter.GetBytes(_sourcePortRaw));
            memoryStream.Write(BitConverter.GetBytes(_destinationPortRaw));
            memoryStream.Write(BitConverter.GetBytes(_lengthRaw));
            memoryStream.Write(new byte[2]);
            memoryStream.Write(payLoad);
            
            Checksum = ComputeChecksum(udpPacket);
            memoryStream.Seek(18, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes(_udpChecksum));
            
            return udpPacket[12..];
        }

        public override string ToString()
        {
            return $"(UDP) {SourcePort} -> {DestinationPort} Len: {Length}";
        }
    }
}