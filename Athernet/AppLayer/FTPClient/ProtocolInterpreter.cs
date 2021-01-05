using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Athernet.AppLayer.FTPClient
{
    class ProtocolInterpreter
    {
        public bool underAthernet { get; set; } = false;
        public Socket Connection { get; set; }
        public byte[] RecvBuffer { get; set; } = new byte[256];
        public ProtocolInterpreter()
        {
            if (BuildConnection())
                Debug.WriteLine("TCP Connection Built Successfully.");
        }
        public void SendCommand(Command cmd)
        {
            Debug.WriteLine($"Command about to send: {cmd.ToString()}");
            if (Connection.Connected)
                Connection.Send(cmd.ToBytes());
            else
                Debug.WriteLine("Closed.");
        }
        public void ReceiveMessage()
        {
            Encoding ASCII = Encoding.ASCII;
            var BytesRecv = Connection.Receive(RecvBuffer);
            Debug.WriteLine($"Received: \"{ASCII.GetString(RecvBuffer.Take(BytesRecv).ToArray())}\"");
        }
        public bool BuildConnection(String DestinationDomain = "ftp.zince.tech", int DestinationPort = 21)
        {
            if (!underAthernet)
            {
                //Send Command Under Internet
                var DestinationAddress = Array.FindAll(
                        Dns.GetHostAddresses(DestinationDomain),
                        ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .First();
                Debug.WriteLine($"Destination address : {DestinationAddress}");
                //DestinationAddress.ToList().ForEach(Console.WriteLine);
                //Debug.WriteLine(DestinationAddress.ToString());
                Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //Connection.Bind(new IPEndPoint(IPAddress.Any, 0));
                Connection.Connect(new IPEndPoint(DestinationAddress, DestinationPort));
                return Connection.Connected;
            }
            else
                /// <remarks>
                /// TODO: SendCommandUnderAthernet(cmd);
                /// </remarks>
                return false;
        }
    }

}
