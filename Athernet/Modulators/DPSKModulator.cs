using Athernet.SampleProviders;
using NWaves.Signals;
using System;
using System.Collections.Generic;

namespace Athernet.Modulators
{
    public sealed class DpskModulator : DifferentialBinaryModulator
    {
        public override double[] Frequency { get; protected set; }
        public override double[] Gain { get; protected set; }

        public DpskModulator(in int sampleRate, in double frequency, in double gain = 1)
        {
            SampleRate = sampleRate;
            Frequency = new[] {frequency, frequency};
            Gain = new[] {gain, -gain};
        }

        public override byte[] Demodulate(float[] samples)
        {
            // Athernet.Utils.Debug.PlaySamples(samples);
            var demodulateCarrier = NewSineSignal();
            var packetLength = samples.Length;
            var frameBits = packetLength / BitDepth - 1;

            samples = ApplyFiltersBeforeMultiply(samples);

            demodulateCarrier.Reset(FindPhase(samples, demodulateCarrier));
            // demodulateCarrier.Reset();
            var sums = CalcSum(samples, frameBits, demodulateCarrier);

            sums = ApplyFiltersAfterMultiply(sums);

            return CalcFrame(sums);
        }

        private float FindPhase(in float[] signal, SineGenerator carrier)
        {
            float maxSum = 0, maxPhase = 0;
            float[] carrierBuf = new float[BitDepth];

            for (float i = -(float) Math.PI / 2; i < Math.PI / 2; i += 0.05f)
            {
                carrier.Reset(i);
                carrier.Read(carrierBuf, 0, BitDepth);

                float sum = 0;
                for (int j = 0; j < BitDepth; j++)
                    sum += signal[j] * carrierBuf[j];

                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxPhase = i;
                }
            }

            return maxPhase;
        }

        private float[] CalcSum(in float[] samples, int frameLength, SineGenerator carrier)
        {
            var sums = new float[samples.Length];
            float[] carrierBuf = new float[BitDepth];
            int nSample = 0;

            for (int i = 0; i < frameLength + 1; i++)
            {
                carrier.Read(carrierBuf, 0, BitDepth);
                for (int j = 0; j < BitDepth; j++)
                {
                    sums[nSample] = samples[nSample] * carrierBuf[j];
                    nSample++;
                }
            }

            return sums;
        }

        private byte[] CalcFrame(in IEnumerable<float> sums)
        {
            var bytes = new byte[FrameBytes];
            var nByte = 0;
            var nBit = 0;
            var lastData = false;

            var e = sums.GetEnumerator();

            GetNextBit();

            for (var i = 0; i < FrameBytes; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    PushNextBit(GetNextBit());
                }
            }

            return bytes;

            bool GetNextBit()
            {
                float sum = 0;
                for (var k = 0; k < BitDepth; k++)
                {
                    e.MoveNext();
                    sum += e.Current;
                }

                var r = (sum > 0) ^ (lastData);
                lastData = sum > 0;
                return r;
            }

            void PushNextBit(bool bit)
            {
                if (bit)
                    bytes[nByte] |= Utils.Maths.LittleByteMask[nBit];

                nBit++;
                
                if (nBit != 8)
                    return;
                nBit = 0;
                nByte++;
            }
        }

        private float[] ApplyFiltersAfterMultiply(in float[] samples)
        {
            var onePole = new NWaves.Filters.OnePole.LowPassFilter(Frequency[0] * 1.5);
            var signal = new DiscreteSignal(SampleRate, samples);
            return onePole.ApplyTo(signal).Samples;
        }

        private float[] ApplyFiltersBeforeMultiply(in float[] samples)
        {
            var chebyshevI = new NWaves.Filters.ChebyshevI.HighPassFilter(Frequency[0], 1);
            var signal = new DiscreteSignal(SampleRate, samples);
            return chebyshevI.ApplyTo(signal).Samples;
        }
    }
}