using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Athernet.Modulators;
using Athernet.SampleProviders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Athernet.Physical
{
    public sealed class Transmitter
    {
        public int DeviceNumber { get; set; }
        public int SampleRate => Modulator.SampleRate;
        public Channel Channel { get; set; } = Channel.Mono;
        public float[] Preamble { get; set; } = new float[0];
        public IModulator Modulator { get; set; }
        public TransmitState State { get; private set; } = TransmitState.Idle;

        /// <summary>
        /// Indicates the playing is stopped
        /// </summary>
        public event EventHandler PlayStopped;

        public Transmitter(IModulator modulator)
        {
            Modulator = modulator;
        }

        public void Play(byte[] bytes)
        {
            if (State == TransmitState.Transmitting)
            {
                return;
            }

            State = TransmitState.Transmitting;

            var splitBitArray = new TransformManyBlock<byte[], IEnumerable<byte>>(DivideBytes);
            var modulateArray = new TransformBlock<IEnumerable<byte>, float[]>(s => Modulator.Modulate(s));
            var playSamples = new ActionBlock<float[]>(PlaySamples);

            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};
            splitBitArray.LinkTo(modulateArray, linkOptions);
            modulateArray.LinkTo(playSamples, linkOptions);

            splitBitArray.Post(bytes);
            splitBitArray.Complete();
            playSamples.Completion.ContinueWith(tsk => OnPlayStopped(EventArgs.Empty));
        }

        private IEnumerable<IEnumerable<byte>> DivideBytes(byte[] source)
        {
            for (var i = 0; i < source.Length; i += Modulator.FrameBytes)
            {
                yield return source.Skip(i);
            }
        }

        private void PlaySamples(float[] samples)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            ISampleProvider provider = new MonoRawSampleProvider(SampleRate, Preamble.Concat(samples));

            provider = Channel switch
            {
                Channel.Mono => provider,
                Channel.Left => new MonoToStereoSampleProvider(provider)
                {
                    LeftVolume = 1.0f,
                    RightVolume = 0.0f
                },
                Channel.Right => new MonoToStereoSampleProvider(provider)
                {
                    LeftVolume = 0.0f,
                    RightVolume = 1.0f
                },
                _ => throw new NotImplementedException(),
            };

            var wo = new WaveOutEvent
            {
                DeviceNumber = DeviceNumber
            };
            wo.Init(provider);
            wo.Play();

            wo.PlaybackStopped += (s, a) => { ewh.Set(); };
            ewh.WaitOne();
        }

        private void OnPlayStopped(EventArgs e)
        {
            State = TransmitState.Idle;
            PlayStopped?.Invoke(this, e);
        }
    }
}