using NAudio.Wave;
using System;
using System.Collections;
using System.Diagnostics;
using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using System.Threading;
using System.IO;
using System.Linq;
using Athernet.MacLayer;
using Athernet.PhysicalLayer;
using NAudio.Wave.SampleProviders;

namespace AthernetCLI
{
    class Program
    {
        private static byte[] RandomByteBuilder(int length, int num)
        {
            byte[] res = new byte[length];
            for (int i = 0; i < length; i++)
            {
                res[i] = (byte)(i+num);
            }

            return res;
        }
        
        private static void Main(string[] args)
        {
            ListUsingDevice();

            // Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var node1 = new Mac(1, 100, 0, 0);
            var node2 = new Mac(2, 100, 3, 3);

            node2.StartReceive();
            node1.StartReceive();
            node2.DataAvailable += (sender, eventArgs) => Console.WriteLine(eventArgs.Data[56]);
            node1.DataAvailable += (sender, eventArgs) => Console.WriteLine(eventArgs.Data[56]);
            
            for (int i = 0; i < 6250 / node1.PayloadBytes; i++)
            {
                node1.AddData(2, RandomByteBuilder(node1.PayloadBytes, i+41));
            }
            // Thread.Sleep(10000);

            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        static void ListUsingDevice()
        {
            for (int i = -1; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                Console.WriteLine($"WaveIn device #{i}\n\t {caps.ProductName}: {caps.ProductGuid}");
            }

            Console.WriteLine();

            for (int i = -1; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                Console.WriteLine($"WaveOut device #{i}\n\t {caps.ProductName}: {caps.ProductGuid}");
            }
        }
    }
}