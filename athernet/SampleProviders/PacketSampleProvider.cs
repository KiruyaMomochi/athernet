using athernet.Packets;
using athernet.Preambles;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace athernet.SampleProviders
{
    class PacketSampleProvider: ISampleProvider
    {
        private readonly IEnumerator<float> sampleEnumerator;

        public WaveFormat WaveFormat { get; }
        public PacketSampleProvider(Preamble preamble, Packet packet) : this(48000, preamble, packet) { }
        public PacketSampleProvider(int sampleRate, Preamble preamble, Packet packet)
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            sampleEnumerator = preamble.Data.Concat(packet.Samples).GetEnumerator();
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
