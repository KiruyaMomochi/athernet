using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Athernet.IPLayer.Header
{
    /// <summary>
    /// This is the IPv4 protocol header.
    /// </summary>
    public class Ipv4Header : ProtocolHeader
    {
        private ushort _ipChecksum;

        public const int Ipv4HeaderLength = 20;
        public override int HeaderLength => Ipv4HeaderLength;

        /// <summary>
        /// Simple constructor that initializes the members to zero.
        /// </summary>
        public Ipv4Header()
        {
            Version = 4;
            Length = Ipv4HeaderLength; // Set the property so it will convert properly
            TypeOfService = 0;
            Id = 0;
            Offset = 0;
            Ttl = 2;
            Protocol = 0;
            Checksum = 0;
            SourceAddress = IPAddress.Any;
            DestinationAddress = IPAddress.Any;
        }

        /// <summary>
        /// Gets and sets the IP version. This should be 4 to indicate the IPv4 header.
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Gets and sets the length of the IPv4 header. This property takes and returns
        /// the number of bytes, but the actual field is the number of 32-bit DWORDs
        /// (the IPv4 header is a multiple of 4-bytes).
        /// </summary>
        public byte Length
        {
            get => (byte) (_lengthRaw << 2);
            set => _lengthRaw = (byte) (value >> 2);
        }

        /// <summary>
        /// Gets the RAW length of the IPv4 header.
        /// </summary>
        private byte _lengthRaw;

        /// <summary>
        /// Gets and sets the type of service field of the IPv4 header. Since it
        /// is a byte, no byte order conversion is required.
        /// </summary>
        public byte TypeOfService { get; set; }

        /// <summary>
        ///  Gets and sets the total length of the IPv4 header and its encapsulated
        ///  payload. Byte order conversion is required.
        /// </summary>
        public ushort TotalLength
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _totalLengthRaw);
            set => _totalLengthRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        /// <summary>
        ///  [NO ORDER CONVERSION] Gets the RAW total length of the IPv4 header and its encapsulated
        ///  payload. Byte order conversion is NOT required.
        /// </summary>
        private ushort _totalLengthRaw;

        /// <summary>
        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is
        /// requried.
        /// </summary>
        public ushort Id
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _idRaw);
            set => _idRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        /// <summary>
        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is
        /// requried.
        /// </summary>
        private ushort _idRaw;

        public byte Flags { get; set; }

        /// <summary>
        /// Gets and sets the offset field of the IPv4 header which indicates if
        /// IP fragmentation has occured.
        /// </summary>
        public ushort Offset
        {
            get => (ushort) IPAddress.NetworkToHostOrder((short) _offsetRaw);
            set => _offsetRaw = (ushort) IPAddress.HostToNetworkOrder((short) value);
        }

        /// <summary>
        /// [NO ORDER CONVERSION] Gets the offset field of the IPv4 header which indicates if
        /// IP fragmentation has occured.
        /// </summary>
        private ushort _offsetRaw;

        private byte _protocol;


        /// <summary>
        /// Gets and sets the time-to-live (TTL) value of the IP header. This field
        /// determines how many router hops the packet is valid for.
        /// </summary>
        public byte Ttl { get; set; }

        /// <summary>
        /// Gets and sets the protocol field of the IPv4 header. This field indicates
        /// what the encapsulated protocol is.
        /// </summary>
        public ProtocolType Protocol
        {
            get => (ProtocolType) _protocol;
            set => _protocol = (byte) value;
        }

        /// <summary>
        /// Gets and sets the checksum field of the IPv4 header. For the IPv4 header, the 
        /// checksum is calculated over the header and payload. Note that this field isn't
        /// meant to be set by the user as the GetProtocolPacketBytes method computes the
        /// checksum when the packet is built.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort) (IPAddress.NetworkToHostOrder(_ipChecksum) << 16);
            set => _ipChecksum = (ushort) (IPAddress.HostToNetworkOrder(value) >> 16);
        }

        /// <summary>
        /// Gets and sets the source IP address of the IPv4 packet. This is stored
        /// as an IPAddress object which will be serialized to the appropriate
        /// byte representation in the GetProtocolPacketBytes method.
        /// </summary>
        public IPAddress SourceAddress { get; set; }

        /// <summary>
        /// Gets and sets the destination IP address of the IPv4 packet. This is stored
        /// as an IPAddress object which will be serialized to the appropriate byte
        /// representation in the GetProtocolPacketBytes method.
        /// </summary>
        public IPAddress DestinationAddress { get; set; }

        /// <summary>
        /// This routine creates an instance of the Ipv4Header class from a byte
        /// array that is a received IGMP packet. This is useful when a packet
        /// is received from the network and the header object needs to be
        /// constructed from those values.
        /// </summary>
        /// <param name="ipv4HeaderBytes">Byte array containing the binary IPv4 header</param>
        /// <returns>Returns the Ipv4Header object created from the byte array</returns>
        public static Ipv4Header Create(byte[] ipv4HeaderBytes)
        {
            // Make sure byte array is large enough to contain an IPv4 header
            if (ipv4HeaderBytes.Length < Ipv4HeaderLength)
                throw new ArgumentException("The header is too large for a IPv4 header", nameof(ipv4HeaderBytes));

            var ipv4Header = new Ipv4Header();
            var binaryReader = new BinaryReader(new MemoryStream(ipv4HeaderBytes));

            var byte0 = binaryReader.ReadByte();
            ipv4Header.Version = (byte) ((byte0 >> 4) & 0xF);
            ipv4Header._lengthRaw = (byte) (byte0 & 0xF);

            ipv4Header.TypeOfService = binaryReader.ReadByte();
            ipv4Header._totalLengthRaw = binaryReader.ReadUInt16();
            ipv4Header._idRaw = binaryReader.ReadUInt16();
            ipv4Header._offsetRaw = binaryReader.ReadUInt16();
            ipv4Header.Ttl = binaryReader.ReadByte();
            ipv4Header._protocol = binaryReader.ReadByte();
            ipv4Header._ipChecksum = binaryReader.ReadUInt16();
            ipv4Header.SourceAddress = new IPAddress(binaryReader.ReadUInt32());
            ipv4Header.DestinationAddress = new IPAddress(binaryReader.ReadUInt32());

            return ipv4Header;
        }

        /// <summary>
        /// This routine takes the properties of the IPv4 header and marshals them into
        /// a byte array representing the IPv4 header that is to be sent on the wire.
        /// </summary>
        /// <param name="header">The encapsulated header</param>
        /// <param name="payLoad">The encapsulated data</param>
        /// <returns>A byte array of the IPv4 header and payload</returns>
        public byte[] GetProtocolPacketBytes(TransportHeader header, ReadOnlySpan<byte> payLoad)
        {
            switch (header)
            {
                case UdpHeader udpHeader:
                    udpHeader.Ipv4PacketHeader = this;
                    Protocol = ProtocolType.Udp;
                    break;
                case IcmpHeader icmpHeader:
                    Protocol = ProtocolType.Icmp;
                    break;
                case TcpHeader tcpHeader:
                    tcpHeader.Ipv4PacketHeader = this;
                    Protocol = ProtocolType.Tcp;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
            
            // Allocate space for the IPv4 header plus payload
            TotalLength = (ushort) (Ipv4HeaderLength + header.HeaderLength + payLoad.Length);

            var ipv4Packet = new byte[Ipv4HeaderLength + header.HeaderLength + payLoad.Length];
            var memoryStream = new MemoryStream(ipv4Packet);

            memoryStream.WriteByte((byte) ((Version << 4) | _lengthRaw));
            memoryStream.WriteByte(TypeOfService);
            memoryStream.Write(BitConverter.GetBytes(_totalLengthRaw));
            memoryStream.Write(BitConverter.GetBytes(_idRaw));
            var offsets = BitConverter.GetBytes(_offsetRaw);
            memoryStream.WriteByte( (byte) (Flags | (offsets[0] & 0x1F)));
            memoryStream.WriteByte(offsets[1]);

            memoryStream.WriteByte(Ttl);
            memoryStream.WriteByte(_protocol);

            // Zero the checksum for now since we will calculate it later
            memoryStream.Write(new byte[2]);

            memoryStream.Write(SourceAddress.GetAddressBytes());
            memoryStream.Write(DestinationAddress.GetAddressBytes());

            memoryStream.Write(header.GetProtocolPacketBytes(payLoad));

            // Compute the checksum over the entire packet (IPv4 header)
            Checksum = ComputeChecksum(ipv4Packet[..Ipv4HeaderLength]);
            // Set the checksum into the built packet
            memoryStream.Seek(10, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes(_ipChecksum));

            return ipv4Packet;
        }

        public override string ToString()
        {
            return $"{SourceAddress} -> {DestinationAddress} {Protocol}";
        }
    }
}