using System;
using Athernet.MacLayer;

namespace MacPing
{
    class Program
    {
        static void Main(string[] args)
        {
            var node1 = new Mac(1, 500, 1, 1);
            var node2 = new Mac(2, 500, 3, 3);
            node1.StartReceive();
            node2.StartReceive();
            
            MacPing(node1, 2);
        }

    }
}
