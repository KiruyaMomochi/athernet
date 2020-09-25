using athernet.Modulators;
using athernet.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace athernet
{
    class Athernet
    {
        public Athernet()
        {
            SampleRate = 48000;
            BitDepth = 44;
            Preamble = new float[0];
            FrameBodyBits = 100;
        }

        public int SampleRate { get; set; }
        public int BitDepth { get; set; }
        public float[] Preamble { get; set; }
        public int FrameBodyBits { get; set; }

        public DPSKModulator Modulator;

        private BitArray[] DivideBitArray(BitArray source)
        {
            int numberOfArrays = (source.Length + FrameBodyBits - 1) / FrameBodyBits;
            int idx = 0;
            var target = new BitArray[numberOfArrays];

            int i;
            for (i = 0; i < numberOfArrays - 1; i++)
            {
                target[i] = new BitArray(FrameBodyBits);
                for (int j = 0; j < FrameBodyBits; j++)
                {
                    target[i][j] = source[idx++];
                }
            }

            for (int j = 0; idx < source.Length; j++)
            {
                target[i][j] = source[idx++];
            }

            return target;
        }

        private void PlaySamples(float[] samples)
        {
            var provider = new RawSampleProvider(Preamble.Concat(samples));
            using var wo = new WaveOutEvent();
            wo.Init(provider);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }

        public void Play(BitArray bitArray)
        {
            var splitBitArray = new TransformManyBlock<BitArray, BitArray>(DivideBitArray);
            var modulateArray = new TransformBlock<BitArray, float[]> (s => Modulator.Modulate(s));
            var playSamples = new ActionBlock<float[]>(PlaySamples);
        }

        public void Record()
        {

        }
    }
}
