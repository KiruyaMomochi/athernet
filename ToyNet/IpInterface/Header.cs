using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;


/// <summary>
/// CREDIT: This namespace is modified based from the following source:
/// https://www.winsocketdotnetworkprogramming.com/clientserversocketnetworkcommunication8chap.html
/// </summary>
namespace ToyNet.IpInterface.Header
{
    /// <summary>
    /// The ProtocolHeader class is the base class for all protocol header classes.
    /// It defines one abstract method that each class must implement which returns
    /// a byte array representation of the protocl packet. It also provides common
    /// routines for building the entire protocol packet as well as for computing 
    /// checksums on packets.
    /// 
    /// </summary>
    // abstract class ProtocolHeader
    // {
    //     /// <summary>
    //     /// This abstracted method returns a byte array that is the protocl
    //     /// header and the payload. This is used by teh BuildPacket method
    //     /// to build the entire packet which may consist of multiple headers
    //     /// and data payload.
    //     /// </summary>
    //     /// <param name="payLoad">The byte array of the data encapsulated in this header</param>
    //     /// <returns>A byte array of the serialized header and payload</returns>
    //     abstract public byte[] GetProtocolPacketBytes(
    //             byte[] payLoad
    //             );

    //     /// <summary>
    //     /// This method builds the entire packet to be sent on the socket. It takes
    //     /// an ArrayList of all encapsulated headers as well as the payload. The
    //     /// ArrayList of headers starts with the outermost header towards the
    //     /// innermost. For example when sending an IPv4/UDP packet, the first entry 
    //     /// would be the IPv4 header followed by the UDP header. The byte payload of 
    //     /// the UDP packet is passed as the second parameter.
    //     /// </summary>
    //     /// <param name="headerList">An array list of all headers to build the packet from</param>
    //     /// <param name="payLoad">Data payload appearing after all the headers</param>
    //     /// <returns>Returns a byte array representing the entire packet</returns>
    //     public byte[] BuildPacket(ArrayList headerList, byte[] payLoad)
    //     {
    //         ProtocolHeader protocolHeader;
    //         byte[] newPayload = null;

    //         // Traverse the array in reverse order since the outer headers may need
    //         //    the inner headers and payload to compute checksums on.
    //         for (int i = headerList.Count - 1; i >= 0; i--)
    //         {
    //             protocolHeader = (ProtocolHeader)headerList[i];

    //             newPayload = protocolHeader.GetProtocolPacketBytes(payLoad);

    //             // The payLoad for the next iteration of the loop is now any
    //             //    encapsulated headers plus the original payload data.
    //             payLoad = newPayload;
    //         }

    //         return payLoad;
    //     }

    //     /// <summary>
    //     /// This is a simple method for computing the 16-bit one's complement
    //     /// checksum of a byte buffer. The byte buffer will be padded with
    //     /// a zero byte if an uneven number.
    //     /// </summary>
    //     /// <param name="payLoad">Byte array to compute checksum over</param>
    //     /// <returns></returns>
    //     static public ushort ComputeChecksum(byte[] payLoad)
    //     {
    //         uint xsum = 0;
    //         ushort shortval = 0,
    //                 hiword = 0,
    //                 loword = 0;

    //         // Sum up the 16-bits
    //         for (int i = 0; i < payLoad.Length / 2; i++)
    //         {
    //             hiword = (ushort)(((ushort)payLoad[i * 2]) << 8);
    //             loword = (ushort)payLoad[(i * 2) + 1];

    //             shortval = (ushort)(hiword | loword);

    //             xsum = xsum + (uint)shortval;
    //         }
    //         // Pad if necessary
    //         if ((payLoad.Length % 2) != 0)
    //         {
    //             xsum += (uint)payLoad[payLoad.Length - 1];
    //         }

    //         xsum = ((xsum >> 16) + (xsum & 0xFFFF));
    //         xsum = (xsum + (xsum >> 16));
    //         shortval = (ushort)(~xsum);

    //         return shortval;
    //     }

    //     /// <summary>
    //     /// Utility function for printing a byte array into a series of 4 byte hex digits with
    //     /// four such hex digits displayed per line.
    //     /// </summary>
    //     /// <param name="printBytes">Byte array to display</param>
    //     static public void PrintByteArray(byte[] printBytes)
    //     {
    //         int index = 0;

    //         while (index < printBytes.Length)
    //         {
    //             for (int i = 0; i < 4; i++)
    //             {
    //                 if (index >= printBytes.Length)
    //                     break;

    //                 for (int j = 0; j < 4; j++)
    //                 {
    //                     if (index >= printBytes.Length)
    //                         break;

    //                     Console.Write("{0}", printBytes[index++].ToString("x2"));
    //                 }
    //                 Console.Write(" ");
    //             }
    //             Console.WriteLine("");
    //         }
    //     }
    // }

    /// <summary>
    /// This is the IPv4 protocol header.
    /// </summary>
    class Ipv4Header
    {
        private byte ipVersion;               // actually only 4 bits
        private byte ipLength;                // actually only 4 bits
        private byte ipTypeOfService;
        private ushort ipTotalLength;
        private ushort ipId;
        private ushort ipOffset;
        private byte ipTtl;
        private byte ipProtocol;
        private ushort ipChecksum;
        private IPAddress ipSourceAddress;
        private IPAddress ipDestinationAddress;

        static public int Ipv4HeaderLength = 20;

        /// <summary>
        /// Simple constructor that initializes the members to zero.
        /// </summary>
        public Ipv4Header(): base()
        {
            ipVersion = 4;
            ipLength = (byte)Ipv4HeaderLength;    // Set the property so it will convert properly
            ipTypeOfService = 0;
            ipId = 0;
            ipOffset = 0;
            ipTtl = 1;
            ipProtocol = 0;
            ipChecksum = 0;
            ipSourceAddress = IPAddress.Any;
            ipDestinationAddress = IPAddress.Any;
        }

        /// <summary>
        /// Gets and sets the IP version. This should be 4 to indicate the IPv4 header.
        /// </summary>
        public byte Version
        {
            get
            {
                return ipVersion;
            }
            set
            {
                ipVersion = value;
            }
        }

        /// <summary>
        /// Gets and sets the length of the IPv4 header. This property takes and returns
        /// the number of bytes, but the actual field is the number of 32-bit DWORDs
        /// (the IPv4 header is a multiple of 4-bytes).
        /// </summary>
        public byte Length
        {
            get
            {
                return (byte)(ipLength * 4);
            }
            set
            {
                ipLength = (byte)(value / 4);
            }
        }

        /// <summary>
        /// Gets the RAW length of the IPv4 header.
        /// </summary>
        public byte LengthRaw
        {
            get
            {
                return (byte)ipLength;
            }
        }
        /// <summary>
        /// Gets and sets the type of service field of the IPv4 header. Since it
        /// is a byte, no byte order conversion is required.
        /// </summary>
        public byte TypeOfService
        {
            get
            {
                return ipTypeOfService;
            }
            set
            {
                ipTypeOfService = value;
            }
        }

        /// <summary>
        ///  Gets and sets the total length of the IPv4 header and its encapsulated
        ///  payload. Byte order conversion is required.
        /// </summary>
        public ushort TotalLength
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)ipTotalLength);
            }
            set
            {
                ipTotalLength = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        ///  [NO ORDER CONVERSION] Gets the RAW total length of the IPv4 header and its encapsulated
        ///  payload. Byte order conversion is NOT required.
        /// </summary>
        public ushort TotalLengthRaw
        {
            get
            {
                return ipTotalLength;
            }
        }

        /// <summary>
        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is
        /// requried.
        /// </summary>
        public ushort Id
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)ipId);
            }
            set
            {
                ipId = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// Gets and sets the ID field of the IPv4 header. Byte order conversion is
        /// requried.
        /// </summary>
        public ushort IdRaw
        {
            get
            {
                return (ushort)ipId;
            }
        }       

        /// <summary>
        /// Gets and sets the offset field of the IPv4 header which indicates if
        /// IP fragmentation has occured.
        /// </summary>
        public ushort Offset
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)ipOffset);
            }
            set
            {
                ipOffset = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// [NO ORDER CONVERSION] Gets the offset field of the IPv4 header which indicates if
        /// IP fragmentation has occured.
        /// </summary>
        public ushort OffsetRaw
        {
            get
            {
                return ipOffset;
            }
        }


        /// <summary>
        /// Gets and sets the time-to-live (TTL) value of the IP header. This field
        /// determines how many router hops the packet is valid for.
        /// </summary>
        public byte Ttl
        {
            get
            {
                return ipTtl;
            }
            set
            {
                ipTtl = value;
            }
        }

        /// <summary>
        /// Gets and sets the protocol field of the IPv4 header. This field indicates
        /// what the encapsulated protocol is.
        /// </summary>
        public byte Protocol
        {
            get
            {
                return ipProtocol;
            }
            set
            {
                ipProtocol = value;
            }
        }

        /// <summary>
        /// Gets and sets the checksum field of the IPv4 header. For the IPv4 header, the 
        /// checksum is calculated over the header and payload. Note that this field isn't
        /// meant to be set by the user as the GetProtocolPacketBytes method computes the
        /// checksum when the packet is built.
        /// </summary>
        public ushort Checksum
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)ipChecksum);
            }
            set
            {
                ipChecksum = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// [NO ORDER CONVERSION] Gets the RAW checksum field of the IPv4 header. For the IPv4 header, the 
        /// checksum is calculated over the header and payload. Note that this field isn't
        /// meant to be set by the user as the GetProtocolPacketBytes method computes the
        /// checksum when the packet is built.
        /// </summary>
        public ushort ChecksumRaw
        {
            get
            {
                return (ushort)ipChecksum;
            }
        }

        /// <summary>
        /// Gets and sets the source IP address of the IPv4 packet. This is stored
        /// as an IPAddress object which will be serialized to the appropriate
        /// byte representation in the GetProtocolPacketBytes method.
        /// </summary>
        public IPAddress SourceAddress
        {
            get
            {
                return ipSourceAddress;
            }
            set
            {
                ipSourceAddress = value;
            }
        }

        /// <summary>
        /// Gets and sets the destination IP address of the IPv4 packet. This is stored
        /// as an IPAddress object which will be serialized to the appropriate byte
        /// representation in the GetProtocolPacketBytes method.
        /// </summary>
        public IPAddress DestinationAddress
        {
            get
            {
                return ipDestinationAddress;
            }
            set
            {
                ipDestinationAddress = value;
            }
        }

        /// <summary>
        /// This routine creates an instance of the Ipv4Header class from a byte
        /// array that is a received IGMP packet. This is useful when a packet
        /// is received from the network and the header object needs to be
        /// constructed from those values.
        /// </summary>
        /// <param name="ipv4Packet">Byte array containing the binary IPv4 header</param>
        /// <param name="bytesCopied">Number of bytes used in header</param>
        /// <returns>Returns the Ipv4Header object created from the byte array</returns>
        static public Ipv4Header Create(byte[] ipv4Packet, ref int bytesCopied)
        {
            Ipv4Header ipv4Header = new Ipv4Header();

            // Make sure byte array is large enough to contain an IPv4 header
            if (ipv4Packet.Length < Ipv4Header.Ipv4HeaderLength)
                return null;

            // Decode the data in the array back into the class properties
            ipv4Header.ipVersion = (byte)((ipv4Packet[0] >> 4) & 0xF);
            ipv4Header.ipLength = (byte)(ipv4Packet[0] & 0xF);
            ipv4Header.ipTypeOfService = ipv4Packet[1];
            ipv4Header.ipTotalLength = BitConverter.ToUInt16(ipv4Packet, 2);
            ipv4Header.ipId = BitConverter.ToUInt16(ipv4Packet, 4);
            ipv4Header.ipOffset = BitConverter.ToUInt16(ipv4Packet, 6);
            ipv4Header.ipTtl = ipv4Packet[8];
            ipv4Header.ipProtocol = ipv4Packet[9];
            ipv4Header.ipChecksum = BitConverter.ToUInt16(ipv4Packet, 10);

            ipv4Header.ipSourceAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 12));
            ipv4Header.ipDestinationAddress = new IPAddress(BitConverter.ToUInt32(ipv4Packet, 16));

            bytesCopied = ipv4Header.Length;

            return ipv4Header;
        }

    }

    /// <summary>
    /// This is the IPv6 header definition.
    /// </summary>
    class Ipv6Header
    {
        private byte ipVersion;
        private byte ipTrafficClass;
        private uint ipFlow;
        private ushort ipPayloadLength;
        private byte ipNextHeader;
        private byte ipHopLimit;
        private IPAddress ipSourceAddress;
        private IPAddress ipDestinationAddress;

        static public int Ipv6HeaderLength = 40;

        /// <summary>
        /// Simple constructor for the IPv6 header that initializes the fields to zero.
        /// </summary>
        public Ipv6Header()
            : base()
        {
            ipVersion = 6;
            ipTrafficClass = 0;
            ipFlow = 0;
            ipPayloadLength = 0;
            ipNextHeader = 0;
            ipHopLimit = 32;
            ipSourceAddress = IPAddress.IPv6Any;
            ipDestinationAddress = IPAddress.IPv6Any;
        }

        /// <summary>
        /// Gets and sets the IP version. This value should be 6.
        /// </summary>
        public byte Version
        {
            get
            {
                return ipVersion;
            }
            set
            {
                ipVersion = value;
            }
        }

        /// <summary>
        /// Gets and sets the traffic class for the header. 
        /// </summary>
        public byte TrafficClass
        {
            get
            {
                return ipTrafficClass;
            }
            set
            {
                ipTrafficClass = value;
            }
        }

        /// <summary>
        /// Gets and sets the flow value for the packet. Byte order conversion
        /// is required for this field.
        /// </summary>
        public uint Flow
        {
            get
            {
                return (uint)IPAddress.NetworkToHostOrder((int)ipFlow);
            }
            set
            {
                ipFlow = (uint)IPAddress.HostToNetworkOrder((int)value);
            }
        }

        /// <summary>
        /// Gets and sets the payload length for the IPv6 packet. Note for IPv6, the
        /// payload length counts only the payload and not the IPv6 header (since
        /// the IPv6 header is a fixed length). Byte order conversion is required.
        /// </summary>
        public ushort PayloadLength
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)ipPayloadLength);
            }
            set
            {
                ipPayloadLength = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// The protocol value of the header encapsulated by the IPv6 header.
        /// </summary>
        public byte NextHeader
        {
            get
            {
                return ipNextHeader;
            }
            set
            {
                ipNextHeader = value;
            }
        }

        /// <summary>
        /// Time-to-live (TTL) of the IPv6 header.
        /// </summary>
        public byte HopLimit
        {
            get
            {
                return ipHopLimit;
            }
            set
            {
                ipHopLimit = value;
            }
        }

        /// <summary>
        /// IPv6 source address in the IPv6 header.
        /// </summary>
        public IPAddress SourceAddress
        {
            get
            {
                return ipSourceAddress;
            }
            set
            {
                ipSourceAddress = value;
            }
        }

        /// <summary>
        /// IPv6 destination address in the IPv6 header.
        /// </summary>
        public IPAddress DestinationAddress
        {
            get
            {
                return ipDestinationAddress;
            }
            set
            {
                ipDestinationAddress = value;
            }
        }

        /// <summary>
        /// This routine creates an instance of the Ipv6Header class from a byte
        /// array that is a received IGMP packet. This is useful when a packet
        /// is received from the network and the header object needs to be
        /// constructed from those values. 
        /// </summary>
        /// <param name="ipv6Packet">Byte array containing the binary IPv6 header</param>
        /// <param name="bytesCopied">Number of bytes used in header</param>
        /// <returns>Returns the Ipv6Header object created from the byte array</returns>
        static public Ipv6Header Create(byte[] ipv6Packet, ref int bytesCopied)
        {
            Ipv6Header ipv6Header = new Ipv6Header();
            byte[] addressBytes = new byte[16];
            uint tempVal = 0,
                        tempVal2 = 0;

            // Ensure byte array is large enough to contain an IPv6 header
            if (ipv6Packet.Length < Ipv6Header.Ipv6HeaderLength)
                return null;

            tempVal = ipv6Packet[0];
            tempVal = (tempVal >> 4) & 0xF;
            ipv6Header.ipVersion = (byte)tempVal;

            tempVal = ipv6Packet[0];
            tempVal = (tempVal & 0xF) >> 4;
            ipv6Header.ipTrafficClass = (byte)(tempVal | (uint)((ipv6Packet[1] >> 4) & 0xF));

            tempVal2 = ipv6Packet[1];
            tempVal2 = (tempVal2 & 0xF) << 16;
            tempVal = ipv6Packet[2];
            tempVal = tempVal << 8;
            ipv6Header.ipFlow = tempVal2 | tempVal | ipv6Packet[3];

            ipv6Header.ipNextHeader = ipv6Packet[4];
            ipv6Header.ipHopLimit = ipv6Packet[5];

            Array.Copy(ipv6Packet, 6, addressBytes, 0, 16);
            ipv6Header.SourceAddress = new IPAddress(addressBytes);

            Array.Copy(ipv6Packet, 24, addressBytes, 0, 16);
            ipv6Header.DestinationAddress = new IPAddress(addressBytes);

            bytesCopied = Ipv6Header.Ipv6HeaderLength;

            return ipv6Header;
        }

        /// <summary>
        /// Packages up the IPv6 header and the given payload into a byte array
        /// suitable for sending on a socket.
        /// </summary>
        /// <param name="payLoad">Data encapsulated by the IPv6 header</param>
        /// <returns>Byte array of the IPv6 packet and payload</returns>
        // public override byte[] GetProtocolPacketBytes(byte[] payLoad)
        // {
        //     byte[] byteValue,
        //             ipv6Packet;
        //     int offset = 0;

        //     ipv6Packet = new byte[Ipv6HeaderLength + payLoad.Length];

        //     ipv6Packet[offset++] = (byte)((ipVersion << 4) | ((ipTrafficClass >> 4) & 0xF));

        //     //tmpbyte1 = (byte) ( ( ipTrafficClass << 4) & 0xF0);
        //     //tmpbyte2 = (byte) ( ( ipFlow >> 16 ) & 0xF );

        //     ipv6Packet[offset++] = (byte)((uint)((ipTrafficClass << 4) & 0xF0) | (uint)((Flow >> 16) & 0xF));
        //     ipv6Packet[offset++] = (byte)((Flow >> 8) & 0xFF);
        //     ipv6Packet[offset++] = (byte)(Flow & 0xFF);

        //     Console.WriteLine("Next header = {0}", ipNextHeader);

        //     byteValue = BitConverter.GetBytes(ipPayloadLength);
        //     Array.Copy(byteValue, 0, ipv6Packet, offset, byteValue.Length);
        //     offset += byteValue.Length;

        //     ipv6Packet[offset++] = (byte)ipNextHeader;
        //     ipv6Packet[offset++] = (byte)ipHopLimit;

        //     byteValue = ipSourceAddress.GetAddressBytes();
        //     Array.Copy(byteValue, 0, ipv6Packet, offset, byteValue.Length);
        //     offset += byteValue.Length;

        //     byteValue = ipDestinationAddress.GetAddressBytes();
        //     Array.Copy(byteValue, 0, ipv6Packet, offset, byteValue.Length);
        //     offset += byteValue.Length;

        //     Array.Copy(payLoad, 0, ipv6Packet, offset, payLoad.Length);

        //     return ipv6Packet;
        // }
    }

    /// <summary>
    /// The ICMP protocol header used with the IPv4 protocol.
    /// </summary>
    class IcmpHeader
    {
        private byte icmpType;                   // ICMP message type
        private byte icmpCode;                   // ICMP message code
        private ushort icmpChecksum;               // Checksum of ICMP header and payload
        private ushort icmpId;                     // Message ID
        private ushort icmpSequence;               // ICMP sequence number

        static public byte EchoRequestType = 8;     // ICMP echo request
        static public byte EchoRequestCode = 0;     // ICMP echo request code
        static public byte EchoReplyType = 0;     // ICMP echo reply
        static public byte EchoReplyCode = 0;     // ICMP echo reply code

        static public int IcmpHeaderLength = 8;    // Length of ICMP header

        /// <summary>
        /// Default constructor for ICMP packet
        /// </summary>
        public IcmpHeader()
            : base()
        {
            icmpType = 0;
            icmpCode = 0;
            icmpChecksum = 0;
            icmpId = 0;
            icmpSequence = 0;
        }

        /// <summary>
        /// ICMP message type.
        /// </summary>
        public byte Type
        {
            get
            {
                return icmpType;
            }
            set
            {
                icmpType = value;
            }
        }

        /// <summary>
        /// ICMP message code.
        /// </summary>
        public byte Code
        {
            get
            {
                return icmpCode;
            }
            set
            {
                icmpCode = value;
            }
        }

        /// <summary>
        /// Checksum of ICMP packet and payload.  Performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)icmpChecksum);
            }
            set
            {
                icmpChecksum = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// ICMP message ID. Used to uniquely identify the source of the ICMP packet.
        /// Performs the necessary byte order conversion.
        /// </summary>
        public ushort Id
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)icmpId);
            }
            set
            {
                icmpId = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// ICMP sequence number. As each ICMP message is sent the sequence should be incremented.
        /// Performs the necessary byte order conversion.
        /// </summary>
        public ushort Sequence
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)icmpSequence);
            }
            set
            {
                icmpSequence = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// This routine creates an instance of the IcmpHeader class from a byte
        /// array that is a received IGMP packet. This is useful when a packet
        /// is received from the network and the header object needs to be
        /// constructed from those values. 
        /// </summary>
        /// <param name="icmpPacket">Byte array containing the binary ICMP header</param>
        /// <param name="bytesCopied">Number of bytes used in header</param>
        /// <returns>Returns the IcmpHeader object created from the byte array</returns>
        static public IcmpHeader Create(byte[] icmpPacket, ref int bytesCopied)
        {
            IcmpHeader icmpHeader = new IcmpHeader();
            int offset = 0;

            // Make sure byte array is large enough to contain an ICMP header
            if (icmpPacket.Length < IcmpHeader.IcmpHeaderLength)
                return null;

            icmpHeader.icmpType = icmpPacket[offset++];
            icmpHeader.icmpCode = icmpPacket[offset++];
            icmpHeader.icmpChecksum = BitConverter.ToUInt16(icmpPacket, offset);
            offset += 2;
            icmpHeader.icmpId = BitConverter.ToUInt16(icmpPacket, offset);
            offset += 2;
            icmpHeader.icmpSequence = BitConverter.ToUInt16(icmpPacket, offset);

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
            byte[] icmpPacket,
                    byteValue;
            int offset = 0;

            icmpPacket = new byte[IcmpHeaderLength + payLoad.Length];

            icmpPacket[offset++] = icmpType;
            icmpPacket[offset++] = icmpCode;
            icmpPacket[offset++] = 0;          // Zero out the checksum until the packet is assembled
            icmpPacket[offset++] = 0;

            byteValue = BitConverter.GetBytes(icmpId);
            Array.Copy(byteValue, 0, icmpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(icmpSequence);
            Array.Copy(byteValue, 0, icmpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            if (payLoad.Length > 0)
            {
                Array.Copy(payLoad, 0, icmpPacket, offset, payLoad.Length);
                offset += payLoad.Length;
            }

            // Compute the checksum over the entire packet
            Checksum = ComputeChecksum(icmpPacket);

            // Put the checksum back into the packet
            byteValue = BitConverter.GetBytes(icmpChecksum);
            Array.Copy(byteValue, 0, icmpPacket, 2, byteValue.Length);

            return icmpPacket;
        }
    }

    /// <summary>
    /// Class that represents the UDP protocol.
    /// </summary>
    class UdpHeader
    {
        private ushort srcPort;
        private ushort destPort;
        private ushort udpLength;
        private ushort udpChecksum;

        public Ipv6Header ipv6PacketHeader;

        public Ipv4Header ipv4PacketHeader;

        static public int UdpHeaderLength = 8;

        /// <summary>
        /// Simple constructor for the UDP header.
        /// </summary>
        public UdpHeader()
            : base()
        {
            srcPort = 0;
            destPort = 0;
            udpLength = 0;
            udpChecksum = 0;

            ipv6PacketHeader = null;
            ipv4PacketHeader = null;
        }

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort SourcePort
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)srcPort);
            }
            set
            {
                srcPort = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// Gets and sets the destination port. Performs the necessary byte order conversion.
        /// </summary>
        public ushort DestinationPort
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)destPort);
            }
            set
            {
                destPort = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// Gets and sets the UDP payload length. This is the length of the payload
        /// plus the size of the UDP header itself.
        /// </summary>
        public ushort Length
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)udpLength);
            }
            set
            {
                udpLength = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// Gets and sets the checksum value. It performs the necessary byte order conversion.
        /// </summary>
        public ushort Checksum
        {
            get
            {
                return (ushort)IPAddress.NetworkToHostOrder((short)udpChecksum);
            }
            set
            {
                udpChecksum = (ushort)IPAddress.HostToNetworkOrder((short)value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="udpData"></param>
        /// <param name="bytesCopied"></param>
        /// <returns></returns>
        static public UdpHeader Create(byte[] udpData, ref int bytesCopied)
        {
            UdpHeader udpPacketHeader = new UdpHeader();

            udpPacketHeader.srcPort = BitConverter.ToUInt16(udpData, 0);
            udpPacketHeader.destPort = BitConverter.ToUInt16(udpData, 2);
            udpPacketHeader.udpLength = BitConverter.ToUInt16(udpData, 4);
            udpPacketHeader.udpChecksum = BitConverter.ToUInt16(udpData, 6);

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
            byte[] udpPacket = new byte[UdpHeaderLength + payLoad.Length],
                    pseudoHeader = null,
                    byteValue = null;
            int offset = 0;

            // Build the UDP packet first
            byteValue = BitConverter.GetBytes(srcPort);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(destPort);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(udpLength);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            udpPacket[offset++] = 0;      // Checksum is initially zero
            udpPacket[offset++] = 0;

            // Copy payload to end of packet
            Array.Copy(payLoad, 0, udpPacket, offset, payLoad.Length);

            if (ipv4PacketHeader != null)
            {
                pseudoHeader = new byte[UdpHeaderLength + 12 + payLoad.Length];

                // Build the IPv4 pseudo header
                offset = 0;

                // Source address
                byteValue = ipv4PacketHeader.SourceAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Destination address
                byteValue = ipv4PacketHeader.DestinationAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // 1 byte zero pad plus next header protocol value
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = ipv4PacketHeader.Protocol;

                // Packet length
                byteValue = BitConverter.GetBytes(udpLength);
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Copy the UDP packet to the end of this
                Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);

            }
            else if (ipv6PacketHeader != null)
            {
                uint ipv6PayloadLength;

                pseudoHeader = new byte[UdpHeaderLength + 40 + payLoad.Length];

                // Build the IPv6 pseudo header
                offset = 0;

                // Source address
                byteValue = ipv6PacketHeader.SourceAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Destination address
                byteValue = ipv6PacketHeader.DestinationAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                ipv6PayloadLength = (uint)IPAddress.HostToNetworkOrder((int)(payLoad.Length + UdpHeaderLength));

                // Packet payload: ICMPv6 headers plus payload
                byteValue = BitConverter.GetBytes(ipv6PayloadLength);
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // 3 bytes zero pad plus next header protocol value
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = ipv6PacketHeader.NextHeader;

                // Copy the UDP packet to the end of this
                Array.Copy(udpPacket, 0, pseudoHeader, offset, udpPacket.Length);
            }

            if (pseudoHeader != null)
            {
                Checksum = ComputeChecksum(pseudoHeader);
            }

            // Put checksum back into packet
            byteValue = BitConverter.GetBytes(udpChecksum);
            Array.Copy(byteValue, 0, udpPacket, 6, byteValue.Length);

            return udpPacket;
        }
    }

}