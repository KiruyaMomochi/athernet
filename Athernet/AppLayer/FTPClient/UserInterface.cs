using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Athernet.AppLayer.FTPClient
{
    public class UserInterface
    {
        public static string NetworkEnvironment;
        public static bool KeepShell = true;
        public Command CurrentCommand;
        public ProtocolInterpreter UserPI { get; private set; }
        public UserInterface(String DestinationDomain = "ftp.zince.tech", int DestinationPort = 21)
        {
            UserPI = new ProtocolInterpreter(DestinationDomain, DestinationPort);
            NetworkEnvironment = UserPI.UnderAthernet ? "AUDIO" : "INTERNET";
            CurrentCommand = new Command();
        }
        /// <summary>
        /// Interactive Shell.
        /// </summary>
        public void Shell()
        {
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
            Console.WriteLine(new String('=',Console.WindowWidth));
            Console.WriteLine();
        }
        public void LoopPrompt()
        {
            while (KeepShell)
            {
                Message ReceivedMessage = UserPI.ReceiveMessage();
                UserPI.TakeAction(ReceivedMessage, CurrentCommand);
                Console.Write("ftp > ");

                String UserInput = Console.ReadLine();
                //Debug.WriteLine("UserInput = " + UserInput);
                if (UserInput == "q")
                {
                    break;
                }
                var UserCommand = new Command(UserInput);
                if (UserCommand.Empty)
                {
                    continue;
                }
                UserPI.SendCommand(UserCommand);
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

    public class Command
    {
        public String Name { get; private set; }
        public String Argument { get; private set; }
        public bool Empty { get; private set; } = true;
        public Command()
        {
            //Debug.WriteLine("Class Command Created WITHOUT input.");
        }

        public Command(String UserInput)
        {
            //Debug.WriteLine($"Class Command Created WITH input = \"{UserInput}\"");
            if (UserInput != null)
            {
                Parse(UserInput);
            }
        }
        public Command DeepClone()
        {
            Command other = new Command();
            other.Name = this.Name;
            other.Argument = this.Argument;
            return other;
        }
        override public string ToString()
        {
            return Name + " " + Argument + Utils.EOL;
        }
        public byte[] ToBytes()
        {
            Encoding utf8 = Encoding.UTF8;
            //Debug.WriteLine("Encoding set to UTF8.");
            return utf8.GetBytes(this.ToString());
        }
        public bool Parse(String UserInput)
        {
            int CommandMaxCount = 2;
            String[] UserInputVector = UserInput.Split(" ", CommandMaxCount, StringSplitOptions.RemoveEmptyEntries);
            Debug.WriteLine($"length = {UserInputVector.Length}");

            if (UserInputVector.Length == 0)
            {
                Debug.WriteLine("Karappo!");
                return true;
            }

            Empty = false;
            Name = UserInputVector.First().Trim();

            if (UserInputVector.Length == CommandMaxCount)
            {
                Argument = UserInputVector.Last().Trim();
            }
            Debug.WriteLine($"Name = \"{Name}\"");
            Debug.WriteLine($"Argument = \"{Argument}\"");
            return true;
        }
    }
}
