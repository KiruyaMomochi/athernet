using Athernet.Modulators;
using Athernet.Preambles.PreambleDetectors;
using Athernet.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Athernet
{
    public class Athernet
    {
        public Athernet()
        {
            SampleRate = 48000;
            BitDepth = 44;
            Preamble = new float[0];
            FrameBodyBits = 100;
            Modulator = new DPSKModulator(SampleRate, 8000, 1);
        }

        public int SampleRate { get; set; }
        public int BitDepth { get; set; }
        public float[] Preamble { get; set; }
        public int FrameBodyBits { get; set; }

        public DPSKModulator Modulator { get; set; }

        private WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);

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

            target[i] = new BitArray(FrameBodyBits);
            for (int j = 0; idx < source.Length; j++)
            {
                target[i][j] = source[idx++];
            }

            return target;
        }

        private void PlaySamples(float[] samples)
        {
            var provider = new RawSampleProvider(SampleRate, Preamble.Concat(samples));
            using WaveOutEvent wo = new WaveOutEvent();
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
            var modulateArray = new TransformBlock<BitArray, float[]>(s => Modulator.Modulate(s));
            var playSamples = new ActionBlock<float[]>(PlaySamples);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            splitBitArray.LinkTo(modulateArray, linkOptions);
            modulateArray.LinkTo(playSamples, linkOptions);

            splitBitArray.Post(bitArray);
            splitBitArray.Complete();
            playSamples.Completion.Wait();
        }

        private void OnDataAvailable(BitArray obj)
        {
            var handler = DataAvailable;
            handler?.Invoke(this, obj);
        }

        public event EventHandler<BitArray> DataAvailable;
        private WaveInEvent recorder;

        public void StartRecording()
        {
            var demodulateSamples = new TransformBlock<float[], BitArray>(DemodulateSamples);
            var dataAvailable = new ActionBlock<BitArray>(OnDataAvailable);

            //var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            demodulateSamples.LinkTo(dataAvailable);

            recorder = new WaveInEvent() { WaveFormat = WaveFormat };
            recorder.DataAvailable += (s, e) => Recorder_DataAvailable(e, demodulateSamples);

            recorder.StartRecording();
        }

        public void StopRecording()
        {
            recorder.StopRecording();
        }

        private float[] buffer = new float[0];
        private bool decoding = false;
        private int ReceivedFrameLength => (FrameBodyBits + 1) * BitDepth;

        private void Recorder_DataAvailable(WaveInEventArgs e, TransformBlock<float[], BitArray> b)
        {
            var data = ToFloatBuffer(e.Buffer, e.BytesRecorded);

            if (decoding)
            {
                var tmp = buffer.Concat(data);
                var rawframe = tmp.Take(ReceivedFrameLength).ToArray();

                if (rawframe.Length < ReceivedFrameLength)
                {
                    buffer = rawframe;
                    decoding = true;
                    return;
                }

                b.Post(rawframe);

                buffer = tmp.Skip(ReceivedFrameLength).ToArray();
                decoding = false;
            }
            else
            {
                buffer = buffer.TakeLast(Preamble.Length).Concat(data).ToArray();
            }

            int? pos = new CrossCorrelationDetector().Detect(buffer, Preamble);

            if (pos is int ipos)
            {
                var rawframe = buffer.Skip(ipos + 1).Take(ReceivedFrameLength).ToArray();

                if (rawframe.Length < ReceivedFrameLength)
                {
                    buffer = rawframe;
                    decoding = true;
                    return;
                }
                b.Post(rawframe);
            }
        }

        private BitArray DemodulateSamples(float[] samples)
        {
            return Modulator.Demodulate(samples);
        }

        private float[] ToFloatBuffer(Byte[] buffer, int bytesRecorded)
        {
            var wave = new WaveBuffer(buffer);

            float[] floatBuffer = WaveFormat.BitsPerSample switch
            {
                16 => wave.ShortBuffer.Take(bytesRecorded / 2).Select(x => (float)x).ToArray(),
                32 => wave.FloatBuffer.Take(bytesRecorded / 4).ToArray(),
                _ => throw new Exception(),
            };

            return floatBuffer;
        }
    }
}
