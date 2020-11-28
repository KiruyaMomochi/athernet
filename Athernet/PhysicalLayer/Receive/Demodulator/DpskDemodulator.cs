using System;

namespace Athernet.PhysicalLayer.Receive.Demodulator
{
    public class DpskDemodulator: IDemodulator
    {
        public int BitDepth { get; set; }
        public int SampleRate { get; set; }
        public byte[] Demodulate(float[] samples, int maxFrameBytes)
        {
            throw new NotImplementedException();
        }
    }
}
