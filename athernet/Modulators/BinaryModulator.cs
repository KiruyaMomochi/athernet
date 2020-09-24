using athernet.Packets;
using athernet.SampleProviders;
using NAudio.Wave.SampleProviders;
using System;
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
            SamplesPerBit = 44;
        }

        public double[] Frequency { get; set; }
        public double[] Gain { get; set; }
        public int SamplesPerBit { get; set; }

        public int SampleRate;

        // Offline Methods
        public virtual Packet Modulate(BitArray bitArray)
        {
            int packetLength = bitArray.Length * SamplesPerBit;
            Packet packet = new Packet(SampleRate, packetLength);
            int nSample = 0;
            SineGenerator carrier = SignalGenerator();

            foreach (bool bit in bitArray)
            {
                if (bit)
                    One(carrier);
                else
                    Zero(carrier);
                nSample += carrier.Read(packet.Samples, nSample, SamplesPerBit);
            }

            return packet;
        }

        public abstract BitArray Demodulate(Packet packet);
    }
}
