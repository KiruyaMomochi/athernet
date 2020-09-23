using athernet.Packets;
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

        protected override void One(SignalGenerator signal)
        {
            lastIdx ^= 1;
            signal.Frequency = Frequency[lastIdx];
            signal.Gain = Gain[lastIdx];
        }

        protected override void Zero(SignalGenerator signal)
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
            SignalGenerator carrier = SignalGenertor();

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
