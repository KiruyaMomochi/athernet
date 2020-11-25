using System;

namespace Athernet.PreambleBuilder
{
    public class WuPreambleBuilder : FunctionPreambleBuilder
    {
        public WuPreambleBuilder(int sampleRate, float time) : base(PreambleFunc, sampleRate, time)
        {
        }

        static float PreambleFunc(int nSample, int sampleRate, int sampleCount)
        {
            var totalTime = (float)sampleCount / sampleRate;
            var time = (float)nSample / sampleRate;
            float frequencyMin = 4000;
            float frequencyMax = 9000;

            var a = (frequencyMax - frequencyMin) * 2 / totalTime;
            float soundVoice = 1;

            if (nSample < sampleCount / 2)
            {
                var phase = time * time * a * (float)Math.PI + time * frequencyMin * (float)Math.PI * 2;
                var anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
            else
            {
                var phase = -time * time * a * (float)Math.PI + time * frequencyMax * 2 * (float)Math.PI;
                var anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
        }
    }
}
