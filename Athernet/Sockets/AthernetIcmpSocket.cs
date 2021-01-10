using System;
using System.Threading;
using Athernet.MacLayer;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace Athernet.Sockets
{
    public class AthernetIcmpSocket
    {
        public class NewDatagramEventArgs : EventArgs
        {
            public IcmpDatagram Datagram;
        }

        public delegate void NewDatagramEventHandler(object sender, NewDatagramEventArgs e);

        public event NewDatagramEventHandler NewDatagram;
        
        private readonly Random _random = new Random();

        private IpV4Address _localAddress;
        private IpV4Address _remoteAddress;

        private ushort _sequenceNumber;

        private uint _acknowledgmentNumber;
        private TcpState _tcpState = TcpState.Closed;

        private Mac _athernetMac;
        
        public AthernetIcmpSocket(Mac mac)
        {
            _athernetMac = mac;
        }
        
        public void Listen()
        {
            _athernetMac.DataAvailable += OnAthernetMacOnDataAvailable;
            _athernetMac.StartReceive();
        }

        private void OnAthernetMacOnDataAvailable(object? sender, DataAvailableEventArgs args)
        {
            Console.WriteLine("-> [ICMP]");
            var icmp = new Packet(args.Data, DateTime.Now, DataLinkKind.IpV4).IpV4.Icmp;
            OnNewDatagramReceived(icmp);
        }

        public void Break()
        {
            _athernetMac.DataAvailable -= OnAthernetMacOnDataAvailable;
        }

        private void OnNewDatagramReceived(IcmpDatagram icmpDatagram)
        {
            NewDatagram?.Invoke(this, new NewDatagramEventArgs{Datagram = icmpDatagram});
        }
        
        public void SendIcmpPacket(IpV4Address destination, byte[] payload) =>
            SendIcmpPacket(destination, new Datagram(payload));
        
        
        public void SendIcmpPacket(IpV4Address destination, Datagram datagram = null)
        {
            var ipV4Layer = new IpV4Layer()
            {
                Source = _localAddress,
                CurrentDestination = destination,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                HeaderChecksum = null,
                Identification = IPIdentification,
                Protocol = null,
                Ttl = 100,
                TypeOfService = 0
            };
            var icmpLayer = new IcmpEchoLayer()
            {
                Checksum = null,
                Identifier = IcmpIdentifier,
                SequenceNumber = _sequenceNumber
            };
            PacketBuilder builder;
            if (datagram != null)
            {
                var payloadLayer = new PayloadLayer()
                {
                    Data = datagram
                };
                builder = new PacketBuilder( ipV4Layer, icmpLayer, payloadLayer);
            }
            else
            {
                builder = new PacketBuilder( ipV4Layer, icmpLayer);
            }
            var packet = builder.Build(DateTime.Now);
            
            _athernetMac.AddPayload(Arp(_remoteAddress.ToString()), packet.Buffer);
            
            Console.WriteLine(
                $"-> [ICMP] PayloadLen={datagram?.Length}");
        }

        public ushort IcmpIdentifier { get; set; }

        public ushort IPIdentification { get; set; }

        public delegate byte ArpDelegate(string address);

        public readonly ArpDelegate Arp = address =>
        {
            const string client = "192.168.1.2";
            return address == client ? (byte) 1 : (byte) 0;
        };

    }
}