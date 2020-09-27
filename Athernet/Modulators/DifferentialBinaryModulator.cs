using Athernet.SampleProviders;
using System.Collections;

namespace Athernet.Modulators
{
    public abstract class DifferentialBinaryModulator : BinaryModulator
    {
        private int lastIdx = 0;

        protected override void One(SineGenerator signal)
        {
            lastIdx ^= 1;
            signal.Frequency = Frequency[lastIdx];
            signal.Gain = Gain[lastIdx];
        }

        protected override void Zero(SineGenerator signal)
        {
            // Do nothing
        }

        public DifferentialBinaryModulator(int sampleRate, double frequncy, double gain) :
            base(sampleRate,
                new double[] { frequncy, frequncy },
                new double[] { gain, -gain })
        { }

        public override float[] Modulate(BitArray frame)
        {
            int packetLength = (frame.Length + 1) * BitDepth;
            float[] samples = new float[packetLength];
            int nSample = 0;
            SineGenerator carrier = SignalGenerator();

            nSample += carrier.Read(samples, nSample, BitDepth);
            foreach (bool bit in frame)
            {
                if (bit)
                    One(carrier);
                else
                    Zero(carrier);
                nSample += carrier.Read(samples, nSample, BitDepth);
            }

            return samples;
        }
    }
}
