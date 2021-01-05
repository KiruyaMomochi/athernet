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
        
        /// <summary>
        /// Interactive Shell.
        /// </summary>
        static public void Shell()
        {
            var UserPI = new ProtocolInterpreter();
            var Environment = UserPI.underAthernet ? "AUDIO" : "INTERNET";
            while (true)
            {
                Console.Write($"[{Environment}] ftp > ");

                String UserInput = Console.ReadLine();
                Debug.WriteLine("UserInput = "+UserInput);
                if (UserInput == "q")
                    break;
                var UserCommand = new Command(UserInput);
                UserPI.SendCommand(UserCommand);
                UserPI.ReceiveMessage();
            }
            UserPI.Connection.Close();
        }
    }

    public class Command
    {
        public String Name { get; private set; }
        public String Argument { get; private set; }

        public Command()
        {
            Debug.WriteLine("Class Command Created WITHOUT input.");
        }

        public Command(String UserInput)
        {
            Debug.WriteLine($"Class Command Created WITH input = \"{UserInput}\"");
            Parse(UserInput);
        }

        override public string ToString()
        {
            return Name + " " + Argument + Utils.EOL;
        }
        public byte[] ToBytes()
        {
            Encoding ASCII = Encoding.ASCII;
            Debug.WriteLine("Encoding set to ASCII.");
            return ASCII.GetBytes(this.ToString());
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
