using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Athernet.Demodulator
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
