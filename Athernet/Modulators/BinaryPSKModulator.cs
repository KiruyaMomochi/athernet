using Athernet.SampleProviders;
using System.Collections;

namespace Athernet.Modulators
{
    public class BinaryPSKModulator : BinaryModulator
    {
        public BinaryPSKModulator(int sampleRate, double frequncy, double gain) :
            base(sampleRate,
                new double[] { frequncy, frequncy },
                new double[] { gain, -gain })
        { }

        public override BitArray Demodulate(float[] samples)
        {
            int packetLength = samples.Length;
            int bitLength = packetLength / BitDepth;

            SineGenerator carrier = NewSineSignal();

            BitArray frame = new BitArray(bitLength);
            int nSample = 0;

            float[] carrierBuf = new float[BitDepth];

            for (int i = 0; i < bitLength; i++)
            {
                carrier.Read(carrierBuf, 0, BitDepth);
                float sum = 0;

                for (int j = 0; j < BitDepth; j++)
                {
                    sum += samples[nSample] * carrierBuf[j];
                    nSample++;
                }

                if (sum > 0)
                {
                    frame.Set(i, true);
                }
                else
                {
                    frame.Set(i, false);
                }
            }

            return frame;
        }
    }
}
