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
            Time = 1;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction((float)i / SampleRate);
            }
        }

        public FunctionPreambleBuilder(Func<int, int, float> buildFunction)
        {
            SampleRate = 48000;
            Time = 1;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction(i, SampleRate);
            }
        }

        public FunctionPreambleBuilder(Func<int, int, int, float> buildFunction)
        {
            SampleRate = 48000;
            Time = 1;
            preamble = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                preamble[i] = buildFunction(i, SampleRate, SampleCount);
            }
        }

        public int SampleRate { get; set; }
        public float Time { get; set; }

        public int SampleCount => (int) (Time * SampleRate);

        public Preamble Build()
        {
            return new Preamble((float[]) preamble.Clone())
            {
                SampleRate = SampleRate
            };
        }
    }
}
