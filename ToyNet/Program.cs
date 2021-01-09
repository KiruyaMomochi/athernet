using System;
using System.Net;
using Athernet.AppLayer.FTPClient;


namespace ToyNet
{
    internal class Program
    {

        private static void Main(string[] args)
        {
            Console.Write("Connecting Host Address:");
            String Address = Console.ReadLine();
            var CLI = new UserInterface(Address);
            //var CLI = new UserInterface("140.110.96.68");
            CLI.Shell();
        }
    }
}
