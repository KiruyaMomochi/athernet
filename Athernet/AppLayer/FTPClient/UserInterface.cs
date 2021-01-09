using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Athernet.AppLayer.FTPClient
{
    public class UserInterface
    {
        public static string NetworkEnvironment;
        public static bool KeepShell = true;
        public string TestString =
            "USER Anonymous"   + Utils.EOL +
            "PASS a"           + Utils.EOL +
            "PWD "             + Utils.EOL +
            "CWD mud"          + Utils.EOL +
            "PASV"             + Utils.EOL +
            "LIST"             + Utils.EOL +
            "PASV"             + Utils.EOL +
            "RETR rfc1918.txt" + Utils.EOL +
            "QUIT" + Utils.EOL +
            "q";
        public StringReader Reader;
        public Command CurrentCommand;

        public ProtocolInterpreter UserPI { get; private set; }
        public UserInterface(System.String DestinationDomain = "ftp.zince.tech", int DestinationPort = 21)
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
                Console.WriteLine(ReceivedMessage.FullMessage);
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
            if (UserPI.Connection.Connected)
            {
                UserPI.Connection.Close();
            }
        }

    }

}