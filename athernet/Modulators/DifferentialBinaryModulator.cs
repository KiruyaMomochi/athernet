using athernet.Packets;
using athernet.SampleProviders;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace athernet.Modulators
{
    abstract class DifferentialBinaryModulator: BinaryModulator
    {
        private int lastIdx = 0;

        protected override void One(SineGenerator signal)
        {
            lastIdx ^= 1;
            signal.Frequency = Frequency[lastIdx];
            signal.Gain = Gain[lastIdx];
        }

        protected override void Zero(SineGenerator signal)
        {
            // Do nothing
        }

        public DifferentialBinaryModulator(int sampleRate, double frequncy, double gain) :
            base(sampleRate,
                new double[] { frequncy, frequncy },
                new double[] { gain, -gain })
        { }

        public override Packet Modulate(BitArray bitArray)
        {
            int packetLength = (bitArray.Length + 1) * SamplesPerBit;
            Packet packet = new Packet(SampleRate, packetLength);
            int nSample = 0;
            SineGenerator carrier = SignalGenerator();

            nSample += carrier.Read(packet.Samples, nSample, SamplesPerBit);
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
    }
}
