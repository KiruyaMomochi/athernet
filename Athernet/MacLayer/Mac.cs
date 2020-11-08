using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Athernet.Modulators;
using Athernet.PhysicalLayer;
using Athernet.Preambles.PreambleBuilders;

namespace Athernet.MacLayer
{
    public class Mac
    {
        public byte Address { get; }

        public int PayloadBytes
        {
            get => _physical.PayloadBytes - 3;
            set => _physical.PayloadBytes = value + 3;
        }

        public bool SendAck { get; set; } = true;
        public bool NeedAck { get; set; } = true;

        public bool SendReTrans { get; set; } = false;


        public int PlayDeviceNumber
        {
            get => _physical.PlayDeviceNumber;
        }

        public int RecordDeviceNumber
        {
            get => _physical.RecordDeviceNumber;
        }

        private readonly Physical _physical;

        private readonly EventWaitHandle _ackEwh = new EventWaitHandle(false, EventResetMode.AutoReset);
        private byte[] _ackFrame;

        private readonly EventWaitHandle _pingEwh = new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool _isPing = false;
        private bool _isAck = false;
        
        public event EventHandler PacketDetected
        {
            add => _physical.PacketDetected += value;
            remove => _physical.PacketDetected -= value;
        }

        public event EventHandler<DataAvailableEventArgs> DataAvailable;

        public Mac(byte address, Physical physical)
        {
            Address = address;
            _physical = physical;
            // _ackFrame = new byte[PayloadBytes];
            _physical.DataAvailable += PhysicalOnDataAvailable;
            _physical.PacketDetected += (sender, args) =>
            {
                Trace.WriteLine($"Mp{Address} Set _pingEwh");
                if (_isAck == true)
                {
                    _isAck = false;
                    _ackEwh.Set();
                }
                else if (_isPing == false)
                {
                    SendPing(1);
                }
                else
                {
                    _pingEwh.Set();
                }
            };
        }

        public Mac(byte address, int payloadBytes, DpskModulator modulator, int playDeviceNumber = 0,
            int recordDeviceNumber = 0) :
            this(address, new Physical(payloadBytes + 3, modulator, playDeviceNumber, recordDeviceNumber)
            { Preamble = new WuPreambleBuilder(modulator.SampleRate, 0.015f).Build() })
        {
        }

        public Mac(byte address, int payloadBytes, int playDeviceNumber = 0, int recordDeviceNumber = 0,
            int sampleRate = 48000) :
            this(address, payloadBytes, new DpskModulator(sampleRate, 8000) { BitDepth = 3 }, playDeviceNumber,
                recordDeviceNumber)
        {
            Address = address;
        }

        private void PhysicalOnDataAvailable(object sender, PhysicalLayer.DataAvailableEventArgs e)
        {
            if (e.Data.Length == 0)
            {
                // if (_isPing)
                // {
                //     // _pingEwh.Set();
                // }
                // else
                //     SendPing(1);
            
                return;
            }
            var frame = MacFrame.Parse(e.Data);

            Trace.WriteLine($"Mx{Address} [{frame.Type}] {frame.Dest} <- {frame.Src}.");

            if (frame.Dest != Address)
            {
                return;
            }

            switch (frame.Type)
            {
                case MacType.Ack when NeedAck:
                    _ackFrame = frame.Frame;
                    _ackEwh.Set();
                    break;
                case MacType.Data when e.CrcResult:
                    if (SendAck)
                        ReplyWithAck(frame);
                    OnDataAvailable(frame.Payload.ToArray());
                    break;
                case MacType.Data when !e.CrcResult:
                    if (SendReTrans)
                        ReplyWithReTrans(frame);
                    break;
                case MacType.Data when !e.CrcResult:
                    Console.WriteLine($"M2{Address} CRC failed.");
                    break;
                case MacType.MacpingReq:
                    Trace.WriteLine($"Mr{Address} Received MacPing req");
                    _zeroPayload = new byte[0];
                    AddData(frame.Src, frame.Dest, MacType.MacpingReply, _zeroPayload);
                    break;
                case MacType.MacpingReply:
                    _pingEwh.Set();
                    break;
                case MacType.ReTrans when SendReTrans:
                    _ackFrame = frame.Frame;
                    _ackEwh.Set();
                    break;
            }
        }

        public void AddPayload(byte dest, byte[] payload)
        {
            if (payload.Length != PayloadBytes)
                throw new InvalidDataException($"bytes have length of {payload.Length}, should be {PayloadBytes}");

            var frame = new MacFrame(dest, Address, MacType.Data, payload);
            var failcnt = 0;
            
            while (true)
            {
                Backoff();
                AddData(frame);

                if (!NeedAck) return;

                Trace.WriteLine($"M1. Waiting for ACK.");
                // if (_ackEwh.WaitOne(AckTimeout) && MacFrame.Parse(_ackFrame).Type == MacType.Ack)
                //     return;
                _isAck = true;
                if (_ackEwh.WaitOne(AckTimeout))
                    return;

                if (failcnt++ >= 5)
                {
                    throw new ApplicationException("Link error");
                }
                Trace.WriteLine($"M2{Address} ACK not received or ReTransmit. Retransmitting.");
            }
        }

        private void Backoff()
        {
            while (!_physical.ChannelFree)
            {
                var waitTime = _backoff.Wait();
                Console.WriteLine($"Mb{Address} Wait {waitTime} times");
            }

            _backoff.Reset();
        }

        private void AddData(MacFrame macFrame)
        {
            Trace.WriteLine(
                $"Mx{Address} [{macFrame.Type}] {macFrame.Src} -> {macFrame.Dest}.");
            AddData(macFrame.Frame);
        }

        private void AddData(byte dest, byte src, MacType ack, Span<byte> payload) =>
            AddData(new MacFrame(dest, src, ack, payload));

        private void AddData(byte dest, byte src, MacType ack, byte[] payload) =>
            AddData(new MacFrame(dest, src, ack, payload));

        private void AddData(byte[] macFrame)
        {
            _physical.AddPayload(macFrame);
        }

        private BackoffHandler _backoff = new BackoffHandler();
        private static byte[] _zeroPayload = new byte[0];

        public int AckTimeout => _physical.FrameSamples / 48 + 600;

        private void SendPing(byte dest)
        {
            _physical.SendPing();
        }

        public TimeSpan Ping(byte dest)
        {
            var before = DateTime.Now;
            _isPing = true;
            SendPing(dest);
            _pingEwh.WaitOne();
            // _isPing = false;
            var after = DateTime.Now;
            return after - before;
        }

        private void ReplyWithAck(in MacFrame frame)
        {
            Backoff();
            // AddData(frame.Src, frame.Dest, MacType.Ack, frame.Payload);
            SendPing(1);
        }

        private void ReplyWithReTrans(in MacFrame frame)
        {
            Backoff();
            AddData(frame.Src, frame.Dest, MacType.ReTrans, frame.Payload);
        }

        public void StartReceive() => _physical.StartReceive();
        public void StopReceive() => _physical.StopReceive();

        private void OnDataAvailable(byte[] data) =>
            DataAvailable?.Invoke(this, new DataAvailableEventArgs() {Data = data});
    }
}