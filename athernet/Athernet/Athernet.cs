using athernet.Modulators;
using athernet.Preambles;
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

            splitBitArray.Post(bitArray);
            splitBitArray.LinkTo(modulateArray, linkOptions);
            modulateArray.LinkTo(playSamples, linkOptions);

            splitBitArray.Complete();
            playSamples.Completion.Wait();
        }

        public void Record()
        {
            var demodulateSamples = new TransformBlock<float[], BitArray>(DemodulateSamples);
            var printResult = new ActionBlock<BitArray>(PrintResult);

            using WaveInEvent recorder = new WaveInEvent() { WaveFormat = WaveFormat };
            recorder.DataAvailable += (s, e) => Recorder_DataAvailable(e, demodulateSamples);

            recorder.StartRecording();
            demodulateSamples.LinkTo(printResult);
            Thread.Sleep(1000000);
        }

        private void PrintResult(BitArray bits)
        {
            foreach (var bit in bits)
            {
                Console.Write(bit switch
                {
                    true => "\nT",
                    false => "F",
                    _ => throw new NotImplementedException()
                } + " ");
            }
            Console.WriteLine();
        }

        private float[] buffer = new float[0];
        private bool decoding = false;
        private int ReceivedFrameLength => (FrameBodyBits + 1) * BitDepth;

        private void Recorder_DataAvailable(WaveInEventArgs e, TransformBlock<float[], BitArray> b)
        {
            //Console.WriteLine($"Recorded {e.BytesRecorded} bytes.");
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

            int? pos = CrossCorrelationDetector.Detect(buffer, Preamble);

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
