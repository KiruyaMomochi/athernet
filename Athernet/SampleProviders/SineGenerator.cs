﻿using NAudio.Wave;
using System;

namespace Athernet.SampleProviders
{
    public class SineGenerator : ISampleProvider
    {
        public double Frequency { get; set; }
        public double Gain { get; set; }
        public double PhaseShift { get; set; }
        public WaveFormat WaveFormat { get; }

        public SineGenerator() : this(48000, 1) { }
        public SineGenerator(int sampleRate, int channel)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);

            // Default
            Frequency = 440.0;
            Gain = 1;
            PhaseShift = 0.0;
        }

        private const double TwoPi = 2 * Math.PI;
        private int nSample = 0;

        public int Read(float[] buffer, int offset, int count)
        {
            // Generator current value
            double multiple;
            double sampleValue;
            int outIndex = offset;

            // Complete Buffer
            for (int sampleCount = 0; sampleCount < count / WaveFormat.Channels; sampleCount++)
            {
                multiple = TwoPi * Frequency / WaveFormat.SampleRate;
                sampleValue = Gain * Math.Sin(nSample * multiple + PhaseShift);
                buffer[outIndex++] = (float)sampleValue;
                nSample++;
            }
            return count;
        }

        public void Reset()
        {
            nSample = 0;
        }

        public void Reset(double phaseShift)
        {
            nSample = 0;
            PhaseShift = phaseShift;
        }
    }
}
