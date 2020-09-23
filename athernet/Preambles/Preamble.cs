using athernet.SampleProviders;
using NAudio.Wave;
using NWaves.Operations.Convolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace athernet.Preambles
{
    class Preamble
    {
        private readonly Convolver convolver;
        private readonly int fftSize;

        public int SampleRate { get; set; }

        public Preamble(float[] preambleData)
        {
            SampleRate = 48000;
            Data = preambleData;
            fftSize = Utils.Maths.Power2RoundUp(Data.Length * 2);
            convolver = new Convolver(fftSize);
        }

        /// <summary>
        /// The data in the preamble
        /// </summary>
        public readonly float[] Data;

        /// <summary>
        /// Detect the possiblity that preamble exists in <paramref name="samples"/>
        /// by cross-correlating
        /// </summary>
        /// <param name="samples">The samples to detect</param>
        /// <returns>The position of local maximum if the preamble is found, otherwise 0.</returns>
        public (float max, int pos) Detect(float[] samples)
        {
            float[] kernel = (float[])Data.Clone();
            float[] output = new float[fftSize];
            convolver.CrossCorrelate(samples, kernel, output);

            float max = output.Max();
            int pos = Array.LastIndexOf(output, max);

            return (max, pos);
        }

        public void Play()
        {
            var provider = new RawSampleProvider(SampleRate, Data);
            using var wo = new WaveOutEvent();
            wo.Init(provider);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }
    }
}
