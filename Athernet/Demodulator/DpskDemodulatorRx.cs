using System;
using Athernet.SampleProvider;

namespace Athernet.Demodulator
{
    public class DpskDemodulatorRx : PskDemodulatorRx
    {
        private bool lastBit = false;
        
        public DpskDemodulatorRx(
            IObservable<float> source,
            SineGenerator carrierGenerator,
            int carrierBufferLength = 18000)
            : base(source, carrierGenerator, carrierBufferLength)
        {
        }

        protected override void SetBit(bool b)
        {
            base.SetBit(lastBit ^ b);
            lastBit = b;
        }
    }
}