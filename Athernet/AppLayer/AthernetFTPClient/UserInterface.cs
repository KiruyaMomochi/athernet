using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Athernet.AppLayer.FTPClient;

namespace Athernet.AppLayer.AthernetFTPClient
{
    public class UserInterface
    {
        public static string NetworkEnvironment;
        public static bool KeepShell = true;
        public string TestString =
            "USER Anonymous"   + FTPClient.Utils.EOL +
            "PASS a"           + FTPClient.Utils.EOL +
            "PWD "             + FTPClient.Utils.EOL +
            "CWD mud"          + FTPClient.Utils.EOL +
            "PASV"             + FTPClient.Utils.EOL +
            "LIST"             + FTPClient.Utils.EOL +
            "PASV"             + FTPClient.Utils.EOL +
            "RETR rfc1918.txt" + FTPClient.Utils.EOL +
            "QUIT" + FTPClient.Utils.EOL +
            "q";
        public StringReader Reader;
        public Command CurrentCommand;

        public ProtocolInterpreter UserPI { get; private set; }
        public UserInterface(System.String DestinationDomain = "10.20.212.86", int DestinationPort = 21)
        {
            UserPI = new ProtocolInterpreter(DestinationDomain, DestinationPort);
            NetworkEnvironment = UserPI.UnderAthernet ? "ATHERNET" : "INTERNET";
            CurrentCommand = new Command();
        }
        
        /// <summary>
        /// Interactive Shell.
        /// </summary>
        public void Shell()
        {
            Reader = new StringReader(TestString);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                Console.WriteLine();
                // TODO: Kill command when ctrl + c is pressed.
            };
            WelcomeMessage();
            LoopPrompt();
            TailTask();
        }
        public void WelcomeMessage()
        {
            Console.WriteLine("FTP Client for Athernet");
            Console.WriteLine($"Under {NetworkEnvironment}");
            Console.WriteLine(new System.String('=', Console.WindowWidth));
            Console.WriteLine();
        }
        public void LoopPrompt()
        {
            while (KeepShell)
            {
                Message ReceivedMessage = UserPI.ReceiveMessage();
                Console.Write(ReceivedMessage.FullMessage);
                StatusCodeClass CurrentStateCodeClass = UserPI.TakeAction(ReceivedMessage, CurrentCommand); // When CurrentCommand.Empty == true, is building connection.
                Debug.WriteLine($"CurrentStateCodeClass = {CurrentStateCodeClass}");
                // TODO: Need to update last **VALID** command!!!!!!!!!!!

                if (CurrentStateCodeClass != StatusCodeClass.PositivePreliminaryReply)
                {
                    Console.Write("ftp > ");
                    String UserInput = Console.ReadLine();
                    //System.String UserInput = Reader.ReadLine();
                    Console.WriteLine(UserInput);
                    //Debug.WriteLine("UserInput = " + UserInput);
                    if (UserInput == "q")
                    {
                        break;
                    }
                    var UserCommand = new Command(UserInput);
                    if (UserCommand.Empty) // invalid
                    {
                        continue;
                    }
                    else
                    {
                        CurrentCommand = UserCommand;
                    }
                    UserPI.SendCommand(CurrentCommand);
                }
                else
                {
                    continue;
                }
                //UserPI.ReceiveMessage();
            }

        }

        public void TailTask()
        {
            if (UserPI.AudioConnection.Connected)
            {
                UserPI.AudioConnection.Break();
            }
        }

    }

}