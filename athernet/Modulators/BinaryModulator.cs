using athernet.Packets;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;

namespace athernet.Modulators
{
    abstract class BinaryModulator
    {
        protected SignalGenerator SignalGenertor()
        {
            return new SignalGenerator(SampleRate, 1)
            {
                Type = SignalGeneratorType.Sin,
                Frequency = Frequency[0],
                Gain = Gain[0]
            };
        }

        protected virtual void One(SignalGenerator signal)
        {
            signal.Frequency = Frequency[0];
            signal.Gain = Gain[0];
        }

        protected virtual void Zero(SignalGenerator signal)
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
            SignalGenerator carrier = SignalGenertor();

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
