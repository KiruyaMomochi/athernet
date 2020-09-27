using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace Athernet.SampleProviders
{
    public class MonoRawSampleProvider : ISampleProvider
    {
        private readonly IEnumerator<float> sampleEnumerator;

        public WaveFormat WaveFormat { get; }
        public MonoRawSampleProvider(IEnumerable<float> data) : this(48000, data) { }
        public MonoRawSampleProvider(int sampleRate, IEnumerable<float> data)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            sampleEnumerator = data.GetEnumerator();
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!sampleEnumerator.MoveNext())
                {
                    return i;
                }
                buffer[offset + i] = sampleEnumerator.Current;
            }

            return count;
        }
    }
}
