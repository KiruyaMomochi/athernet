using System.Collections.Generic;
using Athernet.SampleProviders;
using Athernet.Utils;

namespace Athernet.Modulators
{
    public abstract class DifferentialBinaryModulator : BinaryModulator
    {
        protected int LastIdx = 0;
        public override int FrameSamples => (FrameBits + 1) * BitDepth;

        protected override void One(in SineGenerator carrier)
        {
            LastIdx ^= 1;
            carrier.Frequency = Frequency[LastIdx];
            carrier.Gain = Gain[LastIdx];
        }

        protected override void Zero(in SineGenerator carrier)
        {
            // Do nothing
        }

        public new float[] Modulate(IEnumerable<byte> bytes)
        {
            LastIdx = 0;
            var samples = new float[FrameSamples];
            var nSample = 0;
            var modulateCarrier = NewSineSignal();

            nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            foreach (bool bit in Utils.Maths.ToBits(bytes, Maths.Endianness.LittleEndian))
            {
                if (bit)
                    One(modulateCarrier);
                else
                    Zero(modulateCarrier);
                nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            }

            return samples;
        }
    }
}
