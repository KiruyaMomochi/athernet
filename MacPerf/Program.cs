using Athernet.MacLayer;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MacPerf
{
    class Program
    {
        static int receivedBytes = 0;

        static void Main(string[] args)
        {
            // Console.ReadLine();
            Athernet.Utils.Audio.ListDevices();
            int payloadBytes = 200;

            // var node1 = new Mac(1, payloadBytes, 1, 1);
            var node2 = new Mac(2, payloadBytes, 0, 0);

            //node1.StartReceive();
            node2.StartReceive();
            //var ping = Task.Run(() => MacPing(node1, 2));
            // var perf1 = Task.Run(() => MacPerf(node1, 2, payloadBytes));
            // var perf2 = Task.Run(() => MacPerf(node2, 1, payloadBytes));
            // Task.WaitAll(perf1, perf2);
            // Task.WaitAll(ping, perf2);
            MacPerf(node2, 1, payloadBytes);
        }
        
        private static void MacPerf(Mac node, byte dest, int payloadBytes)
        {
            Task.Run(() => AddRandomPayload(node, dest, payloadBytes));

            while (true)
            {
                var t = Task.Delay(1000);
                receivedBytes = 0;
                t.Wait();
                Console.WriteLine($"Transmit to node {dest}: {receivedBytes * 8 / 1000f} kbps.");
            }

            // ReSharper disable once FunctionNeverReturns
        }

        static void AddRandomPayload(Mac node, byte dest, int payloadBytes)
        {
            while (true)
            {
                Thread.Sleep(300 * ((dest + 1) % 2));
                var payload = Athernet.Utils.Network.GeneratePayload(payloadBytes);
                node.AddPayload(dest, payload);
                receivedBytes += payloadBytes;
                Thread.Sleep(300 * (dest % 2));
            }
        }


        static void MacPing(Mac node, byte dest)
        {
            while (true)
            {
                Thread.Sleep(1000);
                Console.WriteLine(node.Ping(dest));
            }

            // ReSharper disable once FunctionNeverReturns
        }
    }
}