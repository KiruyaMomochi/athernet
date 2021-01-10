using System;
using Athernet.IPLayer.Header;

namespace Athernet.IPLayer.Packet
{
    public class IcmpPacket : ProtocolPacket
    {
        public IcmpHeader Header { get; set; }
        public byte[] Payload { get; set; }

        public static IcmpPacket Parse(byte[] packet)
        {
            var header = packet[..IcmpHeader.IcmpHeaderLength];
            return new IcmpPacket
            {
                Header = IcmpHeader.Create(header),
                Payload = packet[IcmpHeader.IcmpHeaderLength..]
            };
        }
    }
}