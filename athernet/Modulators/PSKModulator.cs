using athernet.Packets;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;

namespace athernet.Modulators
{
    class PSKModulator
    {
        private SignalGenerator Signal()
        {
            return new SignalGenerator(SampleRate, 1)
            {
                Type = SignalGeneratorType.Sin,
                Frequency = Frequency[0],
                Gain = Gain[0]
            };
        }

        private void One(SignalGenerator signal)
        {
            signal.Frequency = Frequency[0];
            signal.Gain = Gain[0];
        }

        private void Zero(SignalGenerator signal)
        {
            signal.Frequency = Frequency[1];
            signal.Gain = Gain[1];
        }

        public PSKModulator(int sampleRate)
        {
            Frequency = new double[] { 8000, 8000 };
            Gain = new double[] { 1.0, -1.0 };
            SampleRate = sampleRate;
            SamplesPerBit = 1000;
        }

        public double[] Frequency { get; set; }
        public double[] Gain { get; set; }
        public int SamplesPerBit { get; set; }

        public int SampleRate;

        // Offline Methods
        public Packet Modulate(BitArray bitArray)
        {
            int packetLength = bitArray.Length * SamplesPerBit;
            Packet packet = new Packet(SampleRate, packetLength);
            int nSample = 0;
            SignalGenerator carrier = Signal();

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

        public BitArray Demodulate(Packet packet)
        {
            int packetLength = packet.Length;
            int bitLength = packetLength / SamplesPerBit;

            SignalGenerator carrier = Signal();
            Zero(carrier);

            BitArray bitArray = new BitArray(bitLength);
            int nSample = 0;

            float[] carrierBuf = new float[SamplesPerBit];

            for (int i = 0; i < bitLength; i++)
            {
                carrier.Read(carrierBuf, 0, SamplesPerBit);
                float sum = 0;

                for (int j = 0; j < SamplesPerBit; j++)
                {
                    //Console.WriteLine($"{packet.Samples[nSample]} {carrierBuf[j]}");
                    sum += packet.Samples[nSample] * carrierBuf[j];
                    nSample++;
                }

                if (sum > 0)
                {
                    bitArray.Set(i, true);
                }
                else
                {
                    bitArray.Set(i, false);
                }

                Console.WriteLine($"{i}: {sum} {bitArray.Get(i)}");
            }

            return bitArray;
        }
    }
}
