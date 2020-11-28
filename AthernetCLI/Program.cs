using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Athernet.MacLayer;
using Athernet.PhysicalLayer;

namespace AthernetCLI
{
    class Program
    {
        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();

            // Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var watch = Stopwatch.StartNew();

            // ReSharper disable twice StringLiteralTypo
            var o = DoTask(@"C:\Users\xtyzw\OneDrive - shanghaitech.edu.cn\CS120_ComputerNetworks\Project\Project 2\INPUT.bin");
            var outFile = new FileInfo(@"C:\Users\xtyzw\OneDrive - shanghaitech.edu.cn\CS120_ComputerNetworks\Project\Project 2\OUTPUT.bin");

            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        private static byte[] DoTask(string fileName, int payloadBytes = 16)
        {
            var file = new FileInfo(fileName);

            var node1 = new Physical(2, 1, payloadBytes);
            var node2 = new Physical(1, 2, payloadBytes);

            node2.StartReceive();

            var buffer = new byte[payloadBytes];
            //Array.Fill<byte>(buffer, 255);

            //file.OpenRead().Read(buffer);
            node1.AddPayload(buffer);
            Thread.Sleep(1000000000);
            return null;
        }
    }
}