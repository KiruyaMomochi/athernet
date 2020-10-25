using NAudio.Wave;
using NWaves.Operations.Convolution;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NWaves.Utils.MemoryOperationExtensions;

namespace Athernet.Preambles.PreambleDetectors
{
    public class CrossCorrelationDetector
    {
        public readonly int WindowSize = 200;

        public float[] Preamble { get; private set; }
        
        private int FftSize => Utils.Maths.Power2RoundUp(Preamble.Length + WindowSize);

        private float[] _samples;
        private readonly Convolver _convolver;

        private readonly float[] _kernel;
        private readonly float[] _output;

        private int SampleLength => Preamble.Length + WindowSize;

        public CrossCorrelationDetector(float[] preamble)
        {
            if (preamble.Length == 0)
                throw new NotSupportedException("Preamble should not be empty.");
            
            Preamble = preamble;
            _convolver = new Convolver(FftSize);

            // Prepare arrays for convolution.
            _samples = new float[FftSize];
            _kernel = new float[Preamble.Length];
            _output = new float[FftSize * 2];
        }

        public int CrossCorrelate()
        {
            // Do the cross correlation.
            Preamble.FastCopyTo(_kernel, Preamble.Length);
            // Athernet.Utils.Debug.PlaySamples(_samples);

            _convolver.CrossCorrelate(_samples, _kernel, _output);
            
            // Find the index of the max
            float localPower = 0;
            var localMaximum = float.MinValue;
            var maxIndex = -1;

            int i = 0;
            for (; i < Preamble.Length - 1; i++)
            {
                localPower = localPower * 63 / 64 + _output[i] * _output[i] / 64;
            }
            
            for (; i < _output.Length; i++)
            {
                localPower = localPower * 63 / 64 + _output[i] * _output[i] / 64;

                if (_output[i] < localMaximum || _output[i] < 120)
                    continue;
                if (_output[i] * 3 > localPower)
                {
                    continue;
                }

                // Athernet.Utils.Debug.PlaySamples(_samples.Take(i).TakeLast(Preamble.Length));
                localMaximum = _output[i];
                maxIndex = i;
            }

            // Athernet.Utils.Debug.PlaySamples(_samples.Take(maxIndex).TakeLast(Preamble.Length));
            return maxIndex;
        }

        /// <summary>
        /// Detect the possibility that preamble exists in <paramref name="samples"/>
        /// by cross-correlating
        /// </summary>
        /// <param name="samples">The samples to detect</param>
        /// <returns>The position of local maximum if the preamble is found, otherwise -1.</returns>
        public int Detect(float[] samples)
        {
            var offset = 0;

            // Do cross correlation while the length of samples is longer than FFT size
            while (samples.Length - offset >= FftSize)
            {
                samples.FastCopyTo(_samples, FftSize, offset);
                
                // Find the last index of preamble.
                // It's valid only if it is not less than Preamble.Length-1 (Case 1, 2).
                var pos = CrossCorrelate();

                // Case 1: the index stays in [Preamble.Length-1, Length-WindowSize).
                if (pos >= Preamble.Length - 1 && pos < FftSize - WindowSize)
                {
                    return offset + pos;
                }
                
                // Case 2: the index stays in [Length-WindowSize, Length).
                if (pos >= FftSize - WindowSize)
                {
                    offset = offset + pos + 1 - Preamble.Length;
                    continue;
                }

                // Case 3: the index is not found.
                offset = offset + FftSize - Preamble.Length; 
            }

            // Tail case: the length is shorter than FFT size, but still possible for a detection.
            if (samples.Length - offset >= Preamble.Length + WindowSize)
            {
                Array.Clear(_samples, 0, FftSize);
                samples.FastCopyTo(_samples, samples.Length - offset, offset);
                
                var pos = CrossCorrelate();

                if (pos >= Preamble.Length - 1 && pos < samples.Length - offset - WindowSize)
                {
                    return offset + pos;
                }
            }

            return -1;
        }
    }
}
