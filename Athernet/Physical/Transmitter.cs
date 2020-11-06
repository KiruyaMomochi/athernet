using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Athernet.Modulators;
using Athernet.SampleProviders;
using Force.Crc32;
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
            Init();
        }
        
        // TPL Blocks used for transmitter.
        // AddCrc -> ModulateArray -> PlaySamples.
        private TransformBlock<IEnumerable<byte>, byte[]> _addCrc;
        private TransformBlock<byte[], float[]> _modulateArray;
        private ActionBlock<float[]> _playSamples;
        
        EventWaitHandle _playSamplesEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        
        private void Init()
        {
            // Initialize blocks.
            _addCrc = new TransformBlock<IEnumerable<byte>, byte[]>(AddCrc);
            _modulateArray = new TransformBlock<byte[], float[]>(s => Modulator.Modulate(s));
            _playSamples = new ActionBlock<float[]>(PlaySamples);
            
            // Link blocks.
            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};
            _addCrc.LinkTo(_modulateArray, linkOptions);
            _modulateArray.LinkTo(_playSamples, linkOptions);
            _playSamples.Completion.ContinueWith(tsk => OnPlayStopped(EventArgs.Empty));
        }
        
        public void Play(IEnumerable<byte> bytes)
        {
            State = TransmitState.Transmitting;
            _addCrc.Post(bytes);
        }

        public void Stop()
        {
            if (State == TransmitState.Idle)
            {
                return;
            }
            _addCrc.Complete();
        }

        private static byte[] AddCrc(IEnumerable<byte> arg)
        {
            Trace.WriteLine("P1. Adding CRC to the incoming payload.");
            var arr = arg.Concat(new byte[4]).ToArray();
            Crc32Algorithm.ComputeAndWriteToEnd(arr);
            return arr;
        }

        // private IEnumerable<IEnumerable<byte>> DivideBytes(byte[] source)
        // {
        //     for (var i = 0; i < source.Length; i += Modulator.FrameBytes)
        //     {
        //         yield return source.Skip(i).Take(Modulator.FrameBytes).ToArray();
        //     }
        // }

        private void PlaySamples(float[] samples)
        {
            Trace.WriteLine($"P3. Playing samples.");
            
            // Athernet.Utils.Debug.WriteTempWav(samples, "real_body.wav");
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

            wo.PlaybackStopped += (s, a) => { _playSamplesEventWaitHandle.Set(); };
            _playSamplesEventWaitHandle.WaitOne();
            Trace.WriteLine($"P4. Finished playing.");
        }

        private void OnPlayStopped(EventArgs e)
        {
            State = TransmitState.Idle;
            PlayStopped?.Invoke(this, e);
        }
    }
}