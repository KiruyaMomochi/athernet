using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual int FrameSamples (int frameBytes) => frameBytes * 8 * BitDepth;

        public virtual float[] Modulate(byte[] bytes)
        {
            var samples = new float[FrameSamples(bytes.Length)];
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

            // Athernet.Utils.Debug.PlaySamples(samples, Guid.Parse("617edf03-aaa7-4b5f-a862-1ab9fc8cc617"));
            return samples;
        }

        public abstract byte[] Demodulate(float[] frame, int frameBytes);

        internal virtual SineGenerator NewSineSignal()
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