using System;
using System.IO;
using System.Net;
using ToyNet.IpInterface.Packet;

namespace ToyNet.IpInterface.Header
{
    /// <summary>
    /// The ICMP protocol header used with the IPv4 protocol.
    /// </summary>
    internal class IcmpHeader: ProtocolHeader
    {
        private byte _icmpType;                   // ICMP message type
        private byte _icmpCode;                   // ICMP message code
        private ushort _icmpChecksum;               // Checksum of ICMP header and payload
        private ushort _icmpId;                     // Message ID
        private ushort _icmpSequence;               // ICMP sequence number

        public static byte EchoRequestType = 8;     // ICMP echo request
        public static byte EchoRequestCode = 0;     // ICMP echo request code
        public static byte EchoReplyType = 0;     // ICMP echo reply
        public static byte EchoReplyCode = 0;     // ICMP echo reply code

        public static int IcmpHeaderLength = 8;    // Length of ICMP header

        /// <summary>
        /// Default constructor for ICMP packet
        /// </summary>
        public IcmpHeader()
            : base()
        {
            _icmpType = 0;
            _icmpCode = 0;
            _icmpChecksum = 0;
            _icmpId = 0;
            _icmpSequence = 0;
        }

        /// <summary>
        /// ICMP message type.
        /// </summary>
        public ICMPType Type
        {
            get => (ICMPType)_icmpType;
            set => _icmpType = (byte)value;
        }

        /// <summary>
        /// ICMP message code.
        /// </summary>
        public byte Code
        {
            get => _icmpCode;
            set => _icmpCode = value;
        }

        /// <summary>
        /// Checksum of ICMP packet and payload.  Performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_icmpChecksum);
            set => _icmpChecksum = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public ushort ChecksumRaw => _icmpChecksum;

        /// <summary>
        /// ICMP message ID. Used to uniquely identify the source of the ICMP packet.
        /// Performs the necessary byte order conversion.
        /// </summary>
        public ushort Id
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_icmpId);
            set => _icmpId = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public ushort IdRaw => _icmpId;

        /// <summary>
        /// ICMP sequence number. As each ICMP message is sent the sequence should be incremented.
        /// Performs the necessary byte order conversion.
        /// </summary>
        public ushort Sequence
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_icmpSequence);
            set => _icmpSequence = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public ushort SequenceRaw => _icmpSequence;

        /// <summary>
        /// This routine creates an instance of the IcmpHeader class from a byte
        /// array that is a received IGMP packet. This is useful when a packet
        /// is received from the network and the header object needs to be
        /// constructed from those values. 
        /// </summary>
        /// <param name="icmpPacket">Byte array containing the binary ICMP header</param>
        /// <param name="bytesCopied">Number of bytes used in header</param>
        /// <returns>Returns the IcmpHeader object created from the byte array</returns>
        public static IcmpHeader Create(byte[] icmpPacket, ref int bytesCopied)
        {
            var icmpHeader = new IcmpHeader();
            var offset = 0;

            // Make sure byte array is large enough to contain an ICMP header
            if (icmpPacket.Length < IcmpHeader.IcmpHeaderLength)
                return null;

            icmpHeader._icmpType = icmpPacket[offset++];
            icmpHeader._icmpCode = icmpPacket[offset++];
            icmpHeader._icmpChecksum = BitConverter.ToUInt16(icmpPacket, offset);
            offset += 2;
            icmpHeader._icmpId = BitConverter.ToUInt16(icmpPacket, offset);
            offset += 2;
            icmpHeader._icmpSequence = BitConverter.ToUInt16(icmpPacket, offset);

            bytesCopied = IcmpHeader.IcmpHeaderLength;

            return icmpHeader;
        }

        /// <summary>
        /// This routine builds the ICMP packet suitable for sending on a raw socket.
        /// It builds the ICMP packet and payload into a byte array and computes
        /// the checksum.
        /// </summary>
        /// <param name="payLoad">Data payload of the ICMP packet</param>
        /// <returns>Byte array representing the ICMP packet and payload</returns>
        public override byte[] GetProtocolPacketBytes(byte[] payLoad)
        {
            var icmpPacket = new byte[IcmpHeader.IcmpHeaderLength + payLoad.Length];
            var byteWriter = new MemoryStream(icmpPacket);

            byteWriter.WriteByte((byte)Type);
            byteWriter.WriteByte(Code);
            byteWriter.WriteByte(0);
            byteWriter.WriteByte(0);
            byteWriter.Write(BitConverter.GetBytes(IdRaw));
            byteWriter.Write(BitConverter.GetBytes(SequenceRaw));
            byteWriter.Write(payLoad);

            // Compute the checksum over the entire packet
            Checksum = ComputeChecksum(icmpPacket);

            // Put the checksum back into the packet
            byteWriter.Write(BitConverter.GetBytes(ChecksumRaw));

            return icmpPacket;
        }
    }
}