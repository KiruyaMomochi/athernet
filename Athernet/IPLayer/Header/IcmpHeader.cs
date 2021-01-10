using System;
using System.IO;
using System.Net;
using Athernet.IPLayer.Packet;

namespace Athernet.IPLayer.Header
{
    /// <summary>
    /// The ICMP protocol header used with the IPv4 protocol.
    /// </summary>
    public class IcmpHeader: TransportHeader
    {
        private byte _icmpType;                   // ICMP message type
        private ushort _icmpId;                     // Message ID

        public const int IcmpHeaderLength = 8;    // Length of ICMP header
        public override int HeaderLength => IcmpHeaderLength;
        
        /// <summary>
        /// ICMP message type.
        /// </summary>
        public IcmpType Type
        {
            get => (IcmpType)_icmpType;
            set => _icmpType = (byte)value;
        }

        /// <summary>
        /// ICMP message code.
        /// </summary>
        public byte Code { get; set; }

        /// <summary>
        /// Checksum of ICMP packet and payload.  Performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_checksumRaw);
            set => _checksumRaw = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        private ushort _checksumRaw;

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
            get => (ushort)IPAddress.NetworkToHostOrder((short)_sequenceRaw);
            set => _sequenceRaw = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        private ushort _sequenceRaw;

        /// <summary>
        /// This routine creates an instance of the IcmpHeader class from a byte
        /// array that is a received IGMP packet. This is useful when a packet
        /// is received from the network and the header object needs to be
        /// constructed from those values. 
        /// </summary>
        /// <param name="icmpHeaderBytes">Byte array containing the binary ICMP header</param>
        /// <returns>Returns the IcmpHeader object created from the byte array</returns>
        public static IcmpHeader Create(byte[] icmpHeaderBytes)
        {
            // Make sure byte array is large enough to contain an ICMP header
            if (icmpHeaderBytes.Length < IcmpHeaderLength)
                throw new ArgumentException("The header is too large for a IPv4 header", nameof(icmpHeaderBytes));
            
            var icmpHeader = new IcmpHeader();
            var binaryReader = new BinaryReader(new MemoryStream(icmpHeaderBytes));
            
            icmpHeader._icmpType = binaryReader.ReadByte();
            icmpHeader.Code = binaryReader.ReadByte();
            icmpHeader._checksumRaw = binaryReader.ReadUInt16();
            icmpHeader._icmpId = binaryReader.ReadUInt16();
            icmpHeader._sequenceRaw = binaryReader.ReadUInt16();

            return icmpHeader;
        }

        /// <summary>
        /// This routine builds the ICMP packet suitable for sending on a raw socket.
        /// It builds the ICMP packet and payload into a byte array and computes
        /// the checksum.
        /// </summary>
        /// <param name="payLoad">Data payload of the ICMP packet</param>
        /// <returns>Byte array representing the ICMP packet and payload</returns>
        public override byte[] GetProtocolPacketBytes(ReadOnlySpan<byte> payLoad)
        {
            var icmpPacket = new byte[IcmpHeaderLength + payLoad.Length];
            var byteWriter = new MemoryStream(icmpPacket);

            byteWriter.WriteByte((byte)Type);
            byteWriter.WriteByte(Code);
            byteWriter.WriteByte(0);
            byteWriter.WriteByte(0);
            byteWriter.Write(BitConverter.GetBytes(IdRaw));
            byteWriter.Write(BitConverter.GetBytes(_sequenceRaw));
            byteWriter.Write(payLoad);

            // Compute the checksum over the entire packet
            Checksum = ComputeChecksum(icmpPacket);

            // Put the checksum back into the packet
            byteWriter.Seek(2, SeekOrigin.Begin);
            byteWriter.Write(BitConverter.GetBytes(_checksumRaw));

            return icmpPacket;
        }

        public bool ValidateChecksum(byte[] payload) => ValidateCheckSum(Checksum, payload);

        public bool ValidateCheckSum(ushort checksum, byte[] payLoad)
        {
            var icmpPacket = new byte[IcmpHeaderLength + payLoad.Length];
            var byteWriter = new MemoryStream(icmpPacket);

            byteWriter.WriteByte((byte)Type);
            byteWriter.WriteByte(Code);
            byteWriter.WriteByte(0);
            byteWriter.WriteByte(0);
            byteWriter.Write(BitConverter.GetBytes(IdRaw));
            byteWriter.Write(BitConverter.GetBytes(_sequenceRaw));
            byteWriter.Write(payLoad);

            // Compute the checksum over the entire packet
            var trueSum = ComputeChecksum(icmpPacket);
            return checksum == trueSum;
        }
    }
}