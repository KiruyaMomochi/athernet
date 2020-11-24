using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Athernet.Demodulator;
using Athernet.Modulator;
using Athernet.PreambleDetector;
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
        public float[] Preamble { get; set; } = Array.Empty<float>();
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
            InitReceiver();
            InitRx();
        }

        private readonly object _lock = new();

        /// <summary>
        /// Start receive new samples.
        /// If the receiving is already started, this function has no effect.
        /// </summary>
        public void StartReceive()
        {
            lock (_lock)
            {
                Debug.WriteLine("-R- Starting receiver.", "ReceiverRx");
                if (State != ReceiveState.Stopped)
                    return;
                State = ReceiveState.Syncing;
            }

            StartRecorder();
        }

        private WaveInEvent _recorder;
        private IObservable<EventPattern<WaveInEventArgs>> _dataReceived;

        private void StartRecorder()
        {
            Debug.WriteLine("-R- Starting recorder.", "ReceiverRx");

            _recorder.StartRecording();
        }

        // private void RecorderOnDataAvailable(object sender, WaveInEventArgs e)
        // {
        //     var floatBuffer = Utils.Audio.ToFloatBuffer(e.Buffer, e.BytesRecorded, _recorder.WaveFormat.BitsPerSample)
        //         .ToArray();
        //     Task.Run(() => _channelPower = floatBuffer.TakeLast(100).Select(x => x * x).Average());
        // }

        // TODO: CSMA
        //private float _channelPower;
        //public bool ChannelFree => _channelPower < 0.02;
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
            _dataReceived
                .Select(e =>
                    Utils.Audio.ToFloatBuffer(e.EventArgs.Buffer, e.EventArgs.BytesRecorded,
                        _recorder.WaveFormat.BitsPerSample))
                .SelectMany(x => x)
                .Window(WindowSize, Preamble.Length)
                .SubscribeOn(TaskPoolScheduler.Default)
                .Select(SkipToPreamble)
                // .Subscribe(x => x.Subscribe(y => Console.Write("!"), () => { Console.Write(".");}));
                .Merge()
                // .Subscribe(x => x.Subscribe(_ => Console.Write("."), _ => Console.Write("E"), () => Console.Write("C")));
                .Subscribe(_ => Console.Write("."));
        }

        private float[] SkipToPreamble(IList<float> observable)
        {
            var arr = _detector.Detect(observable.Take(2 * Preamble.Length + _detector.WindowSize).ToArray());
            return arr == -1 ? null : observable.Skip(arr).Take(FrameSamples).ToArray();
        }

        private IObservable<byte> SkipToPreamble(IObservable<float> observable)
        {
            var ret = observable.Publish().RefCount();
            var samples = ret.Take(2 * Preamble.Length + _detector.WindowSize)
                .ToArray()
                .Select(x => _detector.Detect(x))
                .Where(pos => pos != -1)
                .Select(pos => ret.Skip(pos))
                .Select(x => new PskDemodulatorRx(x, Modulator.NewCarrier()) {BitDepth = Modulator.BitDepth}.Payload)
                .Merge();
            // var core = new PskCore(samples);
            // return core.Payload;
            // return samples;
            return samples;
        }

        private IObservable<IObservable<byte>> ToList(IObservable<IObservable<float>> samples, int maxFrameBytes)
        {
            return samples.Select(
                y =>
                {
                    var core = new DpskCore(Modulator.NewCarrier(), Modulator.BitDepth, maxFrameBytes);
                    y.Subscribe(
                        x => core.Add(x),
                        (e) => core.Error(e),
                        () => core.Complete());
                    return core.Payload;
                }
            );
            // var li = new List<float>();
        }

        private static DataAvailableEventArgs ValidateCrc(byte[] arg)
        {
            if (arg.Length == 0)
            {
                return new DataAvailableEventArgs(arg, true);
            }

            var res = Crc32Algorithm.IsValidWithCrcAtEnd(arg);
            Debug.WriteLine($"R4. Validating CRC: {res}.");
            return new DataAvailableEventArgs(arg.Take(arg.Length - 4).ToArray(), res);
        }

        private byte[] DemodulateSamples(float[] samples)
        {
            var ret = Modulator.Demodulate(samples, PayloadBytes + 4);
            return ret;
        }

        // private IEnumerable<byte> DemodulateSamples(IObservable<float> samples)
        // {
        //     var ret = Modulator.DemodulateRx(samples, PayloadBytes + 4);
        //     return ret;
        // }

        // private IObservable<float> DemodulateSamples(IObservable<float> samples)
        // {
        //     var ret = Modulator.Demodulate(samples, PayloadBytes + 4);
        //     return ret;
        // }

        public void StopReceive()
        {
            _recorder.StopRecording();
            State = ReceiveState.Stopped;
        }

        private void OnDataAvailable(DataAvailableEventArgs args)
        {
            Debug.WriteLine($"R5. New data available, length: {args.Data.Length}.");
            DataAvailable?.Invoke(this, args);
        }

        private void OnPacketDetected()
        {
            Debug.WriteLine($"Rx{DeviceNumber} Packet Detected");
            PacketDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}