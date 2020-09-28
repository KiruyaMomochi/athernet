using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Athernet.Modulators
{
    public interface IModulator
    {
        int BitDepth { get; set; }
        int SampleRate { get; set; }

        float[] Modulate(BitArray frame);
        BitArray Demodulate(float[] samples);
    }
}
