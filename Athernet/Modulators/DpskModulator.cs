using Athernet.SampleProviders;
using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Athernet.Utils;

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
            var demodulateCarrier = NewSineSignal();
            samples = ApplyFiltersBeforeMultiply(samples);
            return CalcFrame(samples, demodulateCarrier, frameBytes);
        }

        public byte[] DemodulateRx(List<float> li)
        {
            Trace.WriteLine($"R3. Demodulate using {this.GetType().Name}.");

            // Read the head
            
            return new byte[0];
        }

        private float FindPhase(in float[] signal, SineGenerator carrier)
        {
            float maxSum = 0, maxPhase = 0;
            float[] carrierBuf = new float[BitDepth];

            for (float i = -(float) Math.PI / 10; i < Math.PI / 10; i += 0.01f)
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

        private byte[] CalcFrame(float[] samples, SineGenerator demodulateCarrier, int frameBytes)
        {
            var bytes = new byte[frameBytes];
            var nByte = 0;
            var nBit = 0;
            var nSample = 0;
            var lastData = false;
            var offset = 1;

            var debounce = 233; // a max number

            var carrier = new float[samples.Length];
            demodulateCarrier.Read(carrier, 0, carrier.Length);
            if (GetFirstBit())
            {
                Trace.WriteLine("DPK Return now.");
                return new byte[0];
            }

            for (var i = 0; i < frameBytes; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    PushNextBit(GetNextBit());
                }
            }

            return bytes;

            bool GetFirstBit()
            {
                float sum = 0, sump1 = 0, sump2 = 0, summ = 0;

                for (var k = 0; k < BitDepth; k++)
                {
                    sum += samples[offset + nSample] * carrier[nSample];
                    summ += samples[offset + nSample - 1] * carrier[nSample];
                    sump1 += samples[offset + nSample + 1] * carrier[nSample];
                    sump2 += samples[offset + nSample + 2] * carrier[nSample];

                    nSample++;
                }

                Trace.WriteLine($"? | {sum}, {sump1}, {sump2}, {summ}");
                if ((sum <= 0 && sump1 <= 0) || (sump1 <= 0 && summ <= 0) || (sum <= 0 && summ <= 0))
                {
                    return true;
                }

                // var max = new []{sum, sump1, sump2, summ}.Max();

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (sump1 > sum)
                {
                    offset++;
                    Trace.WriteLine(
                        $"+ | from {sum} to {sump1} at {nSample - 3}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                    sum = sump1;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (summ > sum)
                {
                    offset--;
                    Trace.WriteLine(
                        $"- | from {sum} to {summ} at {nSample - 3}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                    sum = summ;
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (sump2 > sum)
                {
                    offset += 2;
                    Trace.WriteLine(
                        $"+ | from {sum} to {sump2} at {nSample - 3}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                    sum = sump1;
                }

                lastData = sum > 0;
                return false;
            }

            bool GetNextBit()
            {
                debounce++;

                float sum = 0, sump = 0, summ = 0;
                for (var k = 0; k < BitDepth; k++)
                {
                    sum += samples[offset + nSample] * carrier[nSample];
                    if (debounce >= 50)
                    {
                        summ += samples[offset + nSample - 1] * carrier[nSample];
                        sump += samples[offset + nSample + 1] * carrier[nSample];
                    }

                    nSample++;
                }

                if (debounce >= 50)
                {
                    var (a0, am, ap) = (Math.Abs(sum), Math.Abs(summ), Math.Abs(sump));
                    // Console.WriteLine($"? | {sum}, {sump}, {summ}");

                    if (ap - a0 > 0.05 && sum * sump > 0)
                    {
                        offset++;
                        Trace.WriteLine(
                            $"+ | from {sum} to {sump} at {nSample - 3}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                        sum = sump;
                        debounce = 0;
                    }
                    else if (am - a0 > 0.05 && sum * summ > 0)
                    {
                        offset--;
                        Trace.WriteLine(
                            $"- | from {sum} to {summ} at {nSample - 3}-th sample, {nByte} byte, {nBit} bit.\t Offset: {offset}");
                        sum = summ;
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