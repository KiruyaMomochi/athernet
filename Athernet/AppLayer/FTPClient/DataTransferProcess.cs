using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Athernet.AppLayer.FTPClient
{
    public class DataTransferProcess
    {
        private const int BufferSize = 114514;
        public bool UnderAthernet { get; set; } = false;
        public Socket Connection { get; set; }
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
        public DataTransferProcess(IPAddress Address , int Port = 20)
        {
            TransmissionTask = null;
            RecvMsg = "";
            RecvBuffer = new byte[BufferSize];
            //NetworkEnvironment = UserPI.UnderAthernet ? "ATHERNET" : "INTERNET";
            //CurrentCommand = new Command();
            DestinationAddress = Address;
            DestinationPort = Port;
            Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (BuildConnection(DestinationAddress, DestinationPort))
            {
                Debug.WriteLine("DTP Slave Connection Built Successfully.");
            }


        }

        public bool BuildConnection(IPAddress IPAddress, int DestinationPort)
        {
            if (!UnderAthernet)
            {
                // Send Data Under Internet
                Debug.WriteLine(message: $"[DataTransmission] Destination address : {IPAddress}");
                Connection.Connect(new IPEndPoint(IPAddress, DestinationPort));
                return Connection.Connected;
            }
            else
            {
                /// <remarks>
                /// TODO: SendDataUnderAthernet(cmd);
                /// </remarks>
                return false;
            }
        }

        public void ReceiveData()
        {
            //for (var i = 0; i < 2; i++)
            while (true)
            {
                if (Connection.Connected)
                {

                    int BytesRecv = Connection.Receive(RecvBuffer);
                    RecvMsg += Encoding.UTF8.GetString(RecvBuffer.Take(BytesRecv).ToArray());
                    Debug.WriteLine($"Received: \"{RecvMsg}\"");
                }
                else
                {
                    return;
                }
            }
        }

    }
}
