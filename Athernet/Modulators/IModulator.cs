using System.Collections.Generic;

namespace Athernet.Modulators
{
    public interface IModulator
    {
        int BitDepth { get; set; }
        int SampleRate { get; set; }
        int FrameBytes { get; set; }
        int FrameSamples { get; }

        int FrameBits => FrameBytes * 8;

        float[] Modulate(IEnumerable<byte> bytes);

        byte[] Demodulate(float[] samples);
    }
}