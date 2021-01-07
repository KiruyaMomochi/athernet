using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Athernet.IPLayer.Packet;
using Xunit;

namespace AthernetTest.IPTest
{
    public class ParseHeaderTest
    {
        [Fact]
        public void ParseIcmpHeader()
        {
            var real =
                "08004cab000100b06162636465666768696a6b6c6d6e6f7071727374757677616263646566676869";
            var realPacket = new List<byte>(real.Length / 2);
            for (int i = 0; i < real.Length; i += 2)
            {
                realPacket.Add(Convert.ToByte(real.Substring(i, 2), 16));
            }

            var packet = IcmpPacket.Parse(realPacket.ToArray());
            Assert.Equal(IcmpType.EchoRequest, packet.Header.Type);
            Assert.Equal(0, packet.Header.Code);
            Assert.Equal(0x4cab, packet.Header.Checksum);
            Assert.Equal(1, packet.Header.Id);
            Assert.Equal(176, packet.Header.Sequence);
        }

        [Fact]
        public void ParseIpv4UdpHeader()
        {
            var real =
                "45000029562040003f1193fc0a135dcf0a14dfb1a558091d0015a3b748656c6c6f20576f726c64210a"; var realPacket = new List<byte>(real.Length / 2);
            for (int i = 0; i < real.Length; i += 2)
            {
                realPacket.Add(Convert.ToByte(real.Substring(i, 2), 16));
            }
            var packet = Ipv4Packet.Parse(realPacket.ToArray());
            
            // TODO
            Assert.Equal(4, packet.Header.Version);
            Assert.Equal(ProtocolType.Udp, packet.Header.Protocol);
        }
        
        [Fact]
        public void ParseIpv4IcmpHeader()
        {
            var real =
                "4500003cc5f30000800112680a14dfb178fdffa208004cab000100b06162636465666768696a6b6c6d6e6f7071727374757677616263646566676869";
            var realPacket = new List<byte>(real.Length / 2);
            for (int i = 0; i < real.Length; i += 2)
            {
                realPacket.Add(Convert.ToByte(real.Substring(i, 2), 16));
            }
            var packet = Ipv4Packet.Parse(realPacket.ToArray());
            
            // TODO
            Assert.Equal(4, packet.Header.Version);
            Assert.Equal(ProtocolType.Icmp, packet.Header.Protocol);
        }
    }
}