using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Athernet.Modulator;
using Force.Crc32;
using NAudio.Wave;

namespace Athernet.PhysicalLayer
{
    public sealed class Transmitter
    {
        public int DeviceNumber => _wo.DeviceNumber;

        public Channel Channel { get; set; } = Channel.Mono;

        public float[] Preamble { get; set; } = new float[0];

        public DpskModulator Modulator { get; set; }

        public TransmitState State { get; private set; } = TransmitState.Idle;

        /// <summary>
        /// A buffered wave provider.
        /// Used for queuing samples that need to be played.
        /// </summary>
        private readonly BufferedWaveProvider _buffer;

        private readonly WaveOutEvent _wo;

        /// <summary>
        /// Indicates the playing is stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayStopped;

        /// <summary>
        /// Indicates the playing is complete
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayComplete;

        public Transmitter(DpskModulator modulator, int deviceNumber = 0, int bufferLength = 19200000)
        {
            Modulator = modulator;
            _buffer = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(48000, 1))
            {
                BufferLength = bufferLength,
                ReadFully = false
            };
            _wo = new WaveOutEvent()
            {
                DeviceNumber = deviceNumber
            };

            // Init pipeline and wave out event;
            Init();
        }

        // TPL Blocks used for transmitter.
        // PreProcess -> ModulateArray -> PlaySamples.
        private TransformBlock<byte[], byte[]> _preProcess;
        private TransformBlock<byte[], float[]> _modulateArray;
        private ActionBlock<float[]> _playSamples;

        /// <summary>
        /// Init the TPL pipeline blocks and the <code>WaveOutEvent</code>,
        /// add event listener to <code>PlaybackStopped</code>.
        /// </summary>
        private void Init()
        {
            // Initialize blocks.
            _preProcess = new TransformBlock<byte[], byte[]>(PreProcess);
            _modulateArray = new TransformBlock<byte[], float[]>(Modulator.Modulate);
            _playSamples = new ActionBlock<float[]>(AddSamples);

            // Link blocks.
            var linkOptions = new DataflowLinkOptions {PropagateCompletion = true};
            _preProcess.LinkTo(_modulateArray, linkOptions);
            _modulateArray.LinkTo(_playSamples, linkOptions);

            // Init WaveOutEvent.
            _wo.Init(_buffer);
            _wo.PlaybackStopped += (s, a) => { OnPlayStopped(a); };
        }

        public void AddPayload(byte[] bytes)
        {
            State = TransmitState.Transmitting;
            _preProcess.Post(bytes);
        }

        public void Complete()
        {
            if (State == TransmitState.Idle)
            {
                return;
            }

            _preProcess.Complete();
        }

        /// <summary>
        /// Process the <paramref name="arg"/> before modulation.
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private static byte[] PreProcess(byte[] arg)
        {
            return new PhysicalFrame(arg).Frame;
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
                throw new ArgumentOutOfRangeException();
            }
            
            _buffer.AddSamples(byteFrame, 0, byteFrame.Length);
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

        private static readonly byte[] Noise = new byte[]
            {252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252, 252};

        private static float[] _noiseSamples = null;

        public void SendPing()
        {
            _noiseSamples ??= 
                _noiseSamples = Modulator.Modulate(Noise, false);
            AddSamples(_noiseSamples);
        }
    }
}