using athernet.SampleProviders;
using System;
using System.Collections;
using System.Linq;

namespace athernet.Modulators
{
    class DPSKModulator : DifferentialBinaryModulator
    {
        public DPSKModulator(int sampleRate, double frequncy, double gain) : base(sampleRate, frequncy, gain) { }

        private float findPhase(float[] signal)
        {
            float maxSum = 0;
            float maxPhase = 0;

            float[] carrierBuf = new float[BitDepth];

            for (float i = -(float) Math.PI / 2; i < Math.PI / 2; i += 0.1f)
            {
                float sum = 0;
                var carrier = SignalGenerator();
                carrier.PhaseShift = i;
                carrier.Read(carrierBuf, 0, BitDepth);
                for (int j = 0; j < BitDepth; j++)
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

        public override BitArray Demodulate(float[] samples)
        {
            int packetLength = samples.Length;
            int bitLength = packetLength / BitDepth - 1;

            SineGenerator carrier = SignalGenerator();
            BitArray frame = new BitArray(bitLength);
            int nSample = 0;

            var syncsamp = samples.Take(BitDepth).ToArray();
            carrier.PhaseShift = findPhase(syncsamp);
            Console.WriteLine($"Phase shift: {carrier.PhaseShift}");

            //SineGenerator signal = SignalGenerator();
            //signal.PhaseShift = findPhase(syncsamp);
            //var rawSamples = new float[SampleRate * SamplesPerBit];
            //signal.Read(rawSamples, 0, rawSamples.Length);
            //writeTempCsv(rawSamples, "carrier.csv");

            float[] carrierBuf = new float[BitDepth];

            carrier.Read(carrierBuf, 0, BitDepth);
            float sum = 0;
            for (int j = 0; j < BitDepth; j++)
            {
                sum += samples[nSample] * carrierBuf[j];
                nSample++;
            }
            bool lastData = sum > 0;

            for (int i = 0; i < bitLength; i++)
            {
                carrier.Read(carrierBuf, 0, BitDepth);

                sum = 0;
                for (int j = 0; j < BitDepth; j++)
                {
                    sum += samples[nSample] * carrierBuf[j];
                    nSample++;
                }

                frame.Set(i, (sum > 0) ^ (lastData));
                lastData = sum > 0;
            }

            return frame;
        }
    }
}
