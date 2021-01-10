using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Athernet.AppLayer.FTPClient;
using Athernet.MacLayer;
using Athernet.Sockets;
using PcapDotNet.Packets.IpV4;

namespace Athernet.AppLayer.AthernetFTPClient
{
    public class ProtocolInterpreter
    {
        public bool UnderAthernet { get; set; } = true;
        public bool PassiveConnected { get; private set; } = false;
        public AthernetTcpSocket Connection { get; set; }
        public AthernetTcpSocket AudioConnection { get; set; }
        public Command CurrentCommand { get; private set; }
        public DataTransferProcess UserDTP { get; private set; }
        public byte[] RecvBuffer { get; private set; }
        public IPAddress DestinationAddress { get; private set; }
        public int DestinationPort { get; private set; }

        public ProtocolInterpreter(System.String DestinationDomain, int Port)
        {
            RecvBuffer = new byte[8192];
            DestinationAddress = Array.FindAll(
                    Dns.GetHostAddresses(DestinationDomain),
                    ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .First();
            DestinationPort = Port;

            if (BuildConnection(DestinationAddress, DestinationPort))
            {
                Debug.WriteLine("TCP Connection Built Successfully.");
            }

            UserDTP = null;
        }

        public void SendCommand(Command cmd)
        {
            Debug.WriteLine($"Command about to send: {cmd}");
            if (Connection.Connected)
            {
                Connection.Send(cmd.ToBytes());
                CurrentCommand = cmd.DeepClone();
                Debug.WriteLine($"CurrentCommand = {CurrentCommand}");
            }
            else
            {
                Debug.WriteLine("Closed.");
            }
        }

        // TODO: implement this to check the command is executed on remote successfully, return the response class is enough
        public StatusCodeClass TakeAction(Message Message, Command CurrentCommand = null,
            Command PreviousCommand = null)
        {
            //bool KeepAction = true;
            Message ActionMessage = Message;

            // Connection Establishment
            if (CurrentCommand == null || CurrentCommand.Empty)
            {
                return ActionMessage.GetCodeClass();
                switch (ActionMessage.StatusCode)
                {
                    // TODO: Check success for each one
                    case FtpStatusCode.ServiceTemporarilyNotAvailable:
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
                    case "PASV":
                        return ProcessPassiveConnectionRequest(ActionMessage, PreviousCommand);
                    case "LIST":
                        return ProcessListRequest(ActionMessage, PreviousCommand);
                    case "RETR":
                        return ProcessRetrieveRequest(ActionMessage, PreviousCommand);
                    case "USER":
                    case "PASS":
                    case "PWD":
                    case "CWD":
                    default:
                        return ActionMessage.GetCodeClass();
                    //Debug.WriteLine(ActionMessage.FullMessage);
                }
            }

            // Should not reached
            throw new InvalidOperationException();
        }

        public StatusCodeClass ProcessPassiveConnectionRequest(Message ActionMessage, Command PreviousCommand = null)
        {
            Debug.WriteLine(ActionMessage.StatusCode);
            switch (ActionMessage.StatusCode)
            {
                // 2XX
                case FtpStatusCode.EnteringPassive:
                    PassiveConnected = true;
                    if (UserDTP != null)
                    {
                        if (UserDTP.Connection.Connected)
                            UserDTP.Connection.Close();
                        UserDTP = null; // Release previously-allocated unwanted resources/connections
                    }

                    //ActionMessage.
                    string[] ParsedResult = Regex.Replace(
                            ActionMessage.FullMessage,
                            "[^0-9]",
                            " ")
                        .Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
                        .TakeLast(6)
                        .ToArray();
                    var AddressString = ParsedResult[0] + "." + ParsedResult[1] + "." + ParsedResult[2] + "." +
                                        ParsedResult[3];
                    Debug.WriteLine(ParsedResult);
                    Debug.WriteLine(AddressString);
                    IPAddress Address = IPAddress.Parse(AddressString);
                    int Port = int.Parse(ParsedResult[4]) * 256 + int.Parse(ParsedResult[5]);
                    Debug.WriteLine(Port);
                    UserDTP = new DataTransferProcess(Address, Port);
                    return ActionMessage.GetCodeClass();
                // TODO: Build Connection here!
                // Reminder: If previously built. then TERMINATE IT and build another one!
                // 5XX + 421(ServiceNotAvailable)
                case FtpStatusCode.ServiceNotAvailable: // 421
                case FtpStatusCode.CommandSyntaxError:
                case FtpStatusCode.ArgumentSyntaxError:
                case FtpStatusCode.CommandNotImplemented:
                case FtpStatusCode.NotLoggedIn:
                    PassiveConnected = false;
                    return ActionMessage.GetCodeClass();
                default:
                    throw new InvalidOperationException();
            }

            //if (ActionMessage.StateCode.Code == FtpStatusCode.EnteringPassive)
            throw new InvalidOperationException();
        }

        public StatusCodeClass ProcessRetrieveRequest(Message ActionMessage, Command PreviousCommand = null)
        {
            switch (ActionMessage.StatusCode)
            {
                // 1XX
                case FtpStatusCode.DataAlreadyOpen:
                case FtpStatusCode.OpeningData:
                case FtpStatusCode.RestartMarker:
                    //PassiveConnected = false;
                    Debug.WriteLine("receiving data...");
                    if (UserDTP.TransmissionTask == null)
                    {
                        UserDTP.TransmissionTask = Task.Run(UserDTP.ReceiveData);
                    }

                    return ActionMessage.GetCodeClass();
                // 2XX
                case FtpStatusCode.ClosingData:
                case FtpStatusCode.FileActionOK:
                    UserDTP.Connection.Disconnect(false);
                    UserDTP.TransmissionTask.Wait();
                    //Debug.WriteLine($"Task.Run(UserDTP.ReceiveData) is completed? {task.IsCompleted}")
                    Console.WriteLine(
                        $"{UserDTP.RecvMsg.Length * 8} bytes ({UserDTP.RecvMsg.Length * 8 / 1024.0} KiB) received.");
                    Console.WriteLine("Choose the path to save: (Default to C:\\%AppData%\\)");
                    var SavePath = Console.ReadLine();
                    if (SavePath == null)
                    {
                        SavePath = "C:\\%AppData%\\";
                    }

                    Debug.WriteLine($"Save filename: {CurrentCommand.Argument}");
                    System.IO.StreamWriter file = new System.IO.StreamWriter(SavePath + CurrentCommand.Argument);
                    file.WriteLine(UserDTP.RecvMsg);
                    file.Close();
                    Console.WriteLine($"File \'{CurrentCommand.Argument}\' saved at {SavePath}.");
                    Debug.WriteLine("data received!");
                    Debug.WriteLine($"{UserDTP.RecvMsg.Length} characters received");
                    //PassiveConnected = true;
                    return ActionMessage.GetCodeClass();
                // 4XX
                case FtpStatusCode.CantOpenData:
                case FtpStatusCode.ConnectionClosed:
                case FtpStatusCode.ActionAbortedLocalProcessingError:
                case FtpStatusCode.ActionNotTakenFileUnavailableOrBusy:
                case FtpStatusCode.ServiceNotAvailable:
                // 5XX
                case FtpStatusCode.CommandSyntaxError:
                case FtpStatusCode.ArgumentSyntaxError:
                case FtpStatusCode.NotLoggedIn:
                case FtpStatusCode.ActionNotTakenFileUnavailable:
                    PassiveConnected = false;
                    return ActionMessage.GetCodeClass();
                default:
                    throw new InvalidOperationException();
            }

            throw new InvalidOperationException();
        }

        public StatusCodeClass ProcessListRequest(Message ActionMessage, Command PreviousCommand = null)
        {
            //x = Task.Run(() => { return 1; });
            //X.wait()
            switch (ActionMessage.StatusCode)
            {
                // 1XX
                case FtpStatusCode.DataAlreadyOpen:
                case FtpStatusCode.OpeningData:
                    // TODO here!
                    Debug.WriteLine("receiving data...");
                    if (UserDTP.TransmissionTask == null)
                    {
                        UserDTP.TransmissionTask = Task.Run(UserDTP.ReceiveData);
                    }

                    return ActionMessage.GetCodeClass();
                    break;
                // 2XX
                case FtpStatusCode.ClosingData:
                case FtpStatusCode.FileActionOK:
                    UserDTP.Connection.Disconnect(false);
                    UserDTP.TransmissionTask.Wait();
                    //Debug.WriteLine($"Task.Run(UserDTP.ReceiveData) is completed? {task.IsCompleted}")
                    Console.WriteLine($"{UserDTP.RecvMsg}");
                    Debug.WriteLine("data received...");
                    Debug.WriteLine($"{UserDTP.RecvMsg.Length} characters received.");

                    return ActionMessage.GetCodeClass();
                // 4XX
                case FtpStatusCode.CantOpenData:
                case FtpStatusCode.ConnectionClosed:
                case FtpStatusCode.ActionAbortedLocalProcessingError:
                case FtpStatusCode.ActionNotTakenFileUnavailableOrBusy:
                case FtpStatusCode.ServiceNotAvailable:
                // 5XX
                case FtpStatusCode.CommandSyntaxError:
                case FtpStatusCode.ArgumentSyntaxError:
                case FtpStatusCode.CommandNotImplemented:
                case FtpStatusCode.NotLoggedIn:
                    PassiveConnected = false;
                    return ActionMessage.GetCodeClass();
                default:
                    throw new InvalidOperationException();
            }
        }

        public static bool KeepActionRequired(Message Message)
        {
            return (Message.GetCodeClass() == StatusCodeClass.TransientNegativeCompletionReply);
        }

        public Message ReceiveMessage()
        {
            Message RecvMsg;
            //for (var i = 0; i < 2; i++)
            {
                int BytesRecv = Connection.Receive(RecvBuffer);

                string ReceivedMessage = Encoding.UTF8.GetString(RecvBuffer.Take(BytesRecv).ToArray());
                string CodeText = new string(ReceivedMessage.Take(StatusCode.LengthNumber).ToArray());
                RecvMsg = new Message(CodeText, ReceivedMessage);
                Debug.WriteLine($"Received: \"{RecvMsg.FullMessage}\" Code: {RecvMsg.StatusCode}");
            }
            return RecvMsg;
        }

        public bool BuildConnection(IPAddress DestinationAddress, int DestinationPort)
        {
            var node1 = new Mac(1, 2, 0, 2048);
            AudioConnection = new AthernetTcpSocket(node1);
            AudioConnection.Listen();
            AudioConnection.Bind(2333, new IpV4Address(DestinationAddress.ToString()), (ushort) DestinationPort);
            AudioConnection.Open();
            return true;
        }
    }
}