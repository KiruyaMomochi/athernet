using System.Collections.Generic;
using Athernet.SampleProviders;
using Athernet.Utils;

namespace Athernet.Modulators
{
    public abstract class BinaryModulator : IModulator
    {
        public virtual double[] Frequency { get; protected set; }
        public virtual double[] Gain { get; protected set; }
        public virtual int BitDepth { get; set; } = 44;
        public virtual int SampleRate { get; set; }
        public virtual int FrameBytes { get; set; }

        public virtual int FrameBits => FrameBytes * 8;
        public virtual int FrameSamples => FrameBits * BitDepth;

        public float[] Modulate(IEnumerable<byte> bytes)
        {
            var samples = new float[FrameSamples];
            var nSample = 0;
            var modulateCarrier = NewSineSignal();

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

        public abstract byte[] Demodulate(float[] frame);

        protected virtual SineGenerator NewSineSignal()
            => new SineGenerator(SampleRate, 1)
            {
                Frequency = Frequency[0],
                Gain = Gain[0]
            };

        protected virtual void One(in SineGenerator carrier)
        {
            carrier.Frequency = Frequency[0];
            carrier.Gain = Gain[0];
        }

        protected virtual void Zero(in SineGenerator carrier)
        {
            carrier.Frequency = Frequency[1];
            carrier.Gain = Gain[1];
        }
    }
}