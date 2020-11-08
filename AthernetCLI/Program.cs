using NAudio.Wave;
using System;
using System.Collections;
using System.Diagnostics;
using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using System.Threading;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
                res[i] = (byte) (i + num);
            }

            return res;
        }

        private static void Main(string[] args)
        {
            Athernet.Utils.Audio.ListDevices();

            // Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            var watch = System.Diagnostics.Stopwatch.StartNew();

            DoTask();

            // var node1 = new Mac(1, 100, 1, 1);
            //
            // var modulator = new DpskModulator(48000, 8000)
            // {
            //     BitDepth = 3
            // };
            // var fuck = new ReceiverRx(modulator, 3)
            // {
            //     Preamble = new WuPreambleBuilder(modulator.SampleRate, 0.015f).Build(),
            //     PayloadBytes = 100
            // };
            // fuck.DataAvailable += (sender, eventArgs) => Console.WriteLine(eventArgs.Data);
            // fuck.StartReceive();
            // Task.Run(() =>
            // {
            //     for (int i = 0; i < 6250 / node1.PayloadBytes; i++)
            //     {
            //         node1.AddPayload(2, RandomByteBuilder(node1.PayloadBytes, i + 41));
            //     }
            // });
            //
            // Thread.Sleep(8000);
            // fuck.StopReceive();
            // Console.WriteLine("---Stopped!---");
            // Thread.Sleep(2000);

            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        private static void DoTask()
        {
            var node1 = new Mac(1, 500, 1, 1);
            var node2 = new Mac(2, 500, 3, 3);
            Console.WriteLine(node1.AckTimeout);

            node1.StartReceive();
            node2.StartReceive();
            node1.DataAvailable += (sender, eventArgs) => Console.WriteLine(eventArgs.Data[0]);
            node2.DataAvailable += (sender, eventArgs) => Console.WriteLine(eventArgs.Data[0]);
            
            // for (int i = 0; i < 6250 / node1.PayloadBytes; i++)
            // {
            //     node1.AddPayload(2, RandomByteBuilder(node1.PayloadBytes, i + 41));
            // }
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(node1.Ping(2));
                Thread.Sleep(200);
            }
        }
    }
}