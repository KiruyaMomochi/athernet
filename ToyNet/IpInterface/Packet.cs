using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using ToyNet.IpInterface.Header;

namespace ToyNet.IpInterface.Packet
{
    abstract class ProtocolPacket
    {
        /// <summary>
        /// This abstracted method returns a byte array that is the protocl
        /// header and the payload. This is used by teh BuildPacket method
        /// to build the entire packet which may consist of multiple headers
        /// and data payload.
        /// </summary>
        /// <param name="payLoad">The byte array of the data encapsulated in this header</param>
        /// <returns>A byte array of the serialized header and payload</returns>
        abstract public byte[] GetProtocolPacketBytes(
                byte[] payLoad
                );

        /// <summary>
        /// This method builds the entire packet to be sent on the socket. It takes
        /// an ArrayList of all encapsulated headers as well as the payload. The
        /// ArrayList of headers starts with the outermost header towards the
        /// innermost. For example when sending an IPv4/UDP packet, the first entry 
        /// would be the IPv4 header followed by the UDP header. The byte payload of 
        /// the UDP packet is passed as the second parameter.
        /// </summary>
        /// <param name="headerList">An array list of all headers to build the packet from</param>
        /// <param name="payLoad">Data payload appearing after all the headers</param>
        /// <returns>Returns a byte array representing the entire packet</returns>
        public byte[] BuildPacket(ArrayList headerList, byte[] payLoad)
        {
            ProtocolHeader protocolHeader;
            byte[] newPayload = null;

            // Traverse the array in reverse order since the outer headers may need
            //    the inner headers and payload to compute checksums on.
            for (int i = headerList.Count - 1; i >= 0; i--)
            {
                protocolHeader = (ProtocolHeader)headerList[i];

                newPayload = protocolHeader.GetProtocolPacketBytes(payLoad);

                // The payLoad for the next iteration of the loop is now any
                //    encapsulated headers plus the original payload data.
                payLoad = newPayload;
            }

            return payLoad;
        }
        
        /// <summary>
        /// This is a simple method for computing the 16-bit one's complement
        /// checksum of a byte buffer. The byte buffer will be padded with
        /// a zero byte if an uneven number.
        /// </summary>
        /// <param name="payLoad">Byte array to compute checksum over</param>
        /// <returns></returns>
        static public ushort ComputeChecksum(byte[] payLoad)
        {
            uint xsum = 0;
            ushort shortval = 0,
                    hiword = 0,
                    loword = 0;

            // Sum up the 16-bits
            for (int i = 0; i < payLoad.Length / 2; i++)
            {
                hiword = (ushort)(((ushort)payLoad[i * 2]) << 8);
                loword = (ushort)payLoad[(i * 2) + 1];

                shortval = (ushort)(hiword | loword);

                xsum = xsum + (uint)shortval;
            }
            // Pad if necessary
            if ((payLoad.Length % 2) != 0)
            {
                xsum += (uint)payLoad[payLoad.Length - 1];
            }

            xsum = ((xsum >> 16) + (xsum & 0xFFFF));
            xsum = (xsum + (xsum >> 16));
            shortval = (ushort)(~xsum);

            return shortval;
        }

        /// <summary>
        /// Utility function for printing a byte array into a series of 4 byte hex digits with
        /// four such hex digits displayed per line.
        /// </summary>
        /// <param name="printBytes">Byte array to display</param>
        static public void PrintByteArray(byte[] printBytes)
        {
            int index = 0;

            while (index < printBytes.Length)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (index >= printBytes.Length)
                        break;

                    for (int j = 0; j < 4; j++)
                    {
                        if (index >= printBytes.Length)
                            break;

                        Console.Write("{0}", printBytes[index++].ToString("x2"));
                    }
                    Console.Write(" ");
                }
                Console.WriteLine("");
            }
        }

    }

    class Ipv4Packet : ProtocolPacket
    {
        private Ipv4Header header;
        private byte[] payload;

        public Ipv4Packet()
        {
            header = new Ipv4Header();
            payload = new byte[128]; // Default to be zero
        }

        public Ipv4Header Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
            }
        }

        public byte[] Payload
        {
            get
            {
                return payload;
            }
            set
            {
                Buffer.BlockCopy(value, 0, payload, 0, value.Length); // DeepCopy
            }
        }

        public void SetHeader(IPAddress ipSourceAddress, IPAddress ipDestAddress, int messageSize)
        {
            header.Version = 4;
            header.Protocol = (byte)ProtocolType.Udp;
            header.Ttl = 2;
            header.Offset = 0;
            header.Length = (byte)Ipv4Header.Ipv4HeaderLength;
            header.TotalLength = (ushort)System.Convert.ToUInt16(
                Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength + messageSize);
            header.SourceAddress = ipSourceAddress;
            header.DestinationAddress = ipDestAddress;
        }

        /// <summary>
        /// This routine takes the properties of the IPv4 header and marhalls them into
        /// a byte array representing the IPv4 header that is to be sent on the wire.
        /// </summary>
        /// <param name="payLoad">The encapsulated headers and data</param>
        /// <returns>A byte array of the IPv4 header and payload</returns>
        public override byte[] GetProtocolPacketBytes(byte[] payLoad)
        {
            byte[] ipv4Packet,
                    byteValue;
            int index = 0;

            // Allocate space for the IPv4 header plus payload
            ipv4Packet = new byte[Ipv4Header.Ipv4HeaderLength + payLoad.Length];

            ipv4Packet[index++] = (byte)((header.Version << 4) | header.LengthRaw);
            ipv4Packet[index++] = header.TypeOfService;

            byteValue = BitConverter.GetBytes(header.TotalLengthRaw);
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            byteValue = BitConverter.GetBytes(header.IdRaw);
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            byteValue = BitConverter.GetBytes(header.OffsetRaw);
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            ipv4Packet[index++] = header.Ttl;
            ipv4Packet[index++] = header.Protocol;
            ipv4Packet[index++] = 0; // Zero the checksum for now since we will
            ipv4Packet[index++] = 0; // calculate it later

            // Copy the source address
            byteValue = header.SourceAddress.GetAddressBytes();
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            // Copy the destination address
            byteValue = header.DestinationAddress.GetAddressBytes();
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            // Copy the payload
            Array.Copy(payLoad, 0, ipv4Packet, index, payLoad.Length);
            index += payLoad.Length;

            // Compute the checksum over the entire packet (IPv4 header + payload)
            header.Checksum = ComputeChecksum(ipv4Packet);

            // Set the checksum into the built packet
            byteValue = BitConverter.GetBytes(header.ChecksumRaw);
            Array.Copy(byteValue, 0, ipv4Packet, 10, byteValue.Length);

            return ipv4Packet;
        }

    }

    class IcmpPacket : ProtocolPacket
    {
        private IcmpHeader header;
        private byte[] payload;
        public IcmpPacket()
        {
            header = new IcmpHeader();
            payload = new byte[128]; // Default to be zero
        }
        public IcmpHeader Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
            }
        }

        public byte[] Payload
        {
            get
            {
                return payload;
            }
            set
            {
                Buffer.BlockCopy(value, 0, payload, 0, value.Length); // DeepCopy
            }
        }
        public void SetHeader(ushort identifier, ushort seqenceNumber)
        {
            header.Type = (byte)8;
            header.Code = 0;
            // ↓ like in UDP, just initialized to zero, will get meaningful when ipv4 header is prepared.
            header.Checksum = 0; 
            header.Id = identifier;
            header.Sequence = seqenceNumber;

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

            icmpPacket = new byte[IcmpHeader.IcmpHeaderLength + payLoad.Length];

            icmpPacket[offset++] = header.Type;
            icmpPacket[offset++] = header.Code;
            icmpPacket[offset++] = 0;          // Zero out the checksum until the packet is assembled
            icmpPacket[offset++] = 0;

            byteValue = BitConverter.GetBytes(header.IdRaw);
            Array.Copy(byteValue, 0, icmpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(header.SequenceRaw);
            Array.Copy(byteValue, 0, icmpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            if (payLoad.Length > 0)
            {
                Array.Copy(payLoad, 0, icmpPacket, offset, payLoad.Length);
                offset += payLoad.Length;
            }

            // Compute the checksum over the entire packet
            header.Checksum = ComputeChecksum(icmpPacket);

            // Put the checksum back into the packet
            byteValue = BitConverter.GetBytes(header.ChecksumRaw);
            Array.Copy(byteValue, 0, icmpPacket, 2, byteValue.Length);

            return icmpPacket;
        }


    }

    class UdpPacket : ProtocolPacket
    {
        private UdpHeader header;
        private byte[] payload;
        public UdpPacket()
        {
            header = new UdpHeader();
            payload = new byte[128]; // Default to be zero
        }
        public UdpHeader Header
        {
            get
            {
                return header;
            }
            set
            {
                header = value;
            }
        }

        public byte[] Payload
        {
            get
            {
                return payload;
            }
            set
            {
                Buffer.BlockCopy(value, 0, payload, 0, value.Length); // DeepCopy
            }
        }
        public void SetHeader(ushort sourcePort, ushort destinationPort, int messageSize, Ipv4Header ipv4Header)
        {
            header.SourcePort = sourcePort;
            header.DestinationPort = destinationPort;
            header.Length = (ushort) (UdpHeader.UdpHeaderLength + messageSize);
            // ↓ just initialized to zero, will get meaningful when ipv4 header is prepared.
            header.Checksum = 0;
            // ↓ Set the ipv4 header in the UDP header since it is required to calculate pseudo-header checksum
            header.ipv4PacketHeader = ipv4Header;
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
            int offset = 0;

            // Build the UDP packet first
            byteValue = BitConverter.GetBytes(header.SourcePortRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(header.DestinationPortRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            byteValue = BitConverter.GetBytes(header.LengthRaw);
            Array.Copy(byteValue, 0, udpPacket, offset, byteValue.Length);
            offset += byteValue.Length;

            udpPacket[offset++] = 0;      // Checksum is initially zero
            udpPacket[offset++] = 0;

            // Copy payload to end of packet
            Array.Copy(payLoad, 0, udpPacket, offset, payLoad.Length);

            if (header.ipv4PacketHeader != null)
            {
                pseudoHeader = new byte[UdpHeader.UdpHeaderLength + 12 + payLoad.Length];

                // Build the IPv4 pseudo header
                offset = 0;

                // Source address
                byteValue = header.ipv4PacketHeader.SourceAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // Destination address
                byteValue = header.ipv4PacketHeader.DestinationAddress.GetAddressBytes();
                Array.Copy(byteValue, 0, pseudoHeader, offset, byteValue.Length);
                offset += byteValue.Length;

                // 1 byte zero pad plus next header protocol value
                pseudoHeader[offset++] = 0;
                pseudoHeader[offset++] = header.ipv4PacketHeader.Protocol;

                // Packet length
                byteValue = BitConverter.GetBytes(header.LengthRaw);
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
                header.Checksum = ComputeChecksum(pseudoHeader);
            }

            // Put checksum back into packet
            byteValue = BitConverter.GetBytes(header.ChecksumRaw);
            Array.Copy(byteValue, 0, udpPacket, 6, byteValue.Length);

            return udpPacket;
        }

    }

}