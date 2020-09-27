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

        static void Main(string[] args)
        {
            int PacketLength = 10000;
            BitArray bitArray = new BitArray(PacketLength);
            FunctionPreambleBuilder PreambleBuilder = new FunctionPreambleBuilder(PreambleFunc, 48000, 0.1f);
            for (int i = 0, j = 0; i < PacketLength; i += j, j++)
            {
                bitArray.Set(i, true);
            }

            var athernet = new Athernet()
            {
                Preamble = PreambleBuilder.Build(),
                FrameBodyBits = 1000
            };
            athernet.DataAvailable += Athernet_DataAvailable;
            athernet.StartRecording();
        }

        private static void Athernet_DataAvailable(object sender, BitArray e)
        {
            Utils.Debug.PrintResult(e);
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
