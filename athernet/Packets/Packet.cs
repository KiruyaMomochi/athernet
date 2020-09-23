using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Text;

namespace athernet.Packets
{
    public class Packet : DiscreteSignal
    {
        public Packet(int sampleRate, int length) : base(sampleRate, length) { }
        public Packet(int sampleRate, IEnumerable<float> samples) : base(sampleRate, samples) { }
        public Packet(int sampleRate, float[] samples, bool allocateNew = false) : base(sampleRate, samples, allocateNew) { }

        public new Packet Copy()
        {
            return new Packet(SamplingRate, Samples, true);
        }
    }
}
