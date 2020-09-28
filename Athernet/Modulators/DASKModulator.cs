using Athernet.SampleProviders;
using NWaves.Signals;
using System;
using System.Collections;

namespace Athernet.Modulators
{
    public class DASKModulator : DifferentialBinaryModulator
    {
        private readonly SineGenerator demodulateCarrier;
        public DASKModulator(int sampleRate, double frequncy, double gain) :
            base(sampleRate, new[] { frequncy, frequncy }, new[] { gain, gain / 10 })
        {
            demodulateCarrier = NewSineSignal();
        }

        public override BitArray Demodulate(float[] samples)
        {
            int packetLength = samples.Length;
            int frameLength = packetLength / BitDepth - 1;

            samples = ApplyFiltersBeforeMultiply(samples);

            var (sum, phase) = FindPhase(samples);
            demodulateCarrier.Reset(phase);
            var sums = CalcSum(samples, frameLength);

            sums = ApplyFiltersAfterMultiply(sums);

            return CalcFrame(sums, frameLength, sum);
        }

        private (float, float) FindPhase(float[] signal)
        {
            float maxSum = 0, maxPhase = 0;
            float[] carrierBuf = new float[BitDepth];

            for (float i = -(float)Math.PI / 2; i < Math.PI / 2; i += 0.1f)
            {
                demodulateCarrier.Reset(i);
                demodulateCarrier.Read(carrierBuf, 0, BitDepth);

                float sum = 0;
                for (int j = 0; j < BitDepth; j++)
                    sum += signal[j] * carrierBuf[j];

                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxPhase = i;
                }
            }

            return (maxSum, maxPhase);
        }

        private float[] CalcSum(float[] samples, int frameLength)
        {
            var sums = new float[samples.Length];
            float[] carrierBuf = new float[BitDepth];
            int nSample = 0;

            for (int i = 0; i < frameLength + 1; i++)
            {
                demodulateCarrier.Read(carrierBuf, 0, BitDepth);
                for (int j = 0; j < BitDepth; j++)
                {
                    sums[nSample] = samples[nSample] * carrierBuf[j];
                    nSample++;
                }
            }
            return sums;
        }

        private BitArray CalcFrame(float[] sums, int frameLength, float sumZero)
        {
            var thres = 1;
            BitArray frame = new BitArray(frameLength);
            int nSample = 0;
            float sum = 0;

            for (int j = 0; j < BitDepth; j++)
                sum += sums[nSample++];

            bool lastData = sum > thres;
            Console.Write($"{sum} ");

            for (int i = 0; i < frameLength; i++)
            {
                sum = 0;
                for (int j = 0; j < BitDepth; j++)
                    sum += sums[nSample++];

                frame.Set(i, (sum > thres) ^ (lastData));
                lastData = sum > thres;
                Console.Write($"{sum} ");
            }
            Console.WriteLine();

            return frame;
        }

        protected float[] ApplyFiltersAfterMultiply(float[] samples)
        {
            var onepole = new NWaves.Filters.OnePole.LowPassFilter(Frequency[0]);
            var signal = new DiscreteSignal(SampleRate, samples);
            return onepole.ApplyTo(signal).Samples;
        }

        protected float[] ApplyFiltersBeforeMultiply(float[] samples)
        {
            var cheb1 = new NWaves.Filters.ChebyshevI.HighPassFilter(Frequency[0], 1);
            var signal = new DiscreteSignal(SampleRate, samples);
            return cheb1.ApplyTo(signal).Samples;
        }
    }
}
