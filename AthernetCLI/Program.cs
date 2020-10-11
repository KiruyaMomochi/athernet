using NAudio.Wave;
using System;
using System.Collections;
using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using System.Threading;
using System.IO;
using System.Linq;

namespace AthernetCLI
{
    class Program
    {
        static int PacketLength;

        static BitArray template;

        static void Main(string[] args)
        {
            WuPreambleBuilder PreambleBuilder = new WuPreambleBuilder(48000, 0.1f);

            var athernet = new Athernet.Physical()
            {
                Preamble = PreambleBuilder.Build(),
                PlayChannel = Athernet.Physical.Channel.Right,
                FrameBodyBits = 2000,
                Modulator = new DPSKModulator(48000, 8000, 1)
                {
                    BitDepth = 32
                }
            };

            var file = File.ReadAllText("data.txt");
            var arr = file.Split()[0].Select(x => x switch
            {
                '0' => false,
                '1' => true,
                _ => throw new NotImplementedException(),
            });
            template = new BitArray(arr.ToArray());

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
                Receive(athernet);
            }
        }

        static void Receive(Athernet.Physical athernet)
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
                    athernet.StopRecording();
                    Console.WriteLine("Stopped.");
                    Console.WriteLine(wrong);
                }
            };
            athernet.StartRecording();
            Console.WriteLine("Recording.");
            while (athernet.IsRecording)
            {
                Thread.Sleep(500);
            }
        }
    }
}
