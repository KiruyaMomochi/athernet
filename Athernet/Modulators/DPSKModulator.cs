using Athernet.SampleProviders;
using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public override byte[] Demodulate(float[] samples, int frameBytes)
        {
            Trace.WriteLine($"R3. Demodulate using {this.GetType().Name}.");
            
            // Athernet.Utils.Debug.PlaySamples(samples);
            var demodulateCarrier = NewSineSignal();
            var frameBits = frameBytes * 8;
            
            demodulateCarrier.Reset(FindPhase(samples, demodulateCarrier));

            samples = ApplyFiltersBeforeMultiply(samples);
            // demodulateCarrier.Reset();
            // var sums = CalcSum(samples, frameBits, demodulateCarrier);

            // sums = ApplyFiltersAfterMultiply(sums);

            var carrierSamples = new float[samples.Length];
            demodulateCarrier.Read(carrierSamples, 0, carrierSamples.Length);

            return CalcFrame(samples, carrierSamples, frameBytes);
        }

        private float FindPhase(in float[] signal, SineGenerator carrier)
        {
            float maxSum = 0, maxPhase = 0;
            float[] carrierBuf = new float[BitDepth];

            for (float i = -(float) Math.PI / 2; i < Math.PI / 2; i += 0.1f)
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
            var sums = new float[frameLength * BitDepth];
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

        private byte[] CalcFrame(float[] samples, float[] carrier, int frameBytes)
        {
            var bytes = new byte[frameBytes];
            var nByte = 0;
            var nBit = 0;
            var nSample = 0;
            var lastData = false;
            var offset = 1;

            var debounce = 99; // a max number

            GetNextBit();

            for (var i = 0; i < frameBytes; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    PushNextBit(GetNextBit());
                }
            }

            return bytes;

            bool GetNextBit()
            {
                debounce++;
                
                float sum = 0, sump = 0, summ = 0;
                for (var k = 0; k < BitDepth; k++)
                {
                    sum += samples[offset + nSample] * carrier[nSample];
                    if (debounce >= 100)
                    {
                        summ += samples[offset + nSample - 1] * carrier[nSample];
                        sump += samples[offset + nSample + 1] * carrier[nSample];
                    }

                    nSample++;
                }

                if (debounce >= 100)
                {
                    var (a0, am, ap) = (Math.Abs(sum), Math.Abs(summ), Math.Abs(sump));
                    
                    if (ap - a0 > 0.1)
                    {
                        Trace.WriteLine($"+ | from {sum} to {sump} at {nSample}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                        sum = sump;
                        offset++;
                        debounce = 0;
                    }
                    else if (am - a0 > 0.1)
                    {
                        Trace.WriteLine($"- | from {sum} to {summ} at {nSample}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                        sum = summ;
                        offset--;
                        debounce = 0;
                    }
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

        private byte[] CalcFrame(in IEnumerable<float> sums, int frameBytes)
        {
            var bytes = new byte[frameBytes];
            var nByte = 0;
            var nBit = 0;
            var lastData = false;

            var e = sums.GetEnumerator();

            GetNextBit();

            for (var i = 0; i < frameBytes; i++)
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