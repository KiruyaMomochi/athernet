using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace Athernet.PhysicalLayer.Receive.Rx.Demodulator
{
    public interface IDemodulatorRx
    {
        int Channel { get; init; }
        int SampleRate { get; init; }
        int BitDepth { get; init; }
        int MaxFrameBytes { get; set; }
        int Frequency { get; set; }
        int Gain { get; set; }

        IObservable<IEnumerable<byte>> Demodulate(IObservable<float> samples);
        int FrameSamples(int frameBytes);
    }
}