using System;
using System.Collections.Generic;
using System.Text;

namespace Athernet.Preambles.PreambleBuilders
{
    public class FunctionPreambleBuilder
    {
        private readonly float[] preamble;

        public FunctionPreambleBuilder(Func<float, float> buildFunction, int sampleRate, float time)
        {
            SampleRate = sampleRate;
            Time = time;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction((float)i / SampleRate);
            }
        }

        public FunctionPreambleBuilder(Func<int, int, float> buildFunction, int sampleRate, float time)
        {
            SampleRate = sampleRate;
            Time = time;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction(i, SampleRate);
            }
        }

        public FunctionPreambleBuilder(Func<int, int, int, float> buildFunction, int sampleRate, float time)
        {
            SampleRate = sampleRate;
            Time = time;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction(i, SampleRate, SampleCount);
            }
        }

        public int SampleRate { get; set; }
        public float Time { get; set; }

        public int SampleCount => (int) (SampleRate * Time);

        public float[] Build()
        {
            return (float[])preamble.Clone();
        }
    }
}
