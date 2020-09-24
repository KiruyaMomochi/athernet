using athernet.Packets;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace athernet.Modulators
{
    class DPSKModulator : DifferentialBinaryModulator
    {
        public DPSKModulator(int sampleRate, double frequncy, double gain) : base(sampleRate, frequncy, gain) { }

        public override BitArray Demodulate(Packet packet)
        {
            int packetLength = packet.Length;
            int bitLength = packetLength / SamplesPerBit - 1;

            SignalGenerator carrier = SignalGenertor();
            Zero(carrier);

            BitArray bitArray = new BitArray(bitLength);
            int nSample = 0;

            float[] carrierBuf = new float[SamplesPerBit];

            float sum = 0;
            for (int j = 0; j < SamplesPerBit; j++)
            {
                sum += packet.Samples[nSample] * carrierBuf[j];
                nSample++;
            }
            bool lastData = sum > 0;

            for (int i = 0; i < bitLength; i++)
            {
                carrier.Read(carrierBuf, 0, SamplesPerBit);

                sum = 0;
                for (int j = 0; j < SamplesPerBit; j++)
                {
                    sum += packet.Samples[nSample] * carrierBuf[j];
                    nSample++;
                }

                Console.WriteLine(sum > 0);

                bitArray.Set(i, (sum > 0) ^ (lastData));
                lastData = sum > 0;
            }

            return bitArray;
        }
    }
}
