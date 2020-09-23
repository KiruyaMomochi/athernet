using System;
using System.Collections.Generic;
using System.Text;

namespace athernet.Preambles.PreambleBuilders
{
    class FunctionPreambleBuilder
    {
        private readonly float[] preamble;

        public FunctionPreambleBuilder(Func<float, float> buildFunction)
        {
            SampleRate = 48000;
            SampleCount = 48000;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction((float)i / SampleRate);
            }
        }

        public FunctionPreambleBuilder(Func<int, int, float> buildFunction)
        {
            SampleRate = 48000;
            SampleCount = 48000;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction(i, SampleRate);
            }
        }

        public FunctionPreambleBuilder(Func<int, int, int, float> buildFunction)
        {
            SampleRate = 48000;
            SampleCount = 48000;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction(i, SampleRate, SampleCount);
            }
        }

        public int SampleRate { get; set; }
        public int SampleCount { get; set; }

        // TODO: Change time to constant
        public float Time => SampleCount / SampleRate;
        public Preamble Preamble
        {
            get => new Preamble(preamble)
            {
                SampleRate = SampleRate
            };
        }
    }
}
