using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Athernet.MacLayer;
using Athernet.Sockets;
using PcapDotNet.Packets.IpV4;

namespace Athernet.AppLayer.AthernetFTPClient
{
    public class DataTransferProcess
    {
        private const int BufferSize = 114514;
        public bool UnderAthernet { get; set; } = false;
        
        public AthernetTcpSocket AudioConnection { get; set; }
        public IPAddress DestinationAddress { get; private set; }
        public byte[] RecvBuffer { get; private set; }
        public int DestinationPort { get; private set; }
        public string RecvMsg { get; private set; }
        public Task TransmissionTask { get; set; }
        //public DataTransferProcess(String Domain = "ftp.zince.tech", int Port = 20)
        //{
        //    //NetworkEnvironment = UserPI.UnderAthernet ? "ATHERNET" : "INTERNET";
        //    //CurrentCommand = new Command();
        //    DestinationAddress = Array.FindAll(
        //        Dns.GetHostAddresses(Domain),
        //        ip => ip.AddressFamily == AddressFamily.InterNetwork)
        //    .First();

        //    DestinationPort = Port;

        //    if (BuildConnection(DestinationAddress, DestinationPort))
        //    {
        //        Debug.WriteLine("DTP Slave Connection Built Successfully.");
        //    }

        //    Connection = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Tcp);

        //}

        private Mac _macNode;
        
        public DataTransferProcess(IPAddress Address , int Port, Mac macNode)
        {
            _macNode = macNode;
            TransmissionTask = null;
            RecvMsg = "";
            RecvBuffer = new byte[BufferSize];
            //NetworkEnvironment = UserPI.UnderAthernet ? "ATHERNET" : "INTERNET";
            //CurrentCommand = new Command();
            DestinationAddress = Address;
            DestinationPort = Port;
            AudioConnection = new AthernetTcpSocket(macNode);
            
            if (BuildConnection(DestinationAddress, DestinationPort))
            {
                Debug.WriteLine("DTP Slave Connection Built Successfully.");
            }
        }

        private BlockingCollection<string> _messageQueue = new();
        public bool BuildConnection(IPAddress IPAddress, int DestinationPort)
        {
            // Send Data Under Internet
            AudioConnection.Bind(1989, new IpV4Address(IPAddress.ToString()), (ushort) DestinationPort);
            Debug.WriteLine(message: $"[DataTransmission] Destination address : {IPAddress}");
            AudioConnection.Listen();
            AudioConnection.Open();
            AudioConnection.NewDatagram += (sender, args) => RecvMsg += args.Datagram.Decode(Encoding.UTF8);
            return true;
        }
        
        // public void ReceiveData()
        // {
        //     //for (var i = 0; i < 2; i++)
        //     while (true)
        //     {
        //         if (AudioConnection.Connected)
        //         {
        //             RecvMsg += _messageQueue.Take();
        //             Debug.WriteLine($"Received: \"{RecvMsg}\"");
        //         }
        //         else
        //         {
        //             return;
        //         }
        //     }
        // }

    }
}
