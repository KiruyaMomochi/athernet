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

            ipv4Packet[index++] = (byte)((ipVersion << 4) | ipLength);
            ipv4Packet[index++] = ipTypeOfService;

            byteValue = BitConverter.GetBytes(ipTotalLength);
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            byteValue = BitConverter.GetBytes(ipId);
            Array.Copy(byteValue, 0, ipv4Packet, index, byteValue.Length);
            index += byteValue.Length;

            byteValue = BitConverter.GetBytes(ipOffset);
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
            Checksum = ComputeChecksum(ipv4Packet);

            // Set the checksum into the built packet
            byteValue = BitConverter.GetBytes(ipChecksum);
            Array.Copy(byteValue, 0, ipv4Packet, 10, byteValue.Length);

            return ipv4Packet;
        }

    }

    class UdpPacket
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
        public void SetHeader(ushort sourcePort, ushort destinationPort, int messageSize)
        {
            header.SourcePort = sourcePort;
            header.DestinationPort = destinationPort;
            header.Length = (ushort) (UdpHeader.UdpHeaderLength + messageSize);
            // ↓ just initialized to zero, will get meaningful when ipv4 header is prepared.
            header.Checksum = 0;
            // ↓ Set the ipv4 header in the UDP header since it is required to calculate pseudo-header checksum
            // header.ipv4PacketHeader = ? 
        }

    }
}