using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Threading;
using Athernet.SampleProvider;

namespace Athernet.Modulator
{
    /// <summary>
    /// Demodulate samples from IObservable by PSK method.
    /// </summary>
    public class PskDemodulatorRx
    {
        public int BitDepth { get; init; }

        /// <summary>
        /// The payload where demodulated data will be saved to.
        /// </summary>
        public ISubject<byte> Payload { get; }

        /// <summary>
        /// The list to save samples.
        /// The sample received from Observable will be added here.
        /// </summary>
        private readonly List<float> _samples;

        /// <summary>
        /// The lock for <c>Process</c> function.
        /// When processing, the value is <value>1</value>.
        /// </summary>
        private int _processing = 0;

        /// <summary>
        /// The carrier buffer.
        /// </summary>
        private readonly float[] _carrierBuffer;

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
        /// PSKCore constructor.
        /// </summary>
        /// <param name="source">Samples to be demodulated, as an <c>IObservable</c>.</param>
        /// <param name="carrierGenerator">A sine generator, which will be used to generate carrier.</param>
        /// <param name="carrierBufferLength">The length of carrier buffer.</param>
        public PskDemodulatorRx(IObservable<float> source, SineGenerator carrierGenerator, int carrierBufferLength = 18000)
        {
            // TODO: Give it a initial size.
            _samples = new List<float>();
            // TODO: Give it a buffer size.
            Payload = new ReplaySubject<byte>();
            source.Subscribe(OnNextSample, OnError, OnComplete);


            // Initialize carrier by carrierBufferLength
            _carrierGenerator = carrierGenerator;
            _carrierBuffer = new float[carrierBufferLength];
            _carrierGenerator.Read(_carrierBuffer, 0, carrierBufferLength);
        }

        /// <summary>
        /// The callback function when error occurs in <c>source</c>. 
        /// </summary>
        /// <param name="obj">The exception object.</param>
        private void OnError(Exception obj)
        {
            Console.WriteLine("OnError");
            Payload.OnError(obj);
        }

        /// <summary>
        /// The callback function when <c>source</c> finishes.
        /// </summary>
        private void OnComplete()
        {
            Console.WriteLine("Complete");
            if (_complete)
                return;

            _complete = true;
            Payload.OnCompleted();
        }

        /// <summary>
        /// The callback function when <c>source</c> emits a new sample.
        /// </summary>
        /// <param name="sample">The sample received.</param>
        private void OnNextSample(float sample)
        {
            _samples.Add(sample);
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
        private bool _firstBit = true;

        /// <summary>
        /// The byte in process.
        /// </summary>
        private byte _byte;

        /// <summary>
        /// The bit in process.
        /// </summary>
        private int _nBit;

        /// <summary>
        /// The carrier generator, which is initialized in constructor.
        /// When the buffer is fully used, we use this generator to generate new buffer.
        /// </summary>
        private readonly SineGenerator _carrierGenerator;

        /// <summary>
        /// The main function where we process new samples.
        /// </summary>
        private void Process()
        {
            // Interlock the function so there can only be one instance
            // running at the same time.
            if (Interlocked.Exchange(ref _processing, 1) != 0)
                return;

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
                    var sum = _firstBit ? AdjustSum(0, 2) : AdjustSum(-1, 1);

                    // If sum is positive, we set _nBit of _byte to true
                    SetBit(sum > 0);

                    // Move to the next bit
                    AdvanceBit();

                    // Check if carrier need to be updated
                    CheckCarrier();
                    
                    // Set the first bit to false,
                    // so the following bits won't be treated as the first one. 
                    _firstBit = false;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _processing, 0);
            }
        }

        /// <summary>
        /// If <paramref name="b"/> is true, set the current bit to true.
        /// </summary>
        /// <param name="b">The value to be set</param>
        protected virtual void SetBit(bool b)
        {
            if (b)
                _byte |= (byte) (1 << _nBit);
        }

        /// <summary>
        /// Advance a bit.
        /// If the byte is fully processed, emit the byte and jump to a new byte.
        /// </summary>
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

        /// <summary>
        /// Check if carrier buffer is mostly used.
        /// If yes, then update the buffer.
        /// </summary>
        private void CheckCarrier()
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

            Trace.Assert(_nSample + _offset + minOffset >= 0);

            // Loop for each possible offset.
            for (int offset = minOffset; offset < maxOffset + 1; offset++)
            {
                // Calculate the sum.
                var sum = 0f;
                for (int i = 0; i < BitDepth; i++)
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
        /// Compare the last sum with the new sum
        /// </summary>
        /// <param name="pre">The sum to be compared with.</param>
        /// <param name="now">The new sum.</param>
        /// <returns>True if the new sum is better.</returns>
        private static bool Compare(in float pre, in float now)
            => pre >= 0 && now > pre || pre <= 0 && now < pre;
    }
}