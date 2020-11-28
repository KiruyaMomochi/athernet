using System;
using System.Linq;
using Athernet.PhysicalLayer.Receive.PreambleDetector;
using Athernet.PreambleBuilder;
using Xunit;

namespace AthernetTest.PreambleDetectorTest
{
    public class CrossCorrelationDetectorTest
    {
        [Theory]
        [InlineData(250, 4096)]
        [InlineData(4096, 250)]
        [InlineData(250, 250)]
        [InlineData(4096, 4096)]
        [InlineData(0, 200)]
        public void DetectIdealPreamble(int before, int after)
        {
            var preamble = new WuPreambleBuilder(48000, 0.01f).Build();
            var detector = new CrossCorrelationDetector(preamble);
            var samples = new float[before].Concat(preamble).Concat(new float[after]).ToArray();
            var pos = detector.Detect(samples);
            Assert.Equal(before + preamble.Length - 1, pos);
        }
    }    
}