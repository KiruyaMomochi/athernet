using Athernet.Modulators;
using Xunit;

namespace AthernetTest.ModulatorTest
{
    public class DpskModulatorTest
    {
        [Fact]
        public void DemodulateSelf()
        {
            var modulator = new DpskModulator(48000, 8000)
            {
                FrameBytes = 125
            };
            var bytes = new byte[modulator.FrameBytes];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte) (i % byte.MaxValue);
            }
            var res = modulator.Demodulate(modulator.Modulate(bytes));
            Assert.Equal(bytes.Length, res.Length);
            for (int i = 0; i < res.Length; i++)
            {
                Assert.Equal(bytes[i], res[i]);
            }
        }
    }
}
