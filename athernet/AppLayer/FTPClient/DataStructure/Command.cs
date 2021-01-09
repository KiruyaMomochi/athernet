using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athernet.AppLayer.FTPClient
{
    public class Command
    {
        public System.String Name { get; private set; }
        public System.String Argument { get; private set; }
        public bool Empty { get; private set; } = true;
        public Command()
        {
            //Debug.WriteLine("Class Command Created WITHOUT input.");
        }

        public Command(System.String UserInput)
        {
            //Debug.WriteLine($"Class Command Created WITH input = \"{UserInput}\"");
            if (UserInput != null)
            {
                Parse(UserInput);
            }
        }
        public Command DeepClone()
        {
            Command other = new Command
            {
                Name = this.Name,
                Argument = this.Argument
            };
            return other;
        }
        override public string ToString()
        {
            return Name + " " + Argument + Utils.EOL;
        }
        public byte[] ToBytes()
        {
            Encoding ASCII = Encoding.ASCII;
            //Debug.WriteLine("Encoding set to ASCII.");
            return ASCII.GetBytes(this.ToString());
        }
        public bool Parse(System.String UserInput)
        {
            int CommandMaxCount = 2;
            System.String[] UserInputVector = UserInput.Split(" ", CommandMaxCount, StringSplitOptions.RemoveEmptyEntries);
            Debug.WriteLine($"length = {UserInputVector.Length}");

            if (UserInputVector.Length == 0)
            {
                Debug.WriteLine("Karappo!");
                Empty = true;
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
