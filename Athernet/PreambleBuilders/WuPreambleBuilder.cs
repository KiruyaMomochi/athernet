using System;
using System.Collections.Generic;
using System.Text;

namespace Athernet.Preambles.PreambleBuilders
{
    public class WuPreambleBuilder : FunctionPreambleBuilder
    {
        public WuPreambleBuilder(int sampleRate, float time) : base(PreambleFunc, sampleRate, time)
        {
        }

        static float PreambleFunc(int nSample, int sampleRate, int sampleCount)
        {
            float totalTime = (float)sampleCount / sampleRate;
            float time = (float)nSample / sampleRate;
            float frequencyMin = 4000;
            float frequencyMax = 9000;

            float a = (frequencyMax - frequencyMin) * 2 / totalTime;
            float soundVoice = 1;

            if (nSample < sampleCount / 2)
            {
                float phase = time * time * a * (float)Math.PI + time * frequencyMin * (float)Math.PI * 2;
                float anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
            else
            {
                float phase = -time * time * a * (float)Math.PI + time * frequencyMax * 2 * (float)Math.PI;
                float anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
        }
    }
}
