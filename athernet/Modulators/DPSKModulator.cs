using athernet.Packets;
using athernet.SampleProviders;
using athernet.Utils;
using NAudio.Wave.SampleProviders;
using NWaves.Signals;
using NWaves.Signals.Builders;
using NWaves.Transforms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace athernet.Modulators
{
    class DPSKModulator : DifferentialBinaryModulator
    {
        public DPSKModulator(int sampleRate, double frequncy, double gain) : base(sampleRate, frequncy, gain) { }

        private float findPhase(float[] signal)
        {
            float maxSum = 0;
            float maxPhase = 0;

            float[] carrierBuf = new float[SamplesPerBit];

            for (float i = -(float) Math.PI / 2; i < Math.PI / 2; i += 0.1f)
            {
                float sum = 0;
                var carrier = SignalGenerator();
                carrier.PhaseShift = i;
                carrier.Read(carrierBuf, 0, SamplesPerBit);
                for (int j = 0; j < SamplesPerBit; j++)
                {
                    sum += signal[j] * carrierBuf[j];
                }
                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxPhase = i;
                }
            }
            return maxPhase;
        }

        //static private void writeTempCsv(float[] buffer, string fileName)
        //{
        //    var path = Path.Combine(Path.GetTempPath(), fileName);
        //    File.WriteAllText(path, String.Join(", ", buffer));
        //}

        public override BitArray Demodulate(Packet packet)
        {
            int packetLength = packet.Length;
            int bitLength = packetLength / SamplesPerBit - 1;

            SineGenerator carrier = SignalGenerator();
            BitArray bitArray = new BitArray(bitLength);
            int nSample = 0;

            var syncsamp = packet.Samples.Take(SamplesPerBit).ToArray();
            carrier.PhaseShift = findPhase(syncsamp);
            Console.WriteLine($"Phase shift: {carrier.PhaseShift}");

            //SineGenerator signal = SignalGenerator();
            //signal.PhaseShift = findPhase(syncsamp);
            //var rawSamples = new float[SampleRate * SamplesPerBit];
            //signal.Read(rawSamples, 0, rawSamples.Length);
            //writeTempCsv(rawSamples, "carrier.csv");

            float[] carrierBuf = new float[SamplesPerBit];

            carrier.Read(carrierBuf, 0, SamplesPerBit);
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

                bitArray.Set(i, (sum > 0) ^ (lastData));
                lastData = sum > 0;
            }

            return bitArray;
        }
    }
}
