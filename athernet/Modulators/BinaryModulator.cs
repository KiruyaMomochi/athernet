using athernet.SampleProviders;
using System.Collections;

namespace athernet.Modulators
{
    abstract class BinaryModulator
    {
        protected SineGenerator SignalGenerator()
        {
            return new SineGenerator(SampleRate, 1)
            {
                Frequency = Frequency[0],
                Gain = Gain[0]
            };
        }

        protected virtual void One(SineGenerator signal)
        {
            signal.Frequency = Frequency[0];
            signal.Gain = Gain[0];
        }

        protected virtual void Zero(SineGenerator signal)
        {
            signal.Frequency = Frequency[1];
            signal.Gain = Gain[1];
        }

        public BinaryModulator(int sampleRate, double[] frequncy, double[] gain)
        {
            Frequency = frequncy;
            Gain = gain;
            SampleRate = sampleRate;
            BitDepth = 44;
        }

        public double[] Frequency { get; set; }
        public double[] Gain { get; set; }
        public int BitDepth { get; set; }
        public int SampleRate { get; set; }

        public virtual float[] Modulate(BitArray frame)
        {
            int packetLength = frame.Length * BitDepth;
            float[] samples = new float[packetLength];
            int nSample = 0;
            SineGenerator carrier = SignalGenerator();

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

        public abstract BitArray Demodulate(float[] frame);
    }
}
