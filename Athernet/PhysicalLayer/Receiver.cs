using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Athernet.Modulators;
using Athernet.Preambles.PreambleDetectors;
using Force.Crc32;
using NAudio.Wave;

namespace Athernet.PhysicalLayer
{
    /// <summary>
    /// Receiver of the physical layer
    /// </summary>
    public sealed class Receiver
    {
        public int DeviceNumber { get; }
        public int PayloadBytes { get; set; }
        public float[] Preamble { get; set; } = new float[0];
        public IModulator Modulator { get; set; }
        public ReceiveState State { get; private set; } = ReceiveState.Stopped;

        public int SampleRate => Modulator.SampleRate;

        /// <summary>
        /// Indicates new data is available
        /// </summary>
        public event EventHandler<DataAvailableEventArgs> DataAvailable;

        public event EventHandler PacketDetected;

        public Receiver(IModulator modulator, int deviceNumber = 0)
        {
            Modulator = modulator;
            DeviceNumber = deviceNumber;
        }

        public void StartReceive()
        {
            Trace.WriteLine($"-R- Starting receiver.");
            
            if (State != ReceiveState.Stopped)
                return;

            State = ReceiveState.Syncing;
            InitReceiver();
            StartRecorder();
        }

        public void ReceiveSamples(IEnumerable<float> samples)
        {
            if (State != ReceiveState.Stopped)
                return;

            State = ReceiveState.Syncing;
            InitReceiver();
            AddSamples(samples);
            _demodulateSamples.Complete();
            _dataAvailable.Completion.Wait();
            State = ReceiveState.Stopped;
        }

        private WaveInEvent _recorder;

        private TransformBlock<float[], byte[]> _demodulateSamples;
        private TransformBlock<byte[], DataAvailableEventArgs> _validateCrc;
        private ActionBlock<DataAvailableEventArgs> _dataAvailable;
        private static readonly DataflowLinkOptions LinkOptions = new DataflowLinkOptions {PropagateCompletion = true};
        private float[] _buffer = new float[0];
        
        private void StartRecorder()
        {            
            Trace.WriteLine($"-R- Starting recorder.");

            _recorder?.Dispose();
            _recorder = new WaveInEvent()
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1),
                DeviceNumber = DeviceNumber
            };
            _recorder.DataAvailable += RecorderOnDataAvailable;
            _recorder.RecordingStopped += (s, e) => _demodulateSamples.Complete();
            _recorder.StartRecording();
        }

        private void RecorderOnDataAvailable(object sender, WaveInEventArgs e)
        {
            var floatBuffer = Utils.Audio.ToFloatBuffer(e.Buffer, e.BytesRecorded, _recorder.WaveFormat.BitsPerSample);
            var t1 = Task.Run(() => AddSamples(floatBuffer));
            var t2 = Task.Run(() => _channelPower = floatBuffer.Select(x => x * x).Average());
            // Task.WaitAll(t1);
        }

        private float _channelPower;
        public bool ChannelFree => _channelPower < 0.01;

        private void InitReceiver()
        {
            _demodulateSamples = new TransformBlock<float[], byte[]>(DemodulateSamples);
            _validateCrc = new TransformBlock<byte[], DataAvailableEventArgs>(ValidateCrc);
            _dataAvailable = new ActionBlock<DataAvailableEventArgs>(OnDataAvailable);
            
            _demodulateSamples.LinkTo(_validateCrc, LinkOptions);
            _validateCrc.LinkTo(_dataAvailable, LinkOptions);
        }

        private DataAvailableEventArgs ValidateCrc(byte[] arg)
        {
            var res = Crc32Algorithm.IsValidWithCrcAtEnd(arg);
            Trace.WriteLine($"R4. Validating CRC: {res}.");
            // return res ? arg.Take(arg.Length - 4).ToArray() : null;
            return new DataAvailableEventArgs(arg.Take(arg.Length - 4).ToArray(), res);
        }

        private byte[] DemodulateSamples(float[] samples) => Modulator.Demodulate(samples, PayloadBytes + 4);

        // private int _idx = 0;

        private void AddSamples(IEnumerable<float> samples)
        {
            _buffer = _buffer.Concat(samples).ToArray();
            // Athernet.Utils.Debug.WriteTempWav(_buffer.ToArray(), $"test_{_idx++}.wav");
            var flag = true;

            while (flag)
            {
                Trace.Write($"{State}\r");

                switch (State)
                {
                    case ReceiveState.Syncing:
                        flag = SyncBuffer();
                        break;
                    case ReceiveState.Decoding:
                        flag = DecodeBuffer();
                        break;
                    case ReceiveState.Stopped:
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private bool DecodeBuffer()
        {
            Trace.WriteLine($"R2. Decoding buffer.");
            // CRC
            var frameSamples = ((PayloadBytes + 4) * 8 + 1) * Modulator.BitDepth + 100;

            if (_buffer.Length < frameSamples)
                return false;

            // var samples = _buffer.Skip(1).Take(frameSamples).ToArray(); // hack
            var samples = _buffer.Take(frameSamples).ToArray(); // hack
            // Athernet.Utils.Debug.WriteTempWav(samples, "recv_body.wav");
            _demodulateSamples.Post(samples);
            State = ReceiveState.Syncing;
            return true;
        }

        private bool SyncBuffer()
        {
            var detector = new CrossCorrelationDetector(Preamble);
            var pos = detector.Detect(_buffer.ToArray());

            if (pos != -1)
            {
                Trace.WriteLine($"R1. Found preamble at pos {pos}.");
                _buffer = _buffer.Skip(pos).ToArray();
                State = ReceiveState.Decoding;
                OnPacketDetected();
                return true;
            }
            
            _buffer = _buffer.TakeLast(Preamble.Length + detector.WindowSize).ToArray();
            return false;
        }

        public void StopReceive()
        {
            if (State == ReceiveState.Stopped)
            {
                return;
            }

            _recorder.StopRecording();
            _dataAvailable.Completion.Wait();
            _recorder.Dispose();
            State = ReceiveState.Stopped;
        }


        private void OnDataAvailable(DataAvailableEventArgs args)
        {
            Trace.WriteLine($"R5. New data available, length: {args.Data.Length}.");
            DataAvailable?.Invoke(this, args);
        }

        private void OnPacketDetected()
        {
            PacketDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}