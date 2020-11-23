using NAudio.Wave;
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
        private uint _nSample = 0;

        public int Read(float[] buffer, int offset, int count)
        {
            // Generator current value
            int outIndex = offset;

            // Complete Buffer
            for (int sampleCount = 0; sampleCount < count / WaveFormat.Channels; sampleCount++)
            {
                var multiple = TwoPi * Frequency / WaveFormat.SampleRate;
                var sampleValue = Gain * Math.Sin(_nSample * multiple + PhaseShift);
                for (int i = 0; i < WaveFormat.Channels; i++) 
                    buffer[outIndex++] = (float) sampleValue;
                _nSample++;
            }
            return count;
        }

        public int Peek(float[] buffer, int offset, int count)
        {
            // Generator current value
            int outIndex = offset;

            // Complete Buffer
            for (int sampleCount = 0; sampleCount < count / WaveFormat.Channels; sampleCount++)
            {
                var multiple = TwoPi * Frequency / WaveFormat.SampleRate;
                var sampleValue = Gain * Math.Sin((_nSample + sampleCount) * multiple + PhaseShift);
                for (int i = 0; i < WaveFormat.Channels; i++) 
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
