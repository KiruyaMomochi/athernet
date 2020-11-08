using System.Collections.Generic;
using System.Diagnostics;
using Athernet.SampleProviders;
using Athernet.Utils;

namespace Athernet.Modulators
{
    public abstract class DifferentialBinaryModulator : BinaryModulator
    {
        protected int LastIdx = 0;
        public override int FrameSamples (int frameBytes) => (frameBytes * 8 + 1) * BitDepth;
        
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

        public override float[] Modulate(byte[] bytes) => Modulate(bytes, true);

        public float[] Modulate(byte[] bytes, bool firstBit)
        {
            Trace.WriteLine($"P2. Modulate using {this.GetType().Name}.");
            
            LastIdx = 0;
            var samples = new float[FrameSamples(bytes.Length)];
            var nSample = 0;
            var modulateCarrier = NewSineSignal();


            if (firstBit)
                Zero(modulateCarrier);
            else
                One(modulateCarrier);
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
