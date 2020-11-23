using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using Athernet.SampleProviders;
using NAudio.Wave;

namespace Athernet.Modulators
{
    public class PskCore
    {
        public int BitDepth { get; init; }
        public ISubject<byte> Payload { get; }

        private readonly List<float> _samples;

        private int _processing = 0;

        private readonly float[] _carrier;

        private bool _complete;

        private int _carrierOffset;
        private readonly int _carrierLength;
        
        public PskCore(IObservable<float> source, SineGenerator sampleProvider, int carrierLength = 18000)
        {
            Console.Write("+");

            // TODO: Give it a initial size.
            _samples = new List<float>();
            // TODO: Give it a buffer size.
            Payload = new ReplaySubject<byte>();
            source.Subscribe(OnNextSample, OnError, OnComplete);

            _carrierLength = carrierLength;
            _carrier = new float[_carrierLength];
            _sampleProvider = sampleProvider;
            _sampleProvider.Read(_carrier, 0, _carrierLength);
        }

        private void OnError(Exception obj)
        {
            Console.WriteLine("OnError");
            Payload.OnError(obj);
        }

        private void OnComplete()
        {
            Console.WriteLine("Complete");
            if (_complete)
                return;;

            _complete = true;
            Payload.OnCompleted();
        }

        private void OnNextSample(float sample)
        {
            _samples.Add(sample);
            _listCnt++;
            Process();
        }

        private int _offset;
        private int _nSample;
        private int _listCnt;
        private bool _firstBit = true;

        private byte _byte;
        private int _nBit;
        private readonly SineGenerator _sampleProvider;

        private void Process()
        {
            if (Interlocked.Exchange(ref _processing, 1) != 0)
                return;

            try
            {
                while (!_complete)
                {
                    // Do nothing if there is no new bit we can process.
                    if (_listCnt < _nSample + BitDepth + _offset + 2)
                        return;

                    // If it is the first bit, we check offset 0, 1, 2 and 3
                    // Adjust parameter?
                    var sum = _firstBit ? AdjustSum(0, 2) : AdjustSum(-1, 1);
                    _firstBit = false;
                    
                    if (sum > 0)
                        _byte |= (byte)(1 << _nBit);
                    AdvanceBit();
                    CheckCarrier();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _processing, 0);
            }
        }

        private void AdvanceBit()
        {
            _nBit++;
            if (_nBit != 8)
                return;
            
            // if _nBit == 8
            Payload.OnNext(_byte);
            _nBit = 0;
            _byte = 0;
        }

        private void CheckCarrier()
        {
            if (_nSample - _carrierOffset + BitDepth + 3 < _carrierLength)
                return;
            
            var remainSampleLength = _carrierLength - (_nSample - _carrierOffset);
            _sampleProvider.SeekBack((uint)remainSampleLength);
            _sampleProvider.Read(_carrier, 0, _carrierLength);
            _carrierOffset = _nSample;
        }
        
        private float AdjustSum(int minOffset, int maxOffset)
        {
            var localMaximum = 0f;
            var localMaxOffset = 0;

            Trace.Assert(_nSample + _offset + minOffset >= 0);

            for (int offset = minOffset; offset < maxOffset + 1; offset++)
            {
                var sum = 0f;
                for (int i = 0; i < BitDepth; i++)
                    sum += _samples[_nSample + _offset + offset + i] * _carrier[_nSample - _carrierOffset + i];
                if (!Compare(localMaximum, sum))
                    continue;

                localMaximum = sum;
                localMaxOffset = offset;
            }

            _offset += localMaxOffset;
            _nSample += BitDepth;

            return localMaximum;
        }

        private static bool Compare(in float pre, in float now)
            => pre >= 0 && now > pre || pre <= 0 && now < pre;
    }
}