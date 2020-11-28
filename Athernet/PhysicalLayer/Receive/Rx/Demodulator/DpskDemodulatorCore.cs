using System.Diagnostics;
using Athernet.SampleProvider;

namespace Athernet.PhysicalLayer.Receive.Rx.Demodulator
{
    /// <summary>
    /// Demodulate samples from IObservable by differential PSK method.
    /// </summary>
    public class DpskDemodulatorCore : PskDemodulatorCore
    {
        private bool _lastBit;

        protected override void SetBit(bool b)
        {
            Debug.Write($"{b} ");
            if (_lastBit ^ b)
                Byte |= (byte) (1 << NBit);
            _lastBit = b;
        }

        protected override void AdvanceBit()
        {
            if (FirstBit)
            {
                Byte = 0;
                return;
            }

            base.AdvanceBit();
        }

        public DpskDemodulatorCore(SineGenerator carrierGenerator, int maxPayloadBytes) : base(carrierGenerator, maxPayloadBytes)
        {
        }
    }
}