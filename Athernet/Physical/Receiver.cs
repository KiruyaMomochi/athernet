using Athernet.Modulators;
using Athernet.Preambles.PreambleDetectors;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace Athernet.Physical
{
    /// <summary>
    /// Receiver of the physical layer
    /// </summary>
    public sealed class Receiver
    {
        public int DeviceNumber { get; set; }
        public int SampleRate => Modulator.SampleRate;
        public float[] Preamble { get; set; } = new float[0];
        public IModulator Modulator { get; set; }
        public ReceiveState State { get; private set; } = ReceiveState.Stopped;

        /// <summary>
        /// Indicates new data is available
        /// </summary>
        public event EventHandler<DataAvailableEventArgs> DataAvailable;

        public event EventHandler PacketDetected;

        public Receiver(IModulator modulator)
        {
            Modulator = modulator;
        }

        public void StartReceive()
        {
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
        private ActionBlock<byte[]> _dataAvailable;
        private static readonly DataflowLinkOptions LinkOptions = new DataflowLinkOptions {PropagateCompletion = true};
        private IEnumerable<float> _buffer = new float[0];

        private void StartRecorder()
        {
            _recorder?.Dispose();
            _recorder = new WaveInEvent()
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1),
                DeviceNumber = DeviceNumber
            };
            _recorder.DataAvailable += (s, e) =>
                AddSamples(Utils.Audio.ToFloatBuffer(e.Buffer, e.BytesRecorded, _recorder.WaveFormat.BitsPerSample));
            _recorder.RecordingStopped += (s, e) => _demodulateSamples.Complete();
            _recorder.StartRecording();
        }

        private void InitReceiver()
        {
            _demodulateSamples = new TransformBlock<float[], byte[]>(DemodulateSamples);
            _dataAvailable = new ActionBlock<byte[]>(OnDataAvailable);
            _demodulateSamples.LinkTo(_dataAvailable, LinkOptions);
        }

        private byte[] DemodulateSamples(float[] samples) => Modulator.Demodulate(samples);

        private void AddSamples(IEnumerable<float> samples)
        {
            _buffer = _buffer.Concat(samples);
            var flag = true;

            while (flag)
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

        private bool DecodeBuffer()
        {
            var frameSamples = Modulator.FrameSamples;

            if (_buffer.Count() < frameSamples)
                return false;

            _demodulateSamples.Post(_buffer.Take(frameSamples).ToArray());
            State = ReceiveState.Syncing;
            return true;
        }

        private bool SyncBuffer()
        {
            var detector = new CrossCorrelationDetector(Preamble); 
            var pos = detector.Detect(_buffer.ToArray());
            
            if (pos != -1)
            {
                _buffer = _buffer.Skip(pos+1);
                State = ReceiveState.Decoding;
                OnPacketDetected();
                return true;
            }

            _buffer = _buffer.TakeLast(Preamble.Length + detector.WindowSize);
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


        private void OnDataAvailable(byte[] data)
        {
            DataAvailable?.Invoke(this, new DataAvailableEventArgs() {Data = data});
        }

        private void OnPacketDetected()
        {
            PacketDetected?.Invoke(this, EventArgs.Empty);
        }
    }

    public class DataAvailableEventArgs : EventArgs
    {
        public byte[] Data { get; internal set; }
    }
}