using System;
using System.IO;
using System.Net;

namespace ToyNet.IpInterface.Header
{
    /// <summary>
    /// This is the IPv4 protocol header.
    /// </summary>
    internal class Ipv4Header: ProtocolHeader
    {
        private ushort _ipId;
        private ushort _ipChecksum;

        public static int Ipv4HeaderLength = 20;

        /// <summary>
        /// Simple constructor that initializes the members to zero.
        /// </summary>
        public Ipv4Header() : base()
        {
            Version = 4;
            Length = (byte)Ipv4HeaderLength;    // Set the property so it will convert properly
            TypeOfService = 0;
            Id = 0;
            OffsetRaw = 0;
            Ttl = 1;
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
            get => (byte)(_lengthRaw << 4);
            set => _lengthRaw = (byte)(value >> 4);
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
            get => (ushort)IPAddress.NetworkToHostOrder((short)TotalLengthRaw);
            set => TotalLengthRaw = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        /// <summary>
        ///  [NO ORDER CONVERSION] Gets the RAW total length of the IPv4 header and its encapsulated
        ///  payload. Byte order conversion is NOT required.
        /// </summary>
        public ushort TotalLengthRaw { get; private set; }

        /// <summary>
        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is
        /// requried.
        /// </summary>
        public ushort Id
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)_ipId);
            set => _ipId = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        /// <summary>
        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is
        /// requried.
        /// </summary>
        public ushort IdRaw => _ipId;

        /// <summary>
        /// Gets and sets the offset field of the IPv4 header which indicates if
        /// IP fragmentation has occured.
        /// </summary>
        public ushort Offset
        {
            get => (ushort)IPAddress.NetworkToHostOrder((short)OffsetRaw);
            set => OffsetRaw = (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        /// <summary>
        /// [NO ORDER CONVERSION] Gets the offset field of the IPv4 header which indicates if
        /// IP fragmentation has occured.
        /// </summary>
        public ushort OffsetRaw { get; private set; }


        /// <summary>
        /// Gets and sets the time-to-live (TTL) value of the IP header. This field
        /// determines how many router hops the packet is valid for.
        /// </summary>
        public byte Ttl { get; set; }

        /// <summary>
        /// Gets and sets the protocol field of the IPv4 header. This field indicates
        /// what the encapsulated protocol is.
        /// </summary>
        public byte Protocol { get; set; }

        /// <summary>
        /// Gets and sets the checksum field of the IPv4 header. For the IPv4 header, the 
        /// checksum is calculated over the header and payload. Note that this field isn't
        /// meant to be set by the user as the GetProtocolPacketBytes method computes the
        /// checksum when the packet is built.
        /// </summary>
        public ushort Checksum
        {
            get => (ushort)IPAddress.NetworkToHostOrder(_ipChecksum);
            set => _ipChecksum = (ushort)IPAddress.HostToNetworkOrder(value);
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
        /// <param name="ipv4Packet">Byte array containing the binary IPv4 header</param>
        /// <param name="bytesCopied">Number of bytes used in header</param>
        /// <returns>Returns the Ipv4Header object created from the byte array</returns>
        public static Ipv4Header Create(byte[] ipv4Packet, ref int bytesCopied)
        {
            var ipv4Header = new Ipv4Header();

            // Make sure byte array is large enough to contain an IPv4 header
            if (ipv4Packet.Length < Ipv4Header.Ipv4HeaderLength)
                return null;

            // Decode the data in the array back into the class properties
            ipv4Header.Version = (byte)((ipv4Packet[0] >> 4) & 0xF);
            ipv4Header._lengthRaw = (byte)(ipv4Packet[0] & 0xF);
            ipv4Header.TypeOfService = ipv4Packet[1];
            ipv4Header.TotalLengthRaw = BitConverter.ToUInt16(ipv4Packet, 2);
            ipv4Header._ipId = BitConverter.ToUInt16(ipv4Packet, 4);
            ipv4Header.OffsetRaw = BitConverter.ToUInt16(ipv4Packet, 6);
            ipv4Header.Ttl = ipv4Packet[8];
            ipv4Header.Protocol = ipv4Packet[9];
            ipv4Header._ipChecksum = BitConverter.ToUInt16(ipv4Packet, 10);

            ipv4Header.SourceAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 12));
            ipv4Header.DestinationAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 16));

            bytesCopied = ipv4Header.Length;

            return ipv4Header;
        }

                /// <summary>
        /// This routine takes the properties of the IPv4 header and marhalls them into
        /// a byte array representing the IPv4 header that is to be sent on the wire.
        /// </summary>
        /// <param name="payLoad">The encapsulated headers and data</param>
        /// <returns>A byte array of the IPv4 header and payload</returns>
        public override byte[] GetProtocolPacketBytes(byte[] payLoad)
        {
            var index = 0;

            // Allocate space for the IPv4 header plus payload
            var ipv4Packet = new byte[Ipv4HeaderLength + payLoad.Length];
            var memoryStream = new MemoryStream(ipv4Packet);

            memoryStream.WriteByte((byte)((Version << 4) | _lengthRaw));
            memoryStream.WriteByte(TypeOfService);
            memoryStream.Write(BitConverter.GetBytes(TotalLengthRaw));
            memoryStream.Write(BitConverter.GetBytes(IdRaw));
            memoryStream.Write(BitConverter.GetBytes(OffsetRaw));

            memoryStream.WriteByte(Ttl);
            memoryStream.WriteByte(Protocol);
            memoryStream.WriteByte(0); // Zero the checksum for now since we will
            memoryStream.WriteByte(0); // calculate it later

            memoryStream.Write(SourceAddress.GetAddressBytes());
            memoryStream.Write(DestinationAddress.GetAddressBytes());

            memoryStream.WriteByte((byte)((Version << 4) | _lengthRaw));
            
            // Compute the checksum over the entire packet (IPv4 header + payload)
            Checksum = ComputeChecksum(ipv4Packet);

            // Set the checksum into the built packet
            memoryStream.Write(BitConverter.GetBytes(_ipChecksum));

            return ipv4Packet;
        }
    }
}