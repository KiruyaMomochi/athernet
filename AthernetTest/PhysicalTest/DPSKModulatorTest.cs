using System;
using System.Linq;
using Athernet.PhysicalLayer.Receive.Rx.Demodulator;
using Athernet.PhysicalLayer.Transmit.Modulator;
using Xunit;
using System.Reactive;
using System.Reactive.Linq;

namespace AthernetTest.ModulatorTest
{
    public class DpskModulationTest
    {
        [Fact]
        public void Modulate()
        {
            var dpskModulator = new DpskModulator
            {
                SampleRate = 44100,
                Channel = 2,
                BitDepth = 33
            };
            Assert.Equal(44100, dpskModulator.SampleRate);
            Assert.Equal(2, dpskModulator.Channel);
            Assert.Equal(33, dpskModulator.BitDepth);
        }

        [Fact]
        public void InitModulateDefault()
        {
            var dpskModulator = new DpskModulator();
            Assert.Equal(48000, dpskModulator.SampleRate);
            Assert.Equal(1, dpskModulator.Channel);
            Assert.Equal(3, dpskModulator.BitDepth);
        }

        [Fact]
        public void ModulateData()
        {
            var dpskModulator = new DpskModulator
            {
                BitDepth = 100
            };
            var data = new byte[100];
            Array.Fill<byte>(data, 255);
            var res = dpskModulator.Modulate(data);
        }

        [Fact]
        public void DemodulateModulatedData()
        {
            var dpskModulator = new DpskModulator();
            var dpskDemodulatorRx = new DpskDemodulatorRx{ MaxFrameBytes = 1000 };

            var data = new byte[1000];
            new Random().NextBytes(data);
            data[0] = 0;
            var res = dpskModulator.Modulate(data).Concat(new float[5]);
            var obs = dpskDemodulatorRx.Demodulate(res.ToObservable());
            var recData = obs.Subscribe(x =>
            {
                Console.WriteLine(x);
            });
            obs.Wait();
            //Assert.True(data.SequenceEqual(recData));
        }

        [Fact]
        public void InitDemodulate()
        {
            var demodulatorRx = new DpskDemodulatorRx
            {
                SampleRate = 44100,
                Channel = 2,
                BitDepth = 233
            };
            Assert.Equal(44100, demodulatorRx.SampleRate);
            Assert.Equal(2, demodulatorRx.Channel);
            Assert.Equal(233, demodulatorRx.BitDepth);
        }

        [Fact]
        public void InitDemodulateDefault()
        {
            var demodulatorRx = new DpskDemodulatorRx();
            Assert.Equal(48000, demodulatorRx.SampleRate);
            Assert.Equal(1, demodulatorRx.Channel);
            Assert.Equal(3, demodulatorRx.BitDepth);
        }
    }
}
