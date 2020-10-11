using Athernet.SampleProviders;
using System.Collections;

namespace Athernet.Modulators
{
    public abstract class BinaryModulator : IModulator
    {
        public double[] Frequency { get; set; }
        public double[] Gain { get; set; }
        public int BitDepth { get; set; } = 44;
        public int SampleRate { get; set; }

        public BinaryModulator(in int sampleRate, in double[] frequncy, in double[] gain)
        {
            Frequency = frequncy;
            Gain = gain;
            SampleRate = sampleRate;
        }

        public virtual float[] Modulate(BitArray frame)
        {
            int packetLength = frame.Length * BitDepth;
            float[] samples = new float[packetLength];
            int nSample = 0;
            SineGenerator modulateCarrier = NewSineSignal();

            foreach (bool bit in frame)
            {
                if (bit)
                    One(modulateCarrier);
                else
                    Zero(modulateCarrier);
                nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            }

            return samples;
        }

        public abstract BitArray Demodulate(float[] frame);

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
