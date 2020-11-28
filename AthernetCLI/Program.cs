using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Athernet.MacLayer;
using Athernet.PhysicalLayer;

namespace AthernetCLI
{
    class Program
    {
        private static EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();

            //Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var watch = Stopwatch.StartNew();

            // ReSharper disable twice StringLiteralTypo
            DoTask();
            ewh.WaitOne();

            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        private static void DoTask(int payloadBytes = 4092)
        {
            var node1 = new Physical(2, 1, payloadBytes);
            var node2 = new Physical(1, 2, payloadBytes);

            node2.StartReceive();

            var buffer = new byte[payloadBytes];
            var rand = new Random();
            rand.NextBytes(buffer);
            //Array.Fill<byte>(buffer, 255);
            
            node2.DataAvailable += (sender, args) =>
            {
                Console.WriteLine($"Data: {BitConverter.ToString(args.Data)}, CRC: {args.CrcResult}, Validate: {args.Data.SequenceEqual(buffer)}");
                ewh.Set();
            };
            //file.OpenRead().Read(buffer);
            node1.AddPayload(buffer);
        }
    }
}