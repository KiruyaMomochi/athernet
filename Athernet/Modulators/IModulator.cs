using System.Collections.Generic;

namespace Athernet.Modulators
{
    public interface IModulator
    {
        int BitDepth { get; set; }
        int SampleRate { get; set; }

        public int FrameSamples(int frameBytes);
        
        float[] Modulate(byte[] bytes);

        byte[] Demodulate(float[] samples, int frameBytes);
    }
}