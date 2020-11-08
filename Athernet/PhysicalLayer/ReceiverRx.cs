using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
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
    public sealed class ReceiverRx : IReceiver
    {
        public int DeviceNumber { get; set; }
        public int PayloadBytes { get; set; }
        public float[] Preamble { get; set; } = new float[0];
        public DpskModulator Modulator { get; set; }
        public ReceiveState State { get; private set; } = ReceiveState.Stopped;

        public int SampleRate => Modulator.SampleRate;

        /// <summary>
        /// Indicates new data is available
        /// </summary>
        public event EventHandler<DataAvailableEventArgs> DataAvailable;

        public event EventHandler PacketDetected;

        public ReceiverRx(DpskModulator modulator, int deviceNumber = 0)
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
            InitRx();
            StartRecorder();
        }

        private WaveInEvent _recorder;
        private float[] _buffer = new float[0];
        IObservable<EventPattern<WaveInEventArgs>> _dataReceived;

        private void StartRecorder()
        {
            Trace.WriteLine($"-R- Starting recorder.");

            _recorder.StartRecording();
        }

        // private void RecorderOnDataAvailable(object sender, WaveInEventArgs e)
        // {
        //     var floatBuffer = Utils.Audio.ToFloatBuffer(e.Buffer, e.BytesRecorded, _recorder.WaveFormat.BitsPerSample)
        //         .ToArray();
        //     Task.Run(() => _channelPower = floatBuffer.TakeLast(100).Select(x => x * x).Average());
        // }

        private float _channelPower;
        public bool ChannelFree => _channelPower < 0.02;
        private CrossCorrelationDetector _detector;

        private int WindowSize => 2 * Preamble.Length + Math.Max(_detector.WindowSize, FrameSamples);

        private int FrameSamples => Modulator.FrameSamples(PayloadBytes + 4) + 100;

        private void InitReceiver()
        {
            _detector = new CrossCorrelationDetector(Preamble);

            _recorder = new WaveInEvent()
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1),
                DeviceNumber = DeviceNumber,
                BufferMilliseconds = 10
            };

            _dataReceived = Observable.FromEventPattern<EventHandler<WaveInEventArgs>, WaveInEventArgs>(
                h => _recorder.DataAvailable += h,
                h => _recorder.DataAvailable -= h
            );

        }

        private void InitRx()
        {
            var samplesReceived =
                _dataReceived.Select(e =>
                    Utils.Audio.ToFloatBuffer(e.EventArgs.Buffer, e.EventArgs.BytesRecorded,
                        _recorder.WaveFormat.BitsPerSample));
            var sampleReceived = samplesReceived.SelectMany(x => x).Publish().RefCount();

            var preambleScanWindow1 =
                sampleReceived.Buffer(2 * Preamble.Length + _detector.WindowSize, Preamble.Length);
            preambleScanWindow1
                .Select(SkipToPreamble)
                .Where(x => x != null)
                .Select(x => Modulator.Demodulate(x, 1))
                .Where(x => x.Length == 0)
                .Subscribe(_ => OnPacketDetected());
            
            var preambleScanWindow =
                sampleReceived.
                    Buffer(WindowSize, Preamble.Length)
                    .Where(x => x.AsParallel().Select(y => y * y).Sum() > 100);
            var frames = preambleScanWindow
                .Select(SkipToPreamble)
                .Where(x => x != null);
            var demodulatedFrames = frames.Select(DemodulateSamples).Where(x => x.Length == PayloadBytes + 4);
            var payloads = demodulatedFrames.Select(ValidateCrc);
            payloads.Subscribe(OnDataAvailable);
        }

    private float[] SkipToPreamble(IList<float> observable)
        {
            var arr = _detector.Detect(observable.Take(2 * Preamble.Length + _detector.WindowSize).ToArray());
            return arr == -1 ? null : observable.Skip(arr).Take(FrameSamples).ToArray();
        }

        private static DataAvailableEventArgs ValidateCrc(byte[] arg)
        {
            if (arg.Length == 0)
            {
                return new DataAvailableEventArgs(arg, true);
            }
            var res = Crc32Algorithm.IsValidWithCrcAtEnd(arg);
            Trace.WriteLine($"R4. Validating CRC: {res}.");
            return new DataAvailableEventArgs(arg.Take(arg.Length - 4).ToArray(), res);
        }

        private byte[] DemodulateSamples(float[] samples)
        {
            var ret = Modulator.Demodulate(samples, PayloadBytes + 4);
            // if (ret.Length == 0)
            // {
            //     OnPacketDetected();
            // }

            return ret;
        }

        public void StopReceive()
        {
            _recorder.StopRecording();
            State = ReceiveState.Stopped;
        }

        private void OnDataAvailable(DataAvailableEventArgs args)
        {
            Trace.WriteLine($"R5. New data available, length: {args.Data.Length}.");
            DataAvailable?.Invoke(this, args);
        }

        private void OnPacketDetected()
        {
            Trace.WriteLine($"Rx{DeviceNumber} Packet Detected");
            PacketDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}