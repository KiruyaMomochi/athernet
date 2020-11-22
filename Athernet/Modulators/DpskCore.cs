using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using Athernet.Utils;
using NAudio.Utils;
using NAudio.Wave;

namespace Athernet.Modulators
{
    public class DpskCore
    {
        private int _nBit = 0;
        private int _nByte = 0;
        private int _nSample = 0;
        private bool _lastData = false;
        private int _offset = 1;

        private byte _byte;

        private float _sum, _sump, _summ, _sump2;
        private int _debounce;

        private int _size;
        private readonly float[] _carrier;
        private readonly int _maxFrameBytes;
        private readonly EventWaitHandle _finishEwh = new ManualResetEvent(false);
        private readonly int _bitDepth;

        private readonly List<float> _samples;
        private readonly ReplaySubject<byte> _payload;

        private readonly object _lock = new object();

        public int DebounceThreshold { get; set; } = 500;
        public IObservable<byte> Payload => _payload;

        private void ProcessFirstBit(bool bit)
        {
            if (!bit)
            {
                _payload.OnCompleted();
                return;
            }

            _finishEwh.Set();
        }

        private void ProcessSize(int size)
        {
            _size = size;
        }

        private void ProcessOneByte(byte b)
        {
            _payload.OnNext(b);
            _size--;

            if (_size == 0)
                _payload.OnCompleted();
        }

        public void Add(float sample)
        {
            lock (_lock)
            {
                _samples.Add(sample);
            }
        }

        private void CalcSum(bool summ = false, bool sump = false, bool sump2 = false)
        {
            _summ = _sum = _sump = _sump2 = 0;
            lock (_lock)
            {
                for (var k = 0; k < _bitDepth; k++)
                {
                    _summ += _samples[_offset + _nSample - 1] * _carrier[_nSample];
                    if (summ)
                        _sum += _samples[_offset + _nSample] * _carrier[_nSample];
                    if (sump)
                        _sump += _samples[_offset + _nSample + 1] * _carrier[_nSample];
                    if (sump2)
                        _sump2 += _samples[_offset + _nSample + 2] * _carrier[_nSample];

                    _nSample++;
                }
            }
        }

        private bool CheckFastReply()
        {
            Console.WriteLine($"? | {_sum}, {_sump}, {_sump2}, {_summ}");
            return (_sum <= 0 && _sump <= 0) || (_sump <= 0 && _summ <= 0) || (_sum <= 0 && _summ <= 0);
        }

        private void AdjustFirstBit()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_sump > _sum)
            {
                _offset++;
                Trace.WriteLine(
                    $"+ | from {_sum} to {_sump} at {_nSample - 3}-th sample, {_nByte} byte, {_nBit} bit.\t Offset: {_offset}");
                _sum = _sump;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            else if (_summ > _sum)
            {
                _offset--;
                Trace.WriteLine(
                    $"- | from {_sum} to {_summ} at {_nSample - 3}-th sample, {_nByte} byte, {_nBit} bit.\t Offset: {_offset}");
                _sum = _summ;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            else if (_sump2 > _sum)
            {
                _offset += 2;
                Trace.WriteLine(
                    $"+ | from {_sum} to {_sump2} at {_nSample - 3}-th sample, {_nByte} byte, {_nBit} bit.\t Offset: {_offset}");
                _sum = _sump;
            }
        }

        private void AdjustNextBit()
        {
            var (a0, am, ap) = (Math.Abs(_sum), Math.Abs(_summ), Math.Abs(_sump));
            // Console.WriteLine($"? | {sum}, {sump}, {summ}");

            if (ap - a0 > 0.05 && _sum * _sump > 0)
            {
                _offset++;
                Trace.WriteLine(
                    $"+ | from {_sum} to {_sump} at {_nSample - 3}-th sample, {_nByte} byte, {_nBit} bit.\t Offset: {_offset}");
                _sum = _sump;
                _debounce = 0;
            }
            else if (am - a0 > 0.05 && _sum * _summ > 0)
            {
                _offset--;
                Trace.WriteLine(
                    $"- | from {_sum} to {_summ} at {_nSample - 3}-th sample, {_nByte} byte, {_nBit} bit.\t Offset: {_offset}");
                _sum = _summ;
                _debounce = 0;
            }
        }

        public DpskCore(ISampleProvider signal, int bitDepth, int maxFrameBytes = 300)
        {
            var maxSampleLength = (maxFrameBytes * 8 + 1 + 16) * bitDepth + 100;
            _samples = new List<float>(maxSampleLength);
            _bitDepth = bitDepth;
            _maxFrameBytes = maxFrameBytes;
            _carrier = new float[maxSampleLength];
            signal.Read(_carrier, 0, maxSampleLength);
        }

        private void ProcessNextBit(bool bit)
        {
            if (bit)
                _byte |= Utils.Maths.LittleByteMask[_nBit];
            _nBit++;

            if (_nBit != 8)
                return;

            ProcessNextByte(_byte);
        }

        private void ProcessNextByte(byte b)
        {
            switch (_nByte)
            {
                case 0:
                    _size = b;
                    break;
                case 1:
                    _size |= b << 8;
                    break;
                default:
                    _payload.OnNext(b);
                    break;
            }
            _nByte++;
            if (_nByte == _size)
            {
                _payload.OnCompleted();
            }
        }

        public void Complete() => _payload.OnCompleted();
        public void Error(Exception e) => _payload.OnError(e);
        
        private bool GetFirstBit()
        {
            CalcSum(true, true, true);
            if (CheckFastReply())
                return true;
            AdjustFirstBit();
            _lastData = _sum > 0;
            return false;
        }

        private bool GetNextBit()
        {
            if (_debounce >= 50)
            {
                CalcSum(true, true);
                AdjustNextBit();
                _debounce = 0;
            }
            else
            {
                CalcSum();
                _debounce++;
            }

            var r = (_sum > 0) ^ (_lastData);
            _lastData = _sum > 0;
            return r;
        }
    }
}