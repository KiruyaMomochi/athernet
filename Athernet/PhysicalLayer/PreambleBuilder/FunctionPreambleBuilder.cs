using System;
using System.Diagnostics;

namespace Athernet.PreambleBuilder
{
    public class FunctionPreambleBuilder
    {
        private readonly Func<int, int, int, float> _buildFunction;
        private float[] _preamble;
        private int _sampleRate;

        public int SampleRate
        {
            get => _sampleRate;
            set
            {
                if (value <= 0)
                {
                    throw new InvalidOperationException("SampleRate should be a positive integer");
                }
                if (_sampleRate == 0)
                {
                    _sampleRate = value;
                }
            }
        }

        public float Time { get; set; }
        public int SampleCount => (int)(SampleRate * Time);

        public FunctionPreambleBuilder(Func<int, int, int, float> buildFunction, float time)
        {
            _buildFunction = buildFunction;
            Time = time;
        }

        public float[] Build()
        {
            if (_preamble != null) 
                return (float[]) _preamble.Clone();

            Debug.Assert(SampleRate != 0);
            _preamble = new float[SampleCount];
            for (var i = 0; i < SampleCount; i++)
            {
                _preamble[i] = _buildFunction(i, SampleRate, SampleCount);
            }
            return (float[])_preamble.Clone();
        }
    }
}
