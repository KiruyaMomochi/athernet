using NAudio.Wave;
using System;
using System.Collections;
using athernet.Modulators;
using athernet.Preambles.PreambleBuilders;
using System.Threading;

namespace athernet
{
    class Program
    {
        static int SampleRate = 48000;
        static int PacketLength = 1000;
        static int SamplesPerBit = 44;
        static DPSKModulator Modulator = new DPSKModulator(SampleRate, 8000, 1)
        {
            BitDepth = SamplesPerBit
        };
        static FunctionPreambleBuilder PreambleBuilder = new FunctionPreambleBuilder(PreambleFunc, 48000, 0.1f);
        static BitArray bitArray = new BitArray(PacketLength);

        static void Main(string[] args)
        {
            for (int i = 0, j = 0; i < 1000; i += j, j++)
            {
                bitArray.Set(i, true);
            }

            var athernet = new Athernet()
            {
                Preamble = PreambleBuilder.Build(),
                FrameBodyBits = 1000
            };
            athernet.Record();
            //athernet.Play(bitArray);
            //Console.WriteLine(WaveIn.GetCapabilities(0).ProductName);
            //athernet.Play(new BitArray(1000, false));
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
