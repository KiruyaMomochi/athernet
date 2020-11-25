using System.Collections.Generic;
using NAudio.Wave;

namespace Athernet.SampleProvider
{
    public class MonoRawSampleProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; }
        public MonoRawSampleProvider(IEnumerable<float> data) : this(48000, data) { }
        public MonoRawSampleProvider(int sampleRate, IEnumerable<float> data)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            _sampleEnumerator = data.GetEnumerator();
        }

        private readonly IEnumerator<float> _sampleEnumerator;

        public int Read(float[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (_sampleEnumerator.MoveNext())
                    buffer[offset + i] = _sampleEnumerator.Current;
                else
                    return i;
            }

            return count;
        }
    }
}
