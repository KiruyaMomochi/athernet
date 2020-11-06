using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Athernet.Modulators;
using Force.Crc32;
using NAudio.Wave;

namespace Athernet.PhysicalLayer
{
    public sealed class Transmitter
    {
        public int DeviceNumber => _wo.DeviceNumber;

        public int SampleRate => Modulator.SampleRate;

        public Channel Channel { get; set; } = Channel.Mono;

        public float[] Preamble { get; set; } = new float[0];

        public IModulator Modulator { get; set; }

        public TransmitState State { get; private set; } = TransmitState.Idle;

        private readonly BufferedWaveProvider _provider;

        private readonly WaveOutEvent _wo;

        /// <summary>
        /// Indicates the playing is stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayStopped;

        /// <summary>
        /// Indicates the playing is complete
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayComplete;

        public Transmitter(IModulator modulator, int deviceNumber = 0,int bufferLength = 19200000)
        {
            Modulator = modulator;
            _provider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1))
            {
                BufferLength = bufferLength,
                ReadFully = false
            };
            _wo = new WaveOutEvent()
            {
                DeviceNumber = deviceNumber
            };

            Init();
        }

        // TPL Blocks used for transmitter.
        // AddCrc -> ModulateArray -> PlaySamples.
        private TransformBlock<IEnumerable<byte>, byte[]> _addCrc;

        private TransformBlock<byte[], float[]> _modulateArray;

        private ActionBlock<float[]> _playSamples;

        readonly EventWaitHandle _playSamplesEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        private void Init()
        {
            // Initialize blocks.
            _addCrc = new TransformBlock<IEnumerable<byte>, byte[]>(AddCrc);
            _modulateArray = new TransformBlock<byte[], float[]>(s => Modulator.Modulate(s));
            _playSamples = new ActionBlock<float[]>(AddSamples);

            // Link blocks.
            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};
            _addCrc.LinkTo(_modulateArray, linkOptions);
            _modulateArray.LinkTo(_playSamples, linkOptions);

            // Init WaveOutEvent
            _wo.Init(_provider);
            _wo.PlaybackStopped += (s, a) => { OnPlayStopped(a); };
        }

        public void AddPayload(IEnumerable<byte> bytes)
        {
            State = TransmitState.Transmitting;
            _addCrc.Post(bytes);
        }

        public void Complete()
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

        private void AddSamples(float[] samples)
        {
            Trace.WriteLine($"P3. Playing samples {samples.Length}.");

            // Athernet.Utils.Debug.WriteTempWav(samples, "real_body.wav");
            var byteFrame = new byte[(Preamble.Length + samples.Length) * 4];

            Buffer.BlockCopy(Preamble, 0, byteFrame, 0, Preamble.Length * 4);
            Buffer.BlockCopy(samples, 0, byteFrame, Preamble.Length * 4, samples.Length * 4);

            if (Channel != Channel.Mono)
            {
                throw new NotImplementedException();
            }

            _provider.AddSamples(byteFrame, 0, byteFrame.Length);
            _wo.Play();
        }

        private void OnPlayStopped(StoppedEventArgs e)
        {
            PlayStopped?.Invoke(this, e);

            if (_playSamples.Completion.IsCompleted)
            {
                OnPlayComplete(e);
            }
        }

        private void OnPlayComplete(StoppedEventArgs e)
        {
            State = TransmitState.Idle;
            PlayComplete?.Invoke(this, e);
        }
    }
}