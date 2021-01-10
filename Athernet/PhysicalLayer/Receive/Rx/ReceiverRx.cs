using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Athernet.PhysicalLayer.PreambleBuilder;
using Athernet.PhysicalLayer.Receive.PreambleDetector;
using Athernet.PhysicalLayer.Receive.Rx.Demodulator;
using Force.Crc32;
using NAudio.Wave;

namespace Athernet.PhysicalLayer.Receive.Rx
{
    /// <summary>
    /// Receiver of the physical layer
    /// </summary>
    public sealed class ReceiverRx
    {
        public int DeviceNumber => _recorder.DeviceNumber;

        public int MaxFrameBytes => _demodulator.MaxFrameBytes;
        private readonly float[] _preamble;

        private readonly IDemodulatorRx _demodulator;

        public ReceiveState State { get; private set; } = ReceiveState.Stopped;

        /// <summary>
        /// Indicates new data is available
        /// </summary>
        public event EventHandler<DataAvailableEventArgs> DataAvailable;

        public event EventHandler PacketDetected;

        public ReceiverRx(int deviceNumber, int maxFrameBytes, IDemodulatorRx demodulator,
            WuPreambleBuilder preambleBuilder)
        {
            _demodulator = demodulator;
            _demodulator.MaxFrameBytes = maxFrameBytes;

            preambleBuilder.SampleRate = _demodulator.SampleRate;
            _preamble = preambleBuilder.Build();
            InitReceiver(deviceNumber);
            InitRx();
        }

        public ReceiverRx(int deviceNumber, int maxFrameBytes)
        {
            _demodulator = new DpskDemodulatorRx
            {
                BitDepth = 3,
                SampleRate = 48000,
                Channel = 1,
                MaxFrameBytes = maxFrameBytes
            };
            _preamble = new WuPreambleBuilder(_demodulator.SampleRate, 0.015f).Build();

            InitReceiver(deviceNumber);
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

        private int WindowSize => 2 * _preamble.Length + Math.Max(_detector.WindowSize, FrameSamples + 100);
        private int FrameSamples => _demodulator.FrameSamples(MaxFrameBytes);

        private void InitReceiver(int deviceNumber)
        {
            _detector = new CrossCorrelationDetector(_preamble);

            _recorder = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(_demodulator.SampleRate, 1)
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
                .Window(WindowSize, _preamble.Length)
                .SubscribeOn(TaskPoolScheduler.Default)
                .Select(GetFrame)
                // .Subscribe(x => x.Subscribe(y => Console.Write("!"), () => { Console.Write(".");}));
                .Merge()
                // .Subscribe(x => x.Subscribe(_ => Console.Write("."), _ => Console.Write("E"), () => Console.Write("C")));
                .Subscribe(x =>
                {
                    var res = x.ToArray();
                    OnDataAvailable(ValidateCrc(res));
                });
        }

        private IObservable<IEnumerable<byte>> GetFrame(IObservable<float> observable)
        {
            // TODO: use correct Rx way

            var sub = observable.Replay().RefCount();

            var last = sub.Take(2 * _preamble.Length + _detector.WindowSize)
                .ToArray()
                .Select(x =>
                {
                    var pos = _detector.Detect(x);
                    if (pos != -1)
                    {
                        Debug.WriteLine($"Detected x[{x.Length}] at {pos} in {WindowSize}");
                    }

                    return pos;
                });

            var res = last.Where(x => x != -1).Select(x => _demodulator.Demodulate(sub.Skip(x))).Merge();

            return res;
        }

        private static DataAvailableEventArgs ValidateCrc(byte[] arg)
        {
            if (arg.Length == 0) return new DataAvailableEventArgs(arg, true);

            var res = Crc32Algorithm.IsValidWithCrcAtEnd(arg);
            Debug.WriteLine($"R4. Validating CRC: {res}.");
            return new DataAvailableEventArgs(arg.Take(arg.Length - 4).ToArray(), res);
        }

        public void StopReceive()
        {
            _recorder.StopRecording();
            State = ReceiveState.Stopped;
        }

        private void OnDataAvailable(DataAvailableEventArgs args)
        {
            Trace.WriteLine($"R5. New data available, length: {args.Data.Length}, Crc: {args.CrcResult}.");
            DataAvailable?.Invoke(this, args);
        }

        private void OnPacketDetected()
        {
            Debug.WriteLine($"Rx{DeviceNumber} Packet Detected");
            PacketDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}