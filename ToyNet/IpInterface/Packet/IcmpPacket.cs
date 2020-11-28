using System;
using System.IO;
using ToyNet.IpInterface.Header;

namespace ToyNet.IpInterface.Packet
{
    internal class IcmpPacket : ProtocolPacket
    {
        private IcmpHeader _header = new();
        private readonly byte[] _payload = new byte[128];

        public IcmpHeader Header
        {
            get => _header;
            set => _header = value;
        }

        public byte[] Payload
        {
            get => _payload;
            set => Buffer.BlockCopy(value, 0, _payload, 0, value.Length); // DeepCopy
        }
        
        public void SetHeader(ICMPType type, ushort identifier, ushort sequenceNumber)
        {
            _header.Type = type;
            _header.Code = 0;
            // ↓ like in UDP, just initialized to zero, will get meaningful when ipv4 header is prepared.
            _header.Checksum = 0; 
            _header.Id = identifier;
            _header.Sequence = sequenceNumber;
        }

        /// <summary>
        /// This routine builds the ICMP packet suitable for sending on a raw socket.
        /// It builds the ICMP packet and payload into a byte array and computes
        /// the checksum.
        /// </summary>
        /// <param name="payLoad">Data payload of the ICMP packet</param>
        /// <returns>Byte array representing the ICMP packet and payload</returns>
        public override byte[] GetProtocolPacketBytes(byte[] payLoad) => _header.GetProtocolPacketBytes(payLoad)
    }
}