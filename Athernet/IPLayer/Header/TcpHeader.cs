using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Athernet.IPLayer.Packet;

namespace Athernet.IPLayer.Header
{
    public class TcpHeader : TransportHeader
    {
        public Ipv4Header Ipv4PacketHeader;
        private ushort _sourcePortRaw;
        private ushort _destinationPortRaw;
        private uint _sequenceNumberRaw;
        private uint _acknowledgementNumberRaw;
        private ushort _flagsRaw;
        private ushort _windowSizeRaw;
        private ushort _checksumRaw;
        private ushort _urgentPointerRaw;

        public static readonly int TcpHeaderLength = 20;

        public ushort SourcePort
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _sourcePortRaw);
            set => _sourcePortRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort DestinationPort
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _destinationPortRaw);
            set => _destinationPortRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public uint SequenceNumber
        {
            get => (uint) IPAddress.NetworkToHostOrder((int) _sequenceNumberRaw);
            set => _sequenceNumberRaw = (uint) IPAddress.HostToNetworkOrder((int) value);
        }

        public uint AcknowledgementNumber
        {
            get => (uint) IPAddress.NetworkToHostOrder((int) _acknowledgementNumberRaw);
            set => _acknowledgementNumberRaw = (uint) IPAddress.HostToNetworkOrder((int) value);
        }

        public byte DataOffset { get; set; }

        public TcpFlags Flags
        {
            get => (TcpFlags) IPAddress.NetworkToHostOrder((short) _flagsRaw);
            set => _flagsRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort WindowSize
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _windowSizeRaw);
            set => _windowSizeRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort Checksum
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _checksumRaw);
            set => _checksumRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public ushort UrgentPointer
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _urgentPointerRaw);
            set => _urgentPointerRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        public byte[] Options { get; set; }

        public TcpHeader()
        {
            _sourcePortRaw = 0;
            _destinationPortRaw = 0;
            _windowSizeRaw = 65535;
        }

        /// <summary>
        /// Create a TCP Header from the Packet
        /// </summary>
        public static TcpHeader Create(byte[] tcpPacket)
        {
            if (tcpPacket.Length < TcpHeaderLength)
                throw new ArgumentException("The header is too large for a TCP header", nameof(tcpPacket));

            var tcpHeader = new TcpHeader();
            var binaryReader = new BinaryReader(new MemoryStream(tcpPacket));

            tcpHeader.SourcePort = binaryReader.ReadUInt16();
            tcpHeader.DestinationPort = binaryReader.ReadUInt16();
            tcpHeader.SequenceNumber = binaryReader.ReadUInt32();
            tcpHeader.AcknowledgementNumber = binaryReader.ReadUInt32();

            var byte12 = binaryReader.ReadByte();
            tcpHeader.DataOffset = (byte) ((byte12 >> 4) & 0xF);

            var byte13 = binaryReader.ReadByte();
            tcpHeader._flagsRaw = (ushort) ((byte12 << 4) | byte13);

            tcpHeader.WindowSize = binaryReader.ReadUInt16();
            tcpHeader.Checksum = binaryReader.ReadUInt16();
            tcpHeader.UrgentPointer = binaryReader.ReadUInt16();

            var optionsLength = tcpHeader.DataOffset * 4 - TcpHeaderLength;
            tcpHeader.Options = binaryReader.ReadBytes(optionsLength);

            return tcpHeader;
        }

        public override byte[] GetProtocolPacketBytes(ReadOnlySpan<byte> payLoad)
        {
            byte[] pseudoHeader;
            
            Trace.Assert(HeaderLength == TcpHeaderLength + Options.Length,
                $"{HeaderLength} == {TcpHeaderLength} + {Options.Length}");

            if (Ipv4PacketHeader != null)
            {
                pseudoHeader = new byte[12];
                var pseudoStream = new MemoryStream(pseudoHeader);

                var length = IPAddress.HostToNetworkOrder((short) (HeaderLength + payLoad.Length));

                pseudoStream.Write(Ipv4PacketHeader.SourceAddress.GetAddressBytes());
                pseudoStream.Write(Ipv4PacketHeader.DestinationAddress.GetAddressBytes());
                pseudoStream.WriteByte(0);
                pseudoStream.WriteByte((byte) Ipv4PacketHeader.Protocol);
                pseudoStream.Write(BitConverter.GetBytes(length));
            }
            else
            {
                throw new NullReferenceException("Ipv4PacketHeader");
            }

            var tcpPacket = new byte[pseudoHeader.Length + HeaderLength + payLoad.Length];
            var memoryStream = new MemoryStream(tcpPacket);

            memoryStream.Write(pseudoHeader);
            memoryStream.Write(BitConverter.GetBytes(_sourcePortRaw));
            memoryStream.Write(BitConverter.GetBytes(_destinationPortRaw));
            memoryStream.Write(BitConverter.GetBytes(_sequenceNumberRaw));
            memoryStream.Write(BitConverter.GetBytes(_acknowledgementNumberRaw));

            var flags = BitConverter.GetBytes(_flagsRaw);
            memoryStream.WriteByte((byte) ((DataOffset << 4) | (flags[0] & 0xF)));
            memoryStream.WriteByte(flags[1]);

            memoryStream.Write(BitConverter.GetBytes(_windowSizeRaw));
            memoryStream.Write(new byte[2]);
            memoryStream.Write(BitConverter.GetBytes(_urgentPointerRaw));
            memoryStream.Write(Options);
            memoryStream.Write(payLoad);

            Checksum = ComputeChecksum(tcpPacket);
            memoryStream.Seek(pseudoHeader.Length + 16, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes(_checksumRaw));

            return tcpPacket[12..];
        }

        public override int HeaderLength => DataOffset * 4;
    }
}