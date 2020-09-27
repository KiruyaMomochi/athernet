using athernet.SampleProviders;
using NWaves.Signals;
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
            int nSample;

            var syncsamp = samples.Take(BitDepth).ToArray();
            carrier.PhaseShift = findPhase(syncsamp);
            Console.WriteLine($"Phase shift: {carrier.PhaseShift}");

            //Utils.Debug.writeTempCsv(samples, "samples.csv");

            var sums = new float[samples.Length];

            //samples = ApplyFiltersBeforeMultiply(samples);

            float[] carrierBuf = new float[BitDepth];
            nSample = 0;
            for (int i = 0; i < bitLength + 1; i++)
            {
                carrier.Read(carrierBuf, 0, BitDepth);
                for (int j = 0; j < BitDepth; j++)
                {
                    sums[nSample] = samples[nSample] * carrierBuf[j];
                    nSample++;
                }
            }

            //Utils.Debug.writeTempCsv(sums, "sums.csv");

            //sums = ApplyFiltersAfterMultiply(sums);

            float sum = 0;
            nSample = 0;
            for (int j = 0; j < BitDepth; j++)
            {
                sum += sums[nSample];
                nSample++;
            }
            bool lastData = sum > 0;

            for (int i = 0; i < bitLength; i++)
            {
                sum = 0;
                for (int j = 0; j < BitDepth; j++)
                {
                    sum += sums[nSample];
                    nSample++;
                }

                frame.Set(i, (sum > 0) ^ (lastData));
                lastData = sum > 0;
            }

            return frame;
        }

        private float[] ApplyFiltersAfterMultiply(float[] samples)
        {
            //return samples;
            var deltaPass = 0.96;
            var deltaStop = 0.04;

            var ripplePassDb = NWaves.Utils.Scale.ToDecibel(1 / deltaPass);
            var attenuateDb = NWaves.Utils.Scale.ToDecibel(1 / deltaStop);

            var ellip = new NWaves.Filters.Elliptic.LowPassFilter(Frequency[0]*2, 1, ripplePassDb, attenuateDb);

            //var biquad = new NWaves.Filters.BiQuad.LowPassFilter(Frequency[0] * 2);

            var signal = new DiscreteSignal(SampleRate, samples);
            return ellip.ApplyTo(signal).Samples;
        }

        private float[] ApplyFiltersBeforeMultiply(float[] samples)
        {
            //return samples;
            var cheb1 = new NWaves.Filters.ChebyshevI.HighPassFilter(Frequency[0], 1);

            var signal = new DiscreteSignal(SampleRate, samples);
            return cheb1.ApplyTo(signal).Samples;
        }
    }
}
