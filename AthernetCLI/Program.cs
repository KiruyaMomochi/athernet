using NAudio.Wave;
using System;
using System.Collections;
using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using System.Threading;
using System.IO;
using System.Linq;
using Athernet.Physical;
using Athernet.Projects.Project1;

namespace AthernetCLI
{
    class Program
    {
        static int PacketLength;

        static byte[] template;

        static void Main(string[] args)
        {
            WuPreambleBuilder PreambleBuilder = new WuPreambleBuilder(48000, 0.1f);

            var modulator = new DpskModulator(48000, 8000, 1)
            {
                FrameBytes = 2000,
                BitDepth = 32
            };

            var athernet = new Physical(modulator)
            {
                Preamble = PreambleBuilder.Build(),
                PlayChannel = Channel.Right
            };

            var file = File.ReadAllText("data.txt");
            var arr = file.Split()[0].Select(x => x switch
            {
                '0' => false,
                '1' => true,
                _ => throw new NotImplementedException(),
            });
            template = Athernet.Utils.Maths.ToBytes(new BitArray(arr.ToArray()), Athernet.Utils.Maths.Endianness.LittleEndian).ToArray();

            PacketLength = template.Length;

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "play":
                        athernet.Play(template);
                        break;
                    case "record":
                        Receive(athernet);
                        break;
                    default:
                        Console.WriteLine("?");
                        break;
                };
            }
            else
            {
                athernet.Play(template);
            }
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        static void Receive(Physical athernet)
        {
            BitArray bitArray = new BitArray(PacketLength);
            int wrong = 0;
            int idx = 0;
            athernet.DataAvailable += (s, e) =>
            {
                int lcWrong = 0;
                for (int i = 0; i < e.Data.Length && idx < PacketLength; i++)
                {
                    if (e.Data[i] != template[idx])
                    {
                        Console.WriteLine($"Wrong at {i}");
                        lcWrong++;
                    }
                    idx++;
                }
                wrong += lcWrong;
                Console.WriteLine(lcWrong);

                if (idx == PacketLength)
                {
                    athernet.StopReceive();
                    Console.WriteLine("Stopped.");
                    Console.WriteLine(wrong);
                }
            };
            athernet.StartReceive();
            Console.WriteLine("Recording.");
            while (athernet.IsRecording)
            {
                Thread.Sleep(500);
            }
        }
    }
}
