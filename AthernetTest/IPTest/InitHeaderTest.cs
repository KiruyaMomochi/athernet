using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Athernet.IPLayer.Header;
using Athernet.IPLayer.Packet;
using Xunit;
using Xunit.Abstractions;

namespace AthernetTest.IPTest
{
    public class InitHeaderTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IcmpHeader _icmpHeader;
        private readonly UdpHeader _udpHeader;

        public InitHeaderTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _icmpHeader = new IcmpHeader()
            {
                Type = IcmpType.EchoRequest,
                Code = 0,
                Checksum = 0x4cab,
                Id = 1,
                Sequence = 176
            };
            _udpHeader = new UdpHeader()
            {
                SourcePort = 42328,
                DestinationPort = 2333,
                Checksum = 0xa3b7
            };
        }

        [Fact]
        public void TestIcmpHeader()
        {
            var real =
                "08004cab000100b06162636465666768696a6b6c6d6e6f7071727374757677616263646566676869";
            var realPacket = new List<byte>(real.Length / 2);
            for (int i = 0; i < real.Length; i += 2)
            {
                realPacket.Add(Convert.ToByte(real.Substring(i, 2), 16));
            }

            var testPacket = _icmpHeader.GetProtocolPacketBytes(realPacket.Skip(8).ToArray());
            _testOutputHelper.WriteLine(BitConverter.ToString(testPacket).Replace("-", ""));
            Assert.True(testPacket.SequenceEqual(realPacket));
        }

        [Fact]
        public void TestIpv4ICmpHeader()
        {
            var real =
                "4500003cc5f30000800112680a14dfb178fdffa208004cab000100b06162636465666768696a6b6c6d6e6f7071727374757677616263646566676869";
            var realPacket = new List<byte>(real.Length / 2);
            for (int i = 0; i < real.Length; i += 2)
            {
                realPacket.Add(Convert.ToByte(real.Substring(i, 2), 16));
            }

            var ipv4Header = new Ipv4Header()
            {
                Version = 4,
                Length = 20,
                TypeOfService = 0,
                Id = 0xc5f3,
                Offset = 0,
                Ttl = 128,
                Protocol = ProtocolType.Icmp,
                Checksum = 0x1268,
                SourceAddress = IPAddress.Parse("10.20.223.177"),
                DestinationAddress = IPAddress.Parse("120.253.255.162")
            };
            var testPacket = ipv4Header.GetProtocolPacketBytes(_icmpHeader, realPacket.Skip(28).ToArray());
            _testOutputHelper.WriteLine(BitConverter.ToString(testPacket).Replace("-", ""));
            Assert.True(testPacket.SequenceEqual(realPacket));
        }

        [Fact]
        public void TestIpv4UdpHeader()
        {
            var real =
                "45000029562040003f1193fc0a135dcf0a14dfb1a558091d0015a3b748656c6c6f20576f726c64210a"; 
            //  "45000029562040003F1193FC0A135DCF0A14DFB1A558091D0015ADB748656C6C6F20576F726C64210A"
            var realPacket = new List<byte>(real.Length / 2);
            for (int i = 0; i < real.Length; i += 2)
            {
                realPacket.Add(Convert.ToByte(real.Substring(i, 2), 16));
            }
            
            var ipv4Header = new Ipv4Header()
            {
                Version = 4,
                Length = 20,
                TypeOfService = 0,
                Id = 0x5620,
                Flags = 0x40,
                Offset = 0,
                Ttl = 63,
                Protocol = ProtocolType.Udp,
                Checksum = 0x93fc,
                SourceAddress = IPAddress.Parse("10.19.93.207"),
                DestinationAddress = IPAddress.Parse("10.20.223.177")
            };
            var testPacket = ipv4Header.GetProtocolPacketBytes(_udpHeader, realPacket.Skip(28).ToArray());
            _testOutputHelper.WriteLine(BitConverter.ToString(testPacket).Replace("-", ""));
            Assert.True(testPacket.SequenceEqual(realPacket));
        }
    }
}