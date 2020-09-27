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
        static int PacketLength = 15000;

        static BitArray template;

        static void Main(string[] args)
        {
            FunctionPreambleBuilder PreambleBuilder = new FunctionPreambleBuilder(PreambleFunc, 48000, 0.1f);
            var athernet = new Athernet.Athernet(48000, 30, PreambleBuilder.Build())
            {
                FrameBodyBits = 2000
            };

            var file = File.ReadAllText("data.txt");
            var arr = file.Split()[0].Select(x => x switch
            {
                '0' => false,
                '1' => true,
                _ => throw new NotImplementedException(),
            });
            template = new BitArray(arr.ToArray());

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
                Receive(athernet);
            }
        }

        static void Receive(Athernet.Athernet athernet)
        {
            BitArray bitArray = new BitArray(PacketLength);
            int wrong = 0;
            int idx = 0;
            athernet.DataAvailable += (s, e) =>
            {
                int lcWrong = 0;
                for (int i = 0; i < e.Length && idx < PacketLength; i++)
                {
                    if (e[i] != template[idx])
                    {
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

        static float PreambleFunc(int nSample, int sampleRate, int sampleCount)
        {
            float totalTime = (float)sampleCount / sampleRate;
            float time = (float)nSample / sampleRate;
            float frequencyMin = 4000;
            float frequencyMax = 9000;

            float a = (frequencyMax - frequencyMin) * 2 / totalTime;
            float soundVoice = 1;

            if (nSample < sampleCount / 2)
            {
                float phase = time * time * a * (float)Math.PI + time * frequencyMin * (float)Math.PI * 2;
                float anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
            else
            {
                float phase = -time * time * a * (float)Math.PI + time * frequencyMax * 2 * (float)Math.PI;
                float anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
        }
    }
}
