using System;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using Athernet.PhysicalLayer.PreambleBuilder;
using Athernet.PhysicalLayer.Transmit.Modulator;
using Force.Crc32;
using NAudio.Wave;

namespace Athernet.PhysicalLayer.Transmit
{
    public sealed class Transmitter
    {
        public int DeviceNumber => _wo.DeviceNumber;

        private readonly float[] _preamble;

        public TransmitState State { get; private set; } = TransmitState.Idle;

        /// <summary>
        /// A buffered wave provider.
        /// Used for queuing samples that need to be played.
        /// </summary>
        private BufferedWaveProvider _buffer;

        private readonly WaveOutEvent _wo;

        /// <summary>
        /// Indicates the playing is stopped
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayStopped;

        /// <summary>
        /// Indicates the playing is complete
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayComplete;

        public int BufferLength
        {
            get => _buffer.BufferLength;
            set => _buffer.BufferLength = value;
        }

        public Transmitter(int deviceNumber, IModulator modulator, WuPreambleBuilder preambleBuilder)
        {
            _modulator = modulator;
            preambleBuilder.SampleRate = _modulator.SampleRate;
            _preamble = preambleBuilder.Build();
            
            _wo = new WaveOutEvent
            {
                DeviceNumber = deviceNumber
            };
            Init();
        }

        public Transmitter(int deviceNumber)
        {
            _modulator = new DpskModulator
            {
                BitDepth = 3,
                Channel = 1,
                SampleRate = 48000
            };

            _preamble = new WuPreambleBuilder(_modulator.SampleRate, 0.015f).Build();
            
            _wo = new WaveOutEvent
            {
                DeviceNumber = deviceNumber
            };
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
            _buffer = new BufferedWaveProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(_modulator.SampleRate, 1))
            {
                ReadFully = false
            };

            // Initialize blocks.
            _preProcess = new TransformBlock<byte[], byte[]>(PreProcess);
            _modulateArray = new TransformBlock<byte[], float[]>(_modulator.Modulate);
            _playSamples = new ActionBlock<float[]>(AddSamples);

            // Link blocks.
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
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
            // Add the length of CRC
            var payloadLength = arg.Length + 4;
            // var mask = Utils.Maths.MostSignificantBitMask(payloadLength);
            // Trace.Assert(
            //     payloadLength == mask,
            //     $"The length of data is {payloadLength}, but should be power of 2!"
            // );
            Trace.Assert(
                payloadLength < 1 << 16,
                $"The length of data is {payloadLength}, but should be less than {1 << 16}!");
            
            var frame = new byte[payloadLength + 2];

            byte len;
            // for (len = 0; mask != 1; mask >>= 1, len++) { }
            // Debug.WriteLine($"Len = {len}");

            frame[0] = (byte) (payloadLength >> 8);
            frame[1] = (byte) (payloadLength & 0xFF);
            Buffer.BlockCopy(arg, 0, frame, 2, arg.Length);
            
            Crc32Algorithm.ComputeAndWriteToEnd(frame, 2, arg.Length);
            Debug.WriteLine($"Crc is {BitConverter.ToString(frame[^4..])}");
            return frame;
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
            var byteFrame = new byte[(_preamble.Length + samples.Length) * 4];

            Buffer.BlockCopy(_preamble, 0, byteFrame, 0, _preamble.Length * 4);
            Buffer.BlockCopy(samples, 0, byteFrame, _preamble.Length * 4, samples.Length * 4);

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
        private readonly IModulator _modulator;

        public void SendPing()
        {
            _noiseSamples ??=
                _noiseSamples = _modulator.Modulate(Noise);
            AddSamples(_noiseSamples);
        }
    }
}