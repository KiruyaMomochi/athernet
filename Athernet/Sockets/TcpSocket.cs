using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Athernet.Utils;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using AddressFamily = System.Net.Sockets.AddressFamily;

namespace Athernet.Sockets
{
    public class TcpSocket : IDisposable
    {
        public TcpState TcpState
        {
            get => _tcpState;
            set
            {
                Console.WriteLine($"    State: [{_tcpState}] -> [{value}]");
                _tcpState = value;
            }
        }

        public class NewDatagramEventArgs : EventArgs
        {
            public Datagram Datagram;
        }

        public delegate void NewDatagramEventHandler(object sender, NewDatagramEventArgs e);

        public event NewDatagramEventHandler NewDatagram;

        public ushort IPIdentification { get; set; }

        private readonly Random _random = new Random();
        private uint _sequenceNumber;
        private EthernetLayer _ethernetLayer = null;

        private IpV4Address _localAddress;
        private IpV4Address _remoteAddress;
        private MacAddress _localMacAddress;
        private MacAddress _remoteMacAddress;

        private ushort _localPort;
        private ushort _remotePort;

        private readonly PacketCommunicator _communicator;
        private uint _acknowledgmentNumber;
        private TcpState _tcpState = TcpState.Closed;

        public TcpSocket(int deviceIndex)
        {
            var device = LivePacketDevice.AllLocalMachine[deviceIndex];
            _communicator = device.Open(65536,
                PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal,
                1000);
            var address = device.Addresses.First(x => x.Address is IpV4SocketAddress).Address;
            if (address is IpV4SocketAddress ipV4SocketAddress)
            {
                _localAddress = ipV4SocketAddress.Address;
                _localMacAddress = device.GetMacAddress();
                var gateway = device.GetNetworkInterface().GetIPProperties().GatewayAddresses
                    .First(x => x.Address.AddressFamily == AddressFamily.InterNetwork).Address;
                _remoteMacAddress = Arp.Lookup(gateway);
            }
            else
            {
                throw new InvalidOperationException("Only IpV4 address is supported.");
            }

            _ethernetLayer = new EthernetLayer()
            {
                Source = _localMacAddress,
                Destination = _remoteMacAddress,
                EtherType = EthernetType.None
            };
        }

        public void Bind(ushort localPort, IpV4Address remoteAddress, ushort remotePort)
        {
            _remoteAddress = remoteAddress;
            _remotePort = remotePort;
            _localPort = localPort;
        }

        public void Open()
        {
            if (TcpState != TcpState.Closed)
            {
                throw new InvalidOperationException("The tcp has been placed in a opening state.");
            }

            _sequenceNumber = (uint) _random.Next();
            SendTcpPacket(TcpControlBits.Synchronize);
            TcpState = TcpState.SynSent;

            SpinWait.SpinUntil(() => TcpState == TcpState.Established);
        }

        public void Listen()
        {
            _communicator.SetFilter($"src host {_remoteAddress.ToString()} and dst port {_localPort}");
            _communicator.ReceivePackets(0, x =>
            {
                var tcp = x.Ethernet.IpV4.Tcp;
                Console.WriteLine(
                    $"<- [{TcpState}] {tcp.ControlBits} Seq={tcp.SequenceNumber} Win={tcp.Window} Ack={tcp.AcknowledgmentNumber} PayloadLen={tcp.PayloadLength}");
                OnNewDatagramReceived(tcp);
            });
        }

        public void Break() => _communicator.Break();

        private void OnNewDatagramReceived(TcpDatagram tcpDatagram)
        {
            if (tcpDatagram.IsReset)
            {
                TcpState = TcpState.Closed;
                return;
            }

            // Check ACK
            if (tcpDatagram.IsAcknowledgment && tcpDatagram.AcknowledgmentNumber != _sequenceNumber)
            {
                Console.WriteLine(
                    $"    Ignored since ACK: want {_sequenceNumber}, received {tcpDatagram.AcknowledgmentNumber}");
            }

            // Stop and wait
            _acknowledgmentNumber = (uint) (tcpDatagram.SequenceNumber + tcpDatagram.PayloadLength);

            switch (TcpState)
            {
                case TcpState.SynSent when (tcpDatagram.IsSynchronize && tcpDatagram.IsAcknowledgment):
                    _acknowledgmentNumber += 1;
                    SendTcpPacket(TcpControlBits.Acknowledgment);
                    TcpState = TcpState.Established;
                    break;
                case TcpState.Established when (tcpDatagram.IsFin && tcpDatagram.IsAcknowledgment):
                    _acknowledgmentNumber += 1;
                    TcpState = TcpState.CloseWait;
                    SendTcpPacket(TcpControlBits.Acknowledgment | TcpControlBits.Fin);
                    TcpState = TcpState.LastAck;
                    break;
                case TcpState.Established when tcpDatagram.IsFin:
                    SendTcpPacket(TcpControlBits.Acknowledgment);
                    _acknowledgmentNumber += 1;
                    TcpState = TcpState.CloseWait;
                    SendTcpPacket(TcpControlBits.Fin);
                    TcpState = TcpState.LastAck;
                    break;
                case TcpState.Established when tcpDatagram.IsPush:
                    SendTcpPacket(TcpControlBits.Acknowledgment);
                    OnNewPayloadReceived(tcpDatagram.Payload);
                    break;
                case TcpState.Established:
                    SendTcpPacket(TcpControlBits.Acknowledgment);
                    break;
                case TcpState.LastAck when tcpDatagram.IsAcknowledgment:
                    TcpState = TcpState.Closed;
                    Break();
                    break;
                case TcpState.WaitAck when tcpDatagram.IsAcknowledgment:
                    TcpState = TcpState.Established;
                    break;
                case TcpState.Closed:
                    return;
                default:
                    Console.WriteLine($"    Ignored Packet: {tcpDatagram.ControlBits}");
                    break;
            }
        }

        private void OnNewPayloadReceived(Datagram tcpDatagramPayload)
        {
            NewDatagram?.Invoke(this, new NewDatagramEventArgs {Datagram = tcpDatagramPayload});
        }

        public void SendTcpPacket(TcpControlBits tcpControlBits, byte[] payload) =>
            SendTcpPacket(tcpControlBits, new Datagram(payload));

        public void SendIpv4Packet(Packet ipv4Packet)
        {
            _communicator.SendPacket(PacketBuilder.Build(DateTime.Now, _ethernetLayer, ipv4Packet.IpV4.ExtractLayer(),
                ipv4Packet.IpV4.Payload.ExtractLayer()));
        }

        public void SendTcpPacket(TcpControlBits tcpControlBits, Datagram datagram = null)
        {
            var ipV4Layer = new IpV4Layer()
            {
                Source = _localAddress,
                CurrentDestination = _remoteAddress,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                HeaderChecksum = null,
                Identification = IPIdentification,
                Protocol = null,
                Ttl = 100,
                TypeOfService = 0
            };
            var tcpLayer = new TcpLayer()
            {
                SourcePort = _localPort,
                DestinationPort = _remotePort,
                Checksum = null,
                SequenceNumber = _sequenceNumber,
                ControlBits = tcpControlBits,
                Window = TcpWindowSize,
                UrgentPointer = 0,
                Options = TcpOptions.None,
                AcknowledgmentNumber = (tcpControlBits & TcpControlBits.Acknowledgment) != 0 ? _acknowledgmentNumber : 0
            };
            PacketBuilder builder;
            if (datagram != null)
            {
                var payloadLayer = new PayloadLayer()
                {
                    Data = datagram
                };
                builder = new PacketBuilder(_ethernetLayer, ipV4Layer, tcpLayer, payloadLayer);
            }
            else
            {
                builder = new PacketBuilder(_ethernetLayer, ipV4Layer, tcpLayer);
            }

            var packet = builder.Build(DateTime.Now);
            _communicator.SendPacket(packet);

            Console.WriteLine(
                $"-> [{TcpState}] {tcpLayer.ControlBits} Seq={tcpLayer.SequenceNumber} Win={tcpLayer.Window} Ack={tcpLayer.AcknowledgmentNumber} PayloadLen={datagram?.Length}");

            if ((tcpControlBits & (TcpControlBits.Synchronize | TcpControlBits.Fin)) != 0)
            {
                _sequenceNumber += 1;
            }
            else if (datagram != null)
            {
                _sequenceNumber = (uint) (_sequenceNumber + datagram.Length);
            }
        }

        public void SendPayload(byte[] payload) =>
            SendTcpPacket(TcpControlBits.Push | TcpControlBits.Acknowledgment, payload);

        private const int TcpWindowSize = 1024;

        public void Dispose()
        {
            _communicator?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}