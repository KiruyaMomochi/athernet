using System;

namespace Athernet.PhysicalLayer.PreambleBuilder
{
    public class WuPreambleBuilder : FunctionPreambleBuilder
    {
        public WuPreambleBuilder(int sampleRate, float time) : base(PreambleFunc, time)
        {
            SampleRate = sampleRate;
        }

        private static float PreambleFunc(int nSample, int sampleRate, int sampleCount)
        {
            var totalTime = (float)sampleCount / sampleRate;
            var time = (float)nSample / sampleRate;
            const float frequencyMin = 4000;
            const float frequencyMax = 9000;

            var a = (frequencyMax - frequencyMin) * 2 / totalTime;
            const float soundVoice = 1;

            if (nSample < sampleCount / 2)
            {
                var phase = time * time * a * (float)Math.PI + time * frequencyMin * (float)Math.PI * 2;
                var ans = (float)Math.Cos(phase) * soundVoice;
                return ans;
            }
            else
            {
                var phase = -time * time * a * (float)Math.PI + time * frequencyMax * 2 * (float)Math.PI;
                var ans = (float)Math.Cos(phase) * soundVoice;
                return ans;
            }
        }
    }
}
