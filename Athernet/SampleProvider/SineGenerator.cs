using System;
using System.Diagnostics;
using NAudio.Wave;

namespace Athernet.SampleProvider
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
            Debug.Assert(sampleRate != 0);
            Debug.Assert(channel != 0);

            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);

            // Default
            Frequency = 440.0;
            Gain = 1;
            PhaseShift = 0.0;
        }

        private const double TwoPi = 2 * Math.PI;
        private uint _nSample;

        public int Read(float[] buffer, int offset, int count)
        {
            // Generator current value
            var outIndex = offset;

            // Complete Buffer
            for (var sampleCount = 0; sampleCount < count / WaveFormat.Channels; sampleCount++)
            {
                var multiple = TwoPi * Frequency / WaveFormat.SampleRate;
                var sampleValue = Gain * Math.Sin(_nSample * multiple + PhaseShift);
                for (var i = 0; i < WaveFormat.Channels; i++) 
                    buffer[outIndex++] = (float) sampleValue;
                _nSample++;
            }
            return count;
        }

        public int Peek(float[] buffer, int offset, int count)
        {
            // Generator current value
            var outIndex = offset;

            // Complete Buffer
            for (var sampleCount = 0; sampleCount < count / WaveFormat.Channels; sampleCount++)
            {
                var multiple = TwoPi * Frequency / WaveFormat.SampleRate;
                var sampleValue = Gain * Math.Sin((_nSample + sampleCount) * multiple + PhaseShift);
                for (var i = 0; i < WaveFormat.Channels; i++) 
                    buffer[outIndex++] = (float) sampleValue;
            }
            return count;
        }

        public void SeekBack(uint offset)
        {
            _nSample -= offset;
        }

        public void Reset()
        {
            _nSample = 0;
        }

        public void Reset(double phaseShift)
        {
            _nSample = 0;
            PhaseShift = phaseShift;
        }
    }
}
