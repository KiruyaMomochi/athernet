using System;
using System.Net;
using Athernet.AppLayer.FTPClient;
using Athernet.MacLayer;
using Athernet.Nat;

namespace ToyNet
{
    class Program
    {
        private static void Main(string[] args)
        {
            var node2 = new Mac(0, 4, 2, 2048);
            var nat = new Nat(1, node2);
            nat.Listen();
            
            Console.Write("Connecting Host Address:");
            String Address = Console.ReadLine();
            var CLI = new UserInterface(Address);
            //var CLI = new UserInterface("140.110.96.68");
            CLI.Shell();
        }
    }
}
