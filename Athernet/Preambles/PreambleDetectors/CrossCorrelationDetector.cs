using Athernet.SampleProviders;
using NAudio.Wave;
using NWaves.Operations.Convolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Athernet.Preambles.PreambleDetectors
{
    public class CrossCorrelationDetector
    {
        public int LocalMaximumThreshold { get; set; } = 15;
        public int DetectFactor { get; set; } = 2;

        /// <summary>
        /// Detect the possiblity that preamble exists in <paramref name="samples"/>
        /// by cross-correlating
        /// </summary>
        /// <param name="samples">The samples to detect</param>
        /// <returns>The position of local maximum if the preamble is found, otherwise 0.</returns>
        public int? Detect(float[] samples, float[] template)
        {
            int maxSize = Math.Max(samples.Length, template.Length);
            int fftSize = Utils.Maths.Power2RoundUp(maxSize);
            Convolver convolver = new Convolver(fftSize);

            float[] output = new float[fftSize];
            float[] kernel = (float[])template.Clone();
            convolver.CrossCorrelate(samples, kernel, output);

            float localMaximum = 0;
            int maxidx = 0;

            for (int i = 0; i < output.Length; i++)
            {
                if (output[i] > localMaximum)
                {
                    localMaximum = output[i];
                    maxidx = i;
                }
            }

            if (localMaximum > LocalMaximumThreshold && maxidx < samples.Length && maxidx >= template.Length)
            {
                var sum = samples.Skip(maxidx - template.Length).Take(template.Length).Aggregate(0f, (p, n) => p + n * n);
                if (localMaximum > sum * DetectFactor)
                {
                    //Console.WriteLine($"max index: {maxidx}. localMaximum: {localMaximum}. sum: {sum}");
                    return maxidx;
                }
            }
            return null;
        }
    }
}
