using Athernet.SampleProviders;
using System.Collections;

namespace Athernet.Modulators
{
    public abstract class DifferentialBinaryModulator : BinaryModulator
    {
        protected int lastIdx = 0;

        protected override void One(in SineGenerator carrier)
        {
            lastIdx ^= 1;
            carrier.Frequency = Frequency[lastIdx];
            carrier.Gain = Gain[lastIdx];
            //Console.Write($"{lastIdx}");
        }

        protected override void Zero(in SineGenerator carrier)
        {
            //Console.Write($"{lastIdx}");
            // Do nothing
        }

        public DifferentialBinaryModulator(in int sampleRate, in double[] frequncy, in double[] gain) :
            base(sampleRate,
                frequncy,
                gain)
        { }

        public override float[] Modulate(BitArray frame)
        {
            int packetLength = (frame.Length + 1) * BitDepth;
            float[] samples = new float[packetLength];
            int nSample = 0;
            SineGenerator modulateCarrier = NewSineSignal();

            lastIdx = 0;
            modulateCarrier.Reset();

            nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            foreach (bool bit in frame)
            {
                if (bit)
                    One(modulateCarrier);
                else
                    Zero(modulateCarrier);
                nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            }

            //Console.WriteLine();

            return samples;
        }
    }
}
