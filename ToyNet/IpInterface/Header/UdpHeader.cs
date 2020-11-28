using System;
using System.Net;

namespace ToyNet.IpInterface.Header
{
    /// <summary>
    /// Class that represents the UDP protocol.
    /// </summary>
    class UdpHeader
    {
        private ushort _srcPort;
        private ushort _destPort;
        private ushort _udpLength;
        private ushort _udpChecksum;

        public Ipv6Header Ipv6PacketHeader;

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

            Ipv6PacketHeader = null;
            Ipv4PacketHeader = null;
        }

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort SourcePort
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_srcPort);
            set => _srcPort = (ushort)IPAddress.HostToNetworkOrder((short)value);
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
            get => (ushort)IPAddress.NetworkToHostOrder((short)_destPort);
            set => _destPort = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }
        public ushort DestinationPortRaw => _destPort;

        /// <summary>
        /// Gets and sets the UDP payload length. This is the length of the payload
        /// plus the size of the UDP header itself.
        /// </summary>
        public ushort Length
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_udpLength);
            set => _udpLength = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public ushort LengthRaw => _udpLength;

        /// <summary>
        /// Gets and sets the checksum value. It performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_udpChecksum);
            set => _udpChecksum = (ushort)IPAddress.HostToNetworkOrder((short)value);
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


    }
}