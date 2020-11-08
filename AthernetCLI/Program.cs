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

            var o = DoTask(@"C:\Users\xtyzw\OneDrive - shanghaitech.edu.cn\CS120_ComputerNetworks\Project\Project 2\INPUT.bin", 300);
            var outFile = new FileInfo(@"C:\Users\xtyzw\OneDrive - shanghaitech.edu.cn\CS120_ComputerNetworks\Project\Project 2\OUTPUT.bin");

            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        private static byte[] DoTask(string fileName, int payloadBytes = 500)
        {
            var file = new FileInfo(fileName);
            var o = new byte[6250];
            var offset = 0;
            
            var node1 = new Mac(1, payloadBytes, 1, 1);
            var node2 = new Mac(2, payloadBytes, 3, 3);
            
            node1.StartReceive();
            node2.StartReceive();

            node2.DataAvailable += (sender, eventArgs) =>
            {
                var rem = 6250 - offset;
                Buffer.BlockCopy(eventArgs.Data, 0, o, offset, Math.Min(rem, eventArgs.Data.Length));
                offset += payloadBytes;
            };

            var buffer = new byte[6250];
            file.OpenRead().Read(buffer);

            for (int i = 0; i < buffer.Length; i += payloadBytes)
            {
                var s = buffer.Skip(i).Take(payloadBytes).ToArray();
                var b = s.Concat(new byte[payloadBytes - s.Length]).ToArray();

                node1.AddPayload(2, b);
            }
            
            Console.WriteLine($"Input == Output: {o.SequenceEqual(buffer)}");
            return o;
        }
    }
}