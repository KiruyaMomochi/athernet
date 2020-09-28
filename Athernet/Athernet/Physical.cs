using Athernet.Modulators;
using Athernet.Preambles.PreambleDetectors;
using Athernet.SampleProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Athernet
{
    /// <summary>
    /// The physical layer of Athernet
    /// </summary>
    public class Physical
    {
        /// <summary>
        /// The sample rate of acoustic signal
        /// </summary>
        /// <remarks>
        /// For the best result, use the sample rate that is supported by device
        /// </remarks>
        public int SampleRate { get; set; } = 48000;

        /// <summary>
        /// The number of bits used for a sample.
        /// </summary>
        public int BitDepth { get; set; } = 32;

        /// <summary>
        /// The number of bits that each frame should transmit
        /// </summary>
        /// <remarks>
        /// This may not equal to the number of bits in the packet
        /// For example, when using differential modulators, there will be one more bit
        /// </remarks>
        public int FrameBodyBits { get; set; } = 1000;

        /// <summary>
        /// The modulator that should be used for modulation and demodulation
        /// </summary>
        public IModulator Modulator { get; set; }

        /// <summary>
        /// The channel to play acoustic signal
        /// </summary>
        public Channel PlayChannel { get; set; } = Channel.Mono;
        
        /// <summary>
        /// The preamble to prepend before the frame body.
        /// </summary>
        public float[] Preamble { get; set; }

        /// <summary>
        /// True if it is capturing signals.
        /// </summary>
        public bool IsRecording = false;

        /// <summary>
        /// Indicates new data is available
        /// </summary>
        public event EventHandler<BitArray> DataAvailable;

        private readonly WaveOutEvent wo = new WaveOutEvent();
        private float[] buffer = new float[0];
        private bool decoding = false;
        private int ReceivedFrameLength => (FrameBodyBits + 1) * BitDepth;
        private WaveInEvent recorder;

        /// <summary>
        /// Channels that can be used to play signal
        /// </summary>
        public enum Channel
        {
            /// <summary>
            /// Use single channel, or multiple channel at the same time
            /// </summary>
            Mono,
            /// <summary>
            /// Use the left channel only
            /// </summary>
            Left,
            /// <summary>
            /// Use the right channel only
            /// </summary>
            Right
        }

        /// <summary>
        /// Create a Physical layer object
        /// </summary>
        /// <param name="preamble">The preamble to prepend before the frame body.</param>
        public Physical(float[] preamble)
        {
            Preamble = preamble;
            Modulator = new DPSKModulator(SampleRate, 8000, 1)
            {
                BitDepth = BitDepth
            };
        }

        /// <summary>
        /// Play <paramref name="bitArray"/> using acoustic signals.
        /// </summary>
        /// <param name="bitArray"></param>
        /// <remarks>
        /// If the length of <paramref name="bitArray"/> not divisible by <c>DivideBitArray</c>,
        /// plain signal will be appended to the end of the last package.
        /// </remarks>
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

        /// <summary>
        /// Start recording for the new data
        /// </summary>
        public void StartRecording()
        {
            if (IsRecording == true)
            {
                return;
            }

            var demodulateSamples = new TransformBlock<float[], BitArray>(DemodulateSamples);
            var dataAvailable = new ActionBlock<BitArray>(OnDataAvailable);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            recorder = new WaveInEvent() { WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1) };
            demodulateSamples.LinkTo(dataAvailable, linkOptions);
            recorder.DataAvailable += (s, e) => Recorder_DataAvailable(e, demodulateSamples, recorder.WaveFormat.BitsPerSample);
            recorder.RecordingStopped += (s, e) => demodulateSamples.Complete();
            recorder.StartRecording();
            IsRecording = true;
        }

        /// <summary>
        /// Stop recording for the new data
        /// </summary>
        public void StopRecording()
        {
            recorder.StopRecording();
            recorder.Dispose();
            IsRecording = false;
        }

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
            ISampleProvider provider = new MonoRawSampleProvider(SampleRate, Preamble.Concat(samples));

            if (PlayChannel == Channel.Left)
            {
                provider = new MonoToStereoSampleProvider(provider)
                {
                    LeftVolume = 1.0f,
                    RightVolume = 0.0f
                };
            }
            else if (PlayChannel == Channel.Right)
            {
                provider = new MonoToStereoSampleProvider(provider)
                {
                    LeftVolume = 0.0f,
                    RightVolume = 1.0f
                };
            }

            // TODO: Async
            wo.Init(provider);
            wo.Play();

            Thread.Sleep(samples.Length * 1000 / SampleRate + 100);
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(10);
            }
            wo.Dispose();
        }

        private void OnDataAvailable(BitArray obj)
        {
            var handler = DataAvailable;
            handler?.Invoke(this, obj);
        }

        private void Recorder_DataAvailable(WaveInEventArgs e, TransformBlock<float[], BitArray> b, int bitsPerSample)
        {
            var data = ToFloatBuffer(e.Buffer, e.BytesRecorded, bitsPerSample);

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

        private float[] ToFloatBuffer(Byte[] buffer, int bytesRecorded, int bitsPerSample)
        {
            var wave = new WaveBuffer(buffer);

            float[] floatBuffer = bitsPerSample switch
            {
                16 => wave.ShortBuffer.Take(bytesRecorded / 2).Select(x => (float)x).ToArray(),
                32 => wave.FloatBuffer.Take(bytesRecorded / 4).ToArray(),
                _ => throw new Exception(),
            };

            return floatBuffer;
        }
    }
}
