using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Athernet.SampleProvider;
using Microsoft.VisualBasic;
using Debug = System.Diagnostics.Debug;

namespace Athernet.PhysicalLayer.Receive.Rx.Demodulator
{
    /// <summary>
    /// Demodulate samples from IObservable by PSK method.
    /// </summary>
    /// <remarks>
    /// Physical = <b>Preamble</b> + <b>Frame</b>.
    /// Frame = <b>Length</b> + <b>Payload</b>.
    /// </remarks>
    /// <remarks>
    /// Payload may contain CRC or something like that, however, Demodulator won't check that.
    /// </remarks>
    public class PskDemodulatorCore
    {
        /// <summary>
        /// Number of samples for a bit.
        /// </summary>
        public int BitDepth { get; init; }

        /// <summary>
        /// The payload where demodulated data will be saved to.
        /// </summary>
        private ReplaySubject<IEnumerable<byte>> Payload { get; }

        /// <summary>
        /// The list to save samples.
        /// The sample received from Observable will be added here.
        /// </summary>
        private List<float> _samples;

        /// <summary>
        /// The lock for <c>Process</c> function.
        /// When processing, the value is <value>1</value>.
        /// </summary>
        private int _processing;

        /// <summary>
        /// The carrier buffer.
        /// </summary>
        private float[] _carrierBuffer;

        /// <summary>
        /// It is true only if all data have been processed.
        /// </summary>
        private bool _complete;

        /// <summary>
        /// Offset to the carrier.
        /// When the length of carrier is too long, we subtract this from <c>nSample</c>
        /// to obtain the real index of <c>_carrierBuffer</c>.
        /// </summary>
        private int _carrierOffset;

        /// <summary>
        /// The real length of payload.
        /// </summary>
        private uint? _lastPayloadBytes;

        private readonly List<byte> _frame;

        private readonly int _maxPayloadBytes;

        private IObservable<float> _source;

        /// <summary>
        /// PSKCore constructor.
        /// </summary>
        /// <param name="carrierGenerator">The function used to generate signal.</param>
        /// <param name="maxBytes">The max length of payload.</param>
        public PskDemodulatorCore(SineGenerator carrierGenerator, int maxBytes)
        {
            _maxPayloadBytes = maxBytes;
            _carrierGenerator = carrierGenerator;
            _frame = new List<byte>(_maxPayloadBytes);
            Payload = new ReplaySubject<IEnumerable<byte>>(1);
        }

        /// <summary>
        /// Init the instance and subscribe to the source.
        /// </summary>
        /// <param name="source">The source observable.</param>
        public ISubject<IEnumerable<byte>> Init(IObservable<float> source)
        {
            _source = source;

            _samples = new List<float>(SamplesCapacity);
            // Initialize carrier by carrierBufferLength

            Debug.Assert(CarrierBufferLength != 0);

            _carrierBuffer = new float[CarrierBufferLength];
            _carrierGenerator.Read(_carrierBuffer, 0, CarrierBufferLength);
            _source.Subscribe(OnNextSample, OnError, OnComplete);
            return Payload;
        }

        /// <summary>
        /// The callback function when <c>source</c> finishes.
        /// </summary>
        private void OnComplete()
        {
            //Console.WriteLine("OnComplete.");
            //Process();
            //_complete = true;
            //Process();
        }

        /// <summary>
        /// Initial length of samples list.
        /// </summary>
        public int SamplesCapacity { get; init; }

        /// <summary>
        /// The length of carrier buffer.
        /// </summary>
        public int CarrierBufferLength { get; set; }

        /// <summary>
        /// The SineGenerator used to generate the carrier.
        /// </summary>
        private readonly SineGenerator _carrierGenerator;

        /// <summary>
        /// The callback function when error occurs in <c>source</c>. 
        /// </summary>
        /// <param name="obj">The exception object.</param>
        private void OnError(Exception obj)
        {
            Console.WriteLine("OnError");
            Payload.OnError(obj);
        }

        private void Complete()
        {
            // Utils.Debug.WriteTempWav(_samples.ToArray(), "recv_body.wav");
            _complete = true;
            // Console.WriteLine("Complete");

            Debug.Assert(_frame.Count <= _maxPayloadBytes);
            if (Payload.IsDisposed)
                return;

            Payload.OnNext(_frame);
            Payload.OnCompleted();
        }

        /// <summary>
        /// The callback function when <c>source</c> emits a new sample.
        /// </summary>
        /// <param name="sample">The sample received.</param>
        private void OnNextSample(float sample)
        {
            if (_complete) return;
            _samples.Add(sample);
            // if (_samples.Count % 100 == 0)
            // {
            //     Console.Write($"{sample} ");
            // }
            Process();
        }

        /// <summary>
        /// The offset used for fixing frequency offset.
        /// </summary>
        private int _offset;

        /// <summary>
        /// The number of sample counting from the start.
        /// </summary>
        private int _nSample;

        /// <summary>
        /// Indicating if the processing bit is the first one.
        /// </summary>
        protected bool FirstBit = true;

        /// <summary>
        /// The byte in process.
        /// </summary>
        protected byte Byte;

        /// <summary>
        /// The bit in process.
        /// </summary>
        protected int NBit;

        /// <summary>
        /// The main function where we process new samples.
        /// </summary>
        private void Process()
        {
            // Interlock the function so there can only be one instance
            // running at the same time.
            if (Interlocked.Exchange(ref _processing, 1) != 0)
                return;

            Debug.Assert(BitDepth != 0);

            try
            {
                // If _complete is true, stop the loop
                while (!_complete)
                {
                    // Do nothing if there is no new bit we can process.
                    if (_samples.Count < _nSample + BitDepth + _offset + 2)
                        return;

                    // If it is the first bit, we check offset 0, 1, 2 and 3
                    // TODO: Adjust parameter may still need to be changed
                    //var sum = FirstBit ? AdjustSum(0, 2) : AdjustSum(-1, 1);
                    var sum = FirstBit ? AdjustSum(0, 2) : AdjustSumOffset(1 , -1);

                    // If sum is positive, we set _nBit of _byte to true
                    SetBit(sum > 0);
                    // Move to the next bit
                    AdvanceBit();
                    // Check if carrier need to be updated
                    UpdateCarrier();
                    // Check if complete.
                    UpdateComplete();

                    // Set the first bit to false,
                    // so the following bits won't be treated as the first one. 
                    FirstBit = false;
                }
            }
            finally
            {
                // Release the lock
                Interlocked.Exchange(ref _processing, 0);
            }
        }

        /// <summary>
        /// Check if the process is complete. If true, set <code>complete</code>.
        /// </summary>
        private void UpdateComplete()
        {
            // Complete when no more frame bytes
            if (_lastPayloadBytes == 0)
            {
                // Utils.Debug.WriteTempWav(_samples.ToArray(), "recv_body.wav");
                //_source.SkipLast(0);
                //OnComplete();
                // Console.WriteLine("Set _complete");
                Complete();
            }
        }

        /// <summary>
        /// If <paramref name="b"/> is true, set the current bit to true.
        /// </summary>
        /// <param name="b">The value to be set</param>
        protected virtual void SetBit(bool b)
        {
            if (b)
                Byte |= (byte) (1 << NBit);
        }

        /// <summary>
        /// Advance a bit.
        /// If the byte is fully processed, emit the byte and jump to a new byte.
        /// </summary>
        protected virtual void AdvanceBit()
        {
            NBit++;
            if (NBit != 8)
                return;

            // if _nBit == 8
            AdvanceByte();

            NBit = 0;
            Byte = 0;
        }

        /// <summary>
        /// Advance a byte.
        /// If it is the first byte, it will set <code>_lastFrameBytes</code>.
        /// Otherwise, it will be added to <code>_frame</code>.
        /// </summary>
        private void AdvanceByte()
        {
            // Debug.Write($"-> {Byte:X} Offset: {_offset}\n");

            // Check if it is the first byte
            if (_lastPayloadBytes == null)
            {
                // If true, we set _lastPayloadBytes to 1 << _byte
                _lastPayloadBytes = (uint) 1 << Byte;
                Debug.WriteLine($"Payload Bytes: {_lastPayloadBytes}", GetType());
                // If the result is more than MaxBytes,
                // We complete the list directly.
                if (_lastPayloadBytes > _maxPayloadBytes) Complete();
            }
            else
            {
                // Add bit to the list
                _frame.Add(Byte);
                _lastPayloadBytes--;
            }
        }

        /// <summary>
        /// Check if carrier buffer is mostly used.
        /// If yes, then update the buffer.
        /// </summary>
        private void UpdateCarrier()
        {
            if (_nSample - _carrierOffset + BitDepth + 3 < _carrierBuffer.Length)
                return;

            // Find the remaining samples.
            var remainSampleLength = _carrierBuffer.Length - (_nSample - _carrierOffset);
            _carrierGenerator.SeekBack((uint) remainSampleLength);

            // Read samples.
            _carrierGenerator.Read(_carrierBuffer, 0, _carrierBuffer.Length);
            _carrierOffset = _nSample;
        }

        /// <summary>
        /// Adjust offset based on the sum, and return the sum with the best quality.
        /// </summary>
        /// <param name="minOffset">Minimum offset.</param>
        /// <param name="maxOffset">Maximum offset.</param>
        /// <returns>The best sum.</returns>
        private float AdjustSum(int minOffset, int maxOffset)
        {
            // Init local maximum and offset.
            var localMaximum = 0f;
            var localMaxOffset = 0;

            Debug.Assert(_nSample + _offset + minOffset >= 0);

            // Loop for each possible offset.
            for (var offset = minOffset; offset < maxOffset + 1; offset++)
            {
                // Calculate the sum.
                var sum = 0f;
                for (var i = 0; i < BitDepth; i++)
                    sum += _samples[_nSample + _offset + offset + i] * _carrierBuffer[_nSample - _carrierOffset + i];

                // Compare the local maximum with the new sum
                if (!Compare(localMaximum, sum))
                    continue;

                // If the new sum is better, save it
                localMaximum = sum;
                localMaxOffset = offset;
            }

            // Update offset and _nSample
            _offset += localMaxOffset;
            _nSample += BitDepth;

            return localMaximum;
        }

        /// <summary>
        /// Adjust offset based on the sum, and return the sum with the best quality.
        /// </summary>
        /// <param name="offsets">The offset to check, should not contain 0.</param>
        /// <returns>The best sum.</returns>
        private float AdjustSumOffset(params int[] offsets)
        {
            // Init local maximum and offset.
            var localMaximum = 0f;
            var localMaxOffset = 0;

            Debug.Assert(_nSample + _offset + offsets.Min() >= 0);
            Debug.Assert(!offsets.Contains(0), "offset should not contain 0.");

            for (var i = 0; i < BitDepth; i++)
                localMaximum += _samples[_nSample + _offset + i] * _carrierBuffer[_nSample - _carrierOffset + i];

            // Loop for each possible offset.
            foreach (var offset in offsets)
            {
                // Calculate the sum.
                var sum = 0f;
                for (var i = 0; i < BitDepth; i++)
                    sum += _samples[_nSample + _offset + offset + i] * _carrierBuffer[_nSample - _carrierOffset + i];

                // Compare the local maximum with the new sum
                if (!Compare(localMaximum, sum, 0.05f))
                    continue;

                // If the new sum is better, save it
                localMaximum = sum;
                localMaxOffset = offset;
                break;
            }

            // Update offset and _nSample
            _offset += localMaxOffset;
            _nSample += BitDepth;

            return localMaximum;
        }

        /// <summary>
        /// Compare the last sum with the new sum
        /// </summary>
        /// <param name="pre">The sum to be compared with.</param>
        /// <param name="now">The new sum.</param>
        /// <returns>True if the new sum is better.</returns>
        private static bool Compare(in float pre, in float now)
        {
            return pre >= 0 && now > pre || pre <= 0 && now < pre;
        }

        /// <summary>
        /// Compare the last sum with the new sum
        /// </summary>
        /// <param name="pre">The sum to be compared with.</param>
        /// <param name="now">The new sum.</param>
        /// <param name="delta">The tolerate delta.</param>
        /// <returns>True if the new sum is better.</returns>
        private static bool Compare(in float pre, in float now, in float delta)
        {
            return pre >= 0 && now >= 0 && now - pre > delta || pre <= 0 && now <= 0 && pre - now > delta;
        }
    }
}