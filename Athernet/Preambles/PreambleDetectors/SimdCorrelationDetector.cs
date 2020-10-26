using System;
using System.Numerics;
using NWaves.Operations.Convolution;

namespace Athernet.Preambles.PreambleDetectors
{
    public class SimdCorrelationDetector
    {
        public readonly int WindowSize = 200;
        public readonly float[] Preamble;

        private int SampleLength => Preamble.Length + WindowSize;

        public SimdCorrelationDetector(float[] preamble)
        {
            if (preamble.Length == 0)
                throw new NotSupportedException("Preamble should not be empty.");
            if (Vector.IsHardwareAccelerated == false)
                throw new NotSupportedException("SIMD is not supported.");
            
            Preamble = preamble;
            _result = new float[Preamble.Length];
        }

        private float[] _result;

        private void SimdCorrelate(in float[] samples, int offset)
        {
            var chunkSize = Vector<float>.Count;
            var i = 0;
            for (i = 0; i < Preamble.Length; i += chunkSize)
            {
                var v1 = new Vector<float>(Preamble, i);
                var v2 = new Vector<float>(samples, offset + i);
                (v1 * v2).CopyTo(_result, i);
            }

            for (; i < Preamble.Length; i++)
            {
                _result[i] = Preamble[i] * samples[offset + i];
            }
        }

        public void Detect(float[] samples)
        {
            for (int i = 0; i < samples.Length - Preamble.Length; i++)
            {
                SimdCorrelate(samples, i);
            }
        }
    }
}