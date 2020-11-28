using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using Athernet.SampleProvider;
using Athernet.Utils;
using Debug = System.Diagnostics.Debug;

namespace Athernet.PhysicalLayer.Receive.Rx.Demodulator
{
    public class DpskDemodulatorRx : IDemodulatorRx
    {
        public int MaxFrameBytes { get; set; }
        public int Channel { get; init; } = 1;
        public int BitDepth { get; init; } = 3;
        public int SampleRate { get; init; } = 48000;

        public int Frequency { get; set; } = 8000;
        public int Gain { get; set; } = 1;

        /// <summary>
        /// Get the frame samples. The samples including everything in the frame. 
        /// </summary>
        /// <param name="frameBytes">The number of bytes in a frame.</param>
        /// <returns>The number of samples in a frame. </returns>
        public int FrameSamples(int frameBytes)
        {
            Debug.Assert(BitDepth != 0);
            Debug.Assert(frameBytes <= MaxFrameBytes);

            return BitDepth * (frameBytes * Const.ByteBits + 1);
        }

        public IObservable<IEnumerable<byte>> Demodulate(IObservable<float> samples)
        {
            Debug.Assert(SampleRate != 0);
            Debug.Assert(MaxFrameBytes != 0);
            var core = new DpskDemodulatorCore(new SineGenerator(SampleRate, Channel) { Frequency = Frequency, Gain = Gain }, MaxFrameBytes - 1)
            {
                BitDepth = BitDepth,
                SamplesCapacity = FrameSamples(MaxFrameBytes),
                CarrierBufferLength = FrameSamples(MaxFrameBytes) + SampleRate / 10
            };
            return core.Init(samples);
        }
    }
}
