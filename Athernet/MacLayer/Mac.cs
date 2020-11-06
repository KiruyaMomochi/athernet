using System;
using System.Diagnostics;
using System.IO;
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
        private readonly byte[] _ackPayload;

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
            _ackPayload = new byte[PayloadBytes];
            _physical.DataAvailable += PhysicalOnDataAvailable;
        }

        public Mac(byte address, int payloadBytes, IModulator modulator, int playDeviceNumber = 0, int recordDeviceNumber = 0) :
            this(address, new Physical(payloadBytes + 3, modulator, playDeviceNumber, recordDeviceNumber)
                {Preamble = new WuPreambleBuilder(modulator.SampleRate, 0.015f).Build()})
        {
        }

        public Mac(byte address, int payloadBytes, int playDeviceNumber = 0, int recordDeviceNumber = 0, int sampleRate = 48000) :
            this(address, payloadBytes, new DpskModulator(sampleRate, 8000){BitDepth = 3}, playDeviceNumber, recordDeviceNumber)
        {
            Address = address;
        }

        private void PhysicalOnDataAvailable(object sender, PhysicalLayer.DataAvailableEventArgs e)
        {
            var frame = MacFrame.Parse(e.Data);
            
            Trace.WriteLine($"Mx! {Address}: [{frame.Type}] {frame.Src} -> {frame.Dest}.");


            if (frame.Dest != Address)
            {
                 return;
            }

            switch (frame.Type)
            {
                case MacType.Ack when NeedAck:
                    Trace.WriteLine($"M2. ACK received.");
                    frame.Payload.CopyTo(_ackPayload);
                    _ackEwh.Set();
                    break;
                case MacType.Data when e.CrcResult:
                    if (SendAck)
                        ReplyWithAck(frame);
                    OnDataAvailable(frame.Payload.ToArray());
                    break;
                case MacType.MacpingReq:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AddData(byte dest, byte[] payload)
        {
            if (payload.Length != PayloadBytes)
                throw new InvalidDataException($"bytes have length of {payload.Length}, should be {PayloadBytes}");

            var frame = new MacFrame(dest, Address, MacType.Data, payload);
            
            while (true)
            {
                _physical.AddPayload(frame.Frame);

                if (!NeedAck) return;

                Trace.WriteLine($"M1. Waiting for ACK.");
                if (_ackEwh.WaitOne(AckTimeout))
                {
                    return;
                }

                Trace.WriteLine($"M2- ACK not received. Retransmitting.");
            }
        }

        public int AckTimeout { get; set; } = 500;

        private void ReplyWithAck(in MacFrame frame)
        {
            Trace.WriteLine($"Ma. Replying ACK.");
            var ackFrame = new MacFrame(frame.Src, frame.Dest, MacType.Ack, frame.Payload);
            _physical.AddPayload(ackFrame.Frame);
        }

        public void StartReceive() => _physical.StartReceive();
        public void StopReceive() => _physical.StopReceive();

        private void OnDataAvailable(byte[] data)
        {
            DataAvailable?.Invoke(this, new DataAvailableEventArgs() {Data = data});
        }
    }
}