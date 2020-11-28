using System;
using System.Net;
using System.Net.Sockets;
using ToyNet.IpInterface.Header;

namespace ToyNet.IpInterface.Packet
{
    class Ipv4Packet : ProtocolPacket
    {
        private Ipv4Header _header;
        private byte[] _payload;

        public Ipv4Packet()
        {
            _header = new Ipv4Header();
            _payload = new byte[128]; // Default to be zero
        }

        public Ipv4Header Header
        {
            get => _header;
            set => _header = value;
        }

        public byte[] Payload
        {
            get => _payload;
            set => Buffer.BlockCopy(value, 0, _payload, 0, value.Length); // DeepCopy
        }

        public void SetHeader(IPAddress ipSourceAddress, IPAddress ipDestAddress, int messageSize)
        {
            _header.Version = 4;
            _header.Protocol = (byte)ProtocolType.Udp;
            _header.Ttl = 2;
            _header.Offset = 0;
            _header.Length = (byte)Ipv4Header.Ipv4HeaderLength;
            _header.TotalLength = System.Convert.ToUInt16(
                Ipv4Header.Ipv4HeaderLength + UdpHeader.UdpHeaderLength + messageSize);
            _header.SourceAddress = ipSourceAddress;
            _header.DestinationAddress = ipDestAddress;
        }
    }
}