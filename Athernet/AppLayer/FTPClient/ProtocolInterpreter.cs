using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace Athernet.AppLayer.FTPClient
{
    public class ProtocolInterpreter
    {
        public bool UnderAthernet { get; set; } = false;
        public Socket Connection { get; set; }
        public static ManualResetEvent ReceiveEvent { get; private set; }
        public Command CurrentCommand { get; private set; }
        public DataTransferProcess UserDTP { get; private set; }
        public ProtocolInterpreter(String DestinationDomain, int DestinationPort)
        {
            ProtocolInterpreter.ReceiveEvent = new ManualResetEvent(false);
            if (BuildConnection(DestinationDomain, DestinationPort))
            {
                Debug.WriteLine("TCP Connection Built Successfully.");
            }
        }
        public void SendCommand(Command cmd)
        {
            Debug.WriteLine($"Command about to send: {cmd.ToString()}");
            if (Connection.Connected)
            {
                Connection.Send(cmd.ToBytes());
                CurrentCommand = cmd.DeepClone();
                Debug.WriteLine($"CurrentCommand = {CurrentCommand.ToString()}");
            }
            else
            {
                Debug.WriteLine("Closed.");
            }
        }
        public String TakeAction(Message Message, Command CurrentCommand = null, Command PreviousCommand = null)
        {
            bool KeepAction = true;
            Message ActionMessage = Message;
            while (KeepAction)
            {
                // Connection Establishment
                if (CurrentCommand == null || CurrentCommand.Empty)
                {
                    switch (ActionMessage.StateCode.Code)
                    {
                        case FtpStatusCode.ServiceTemporarilyNotAvailable:
                            ActionMessage = ReceiveMessage();
                            Debug.WriteLine(ActionMessage.FullMessage);
                            break;
                        case FtpStatusCode.SendUserCommand:
                        case FtpStatusCode.ServiceNotAvailable:
                        default:
                            Debug.WriteLine(ActionMessage.FullMessage);
                            break;
                    }
                }
                else
                {
                    switch (CurrentCommand.Name)
                    {
                        //case "PASV":
                        //    ProcessPassiveRequest(ActionMessage.FullMessage);
                        //    break;
                        //case "LIST":
                        //    ProcessListRequest(ActionMessage.FullMessage);
                        //    break;
                        //case "RETR":
                        //    ProcessRetrieveRequest(ActionMessage.FullMessage);
                        //    break;
                        case "USER":
                        case "PASS":
                        case "PWD":
                        case "CWD":
                        default:
                            Debug.WriteLine(ActionMessage.FullMessage);
                            break;
                    }
                }

                KeepAction = KeepActionRequired(ActionMessage);
            }
            return ActionMessage.FullMessage;
        }

        public bool KeepActionRequired(Message Message)
        {
            return (Message.StateCode.GetClass() == StateCodeClass.TransientNegativeCompletionReply);
        }
        public Message ReceiveMessage()
        {
            Encoding utf8 = Encoding.UTF8;
            StateObject State = new StateObject();
            //int BytesRecv = Connection.Receive(RecvBuffer);
            State.WorkSocket = Connection;
            ReceiveEvent.Reset();
            Debug.WriteLine("About to receive message");
            Connection.BeginReceive(
                buffer: State.Buffer,
                offset: 0,
                size: StateObject.BufferSize,
                socketFlags: SocketFlags.None,
                callback: new AsyncCallback(ReadCallback),
                state: State);
            ReceiveEvent.WaitOne(new TimeSpan(hours:0, minutes:0, seconds:1));
            var ReceivedMessage = State.StringBuffer.ToString();
            Debug.WriteLine($"Received: \"{ReceivedMessage}\" Code: ");
            var CodeText = State.StringBuffer.ToString().Take(StatusCode.LengthNumber).ToArray().ToString();
            var RecvMsg = new Message(CodeText, ReceivedMessage);
            Debug.WriteLine(CodeText);
            return RecvMsg;
        }
        public static void ReadCallback(IAsyncResult AsyncResult)
        {
            try
            {
                Debug.WriteLine("Try to end receiving...");
                StateObject State = (StateObject)AsyncResult.AsyncState;
                Socket WorkSocket = State.WorkSocket;
                int ReadBytes = WorkSocket.EndReceive(AsyncResult);
                Debug.WriteLine($"Received {ReadBytes} Bytes... Payload = \"{Encoding.UTF8.GetString(State.Buffer, 0, ReadBytes)}\"");
                Console.Write(Encoding.UTF8.GetString(State.Buffer, 0, ReadBytes));
                
                if (ReadBytes > 0)
                {
                    Debug.WriteLine($"Receiving...");
                    WorkSocket.BeginReceive(
                        buffer: State.Buffer,
                        offset: 0,
                        size: StateObject.BufferSize,
                        socketFlags: SocketFlags.None,
                        callback: new AsyncCallback(ReadCallback),
                        state: State);
                    State.StringBuffer.Append(Encoding.UTF8.GetString(State.Buffer, 0, ReadBytes));
                    Debug.WriteLine($"One Callback done.");
                }
                //else
                //{
                //    Debug.Write($"Received.");

                //    if (State.StringBuffer.Length > 1)
                //    {
                //        // All data received when no more bytes get received
                //        ProtocolInterpreter.ReceivedFullMessage = State.StringBuffer.ToString();
                //    }
                //    ProtocolInterpreter.ReceiveEvent.Set();
                //}
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
        public bool BuildConnection(String DestinationDomain, int DestinationPort)
        {
            if (!UnderAthernet)
            {
                //Send Command Under Internet
                var DestinationAddress = Array.FindAll(
                        Dns.GetHostAddresses(DestinationDomain),
                        ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .First();
                Debug.WriteLine($"Destination address : {DestinationAddress}");
                Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Connection.Connect(new IPEndPoint(DestinationAddress, DestinationPort));
                return Connection.Connected;
            }
            else
            {
                /// <remarks>
                /// TODO: SendCommandUnderAthernet(cmd);
                /// </remarks>
                return false;
            }
        }
    }
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] Buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder StringBuffer = new StringBuilder();

        // Client socket.
        public Socket WorkSocket = null;
    }

    public class StatusCode
    {
        //static public FtpStatusCode FTPState;

        static public int LengthNumber = 3;
        public FtpStatusCode Code { get; set; }
        public StatusCode(int Number)
        {
            if (!IsFtpStatusCode(Number))
            {
                Code = FtpStatusCode.Undefined;
            }
            else
            {
                Code = (FtpStatusCode) Number;
            }
        }
        public StatusCode(String NumberString)
        {
            int result;
            bool IsNumber = int.TryParse(NumberString, out result);
            if (IsNumber && !IsFtpStatusCode(result))
            {
                Code = (FtpStatusCode) result;
            }
            else
            {
                Code = FtpStatusCode.Undefined;
            }
        }
        public StateCodeClass GetClass()
        {
            return (StateCodeClass) ((int) Code % 100);
        }
        public bool IsFtpStatusCode(int Number)
        {
            return Enum.IsDefined(typeof(FtpStatusCode), Number) && Number != (int)FtpStatusCode.Undefined;
        }
    }

    public enum StateCodeClass : int
    {
        PositivePreliminaryReply = 1,
        PositiveCompletionReply = 2,
        PositiveIntermediateReply = 3,
        TransientNegativeCompletionReply = 4,
        PermanentNegativeCompletionReply = 5
    }

    public class Message
    {
        public StatusCode StateCode { get; set; }
        public String FullMessage { get; set; }

        public Message(String CodeText, String FullText)
        {
            StateCode = new StatusCode(CodeText);
            FullMessage = FullText;
        }
    }


}
