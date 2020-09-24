using athernet.Packets;
using athernet.SampleProviders;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace athernet.Modulators
{
    class BinaryPSKModulator : BinaryModulator
    {
        public BinaryPSKModulator(int sampleRate, double frequncy, double gain) :
            base(sampleRate,
                new double[] { frequncy, frequncy }, 
                new double[] { gain, -gain })
        { }

        public override BitArray Demodulate(Packet packet)
        {
            int packetLength = packet.Length;
            int bitLength = packetLength / SamplesPerBit;

            SineGenerator carrier = SignalGenerator();

            BitArray bitArray = new BitArray(bitLength);
            int nSample = 0;

            float[] carrierBuf = new float[SamplesPerBit];

            for (int i = 0; i < bitLength; i++)
            {
                carrier.Read(carrierBuf, 0, SamplesPerBit);
                float sum = 0;

                for (int j = 0; j < SamplesPerBit; j++)
                {
                    sum += packet.Samples[nSample] * carrierBuf[j];
                    nSample++;
                }

                Console.WriteLine(sum);

                if (sum > 0)
                {
                    bitArray.Set(i, true);
                }
                else
                {
                    bitArray.Set(i, false);
                }
            }

            return bitArray;
        }
    }
}
