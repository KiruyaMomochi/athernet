using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Athernet.PhysicalLayer;

namespace Athernet.MacLayer
{
    /// <summary>
    /// The MAC layer.
    /// </summary>
    /// <remarks>
    /// Neither the MAC layer nor the Physics layer will do split for you.
    /// </remarks>
    public class Mac
    {
        public Dictionary<IPEndPoint, IPEndPoint> NatTable;

        public byte Address { get; }

        public int MaxDataBytes => _physical.MaxDataBytes;

        public bool SendAck { get; set; } = true;
        public bool NeedAck { get; set; } = true;

        public bool SendReTrans { get; set; } = false;

        public int PlayDeviceNumber => _physical.PlayDeviceNumber;
        public int RecordDeviceNumber => _physical.RecordDeviceNumber;

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
            SubscribePhysical();
            InitNatTable();
        }

        public Mac(byte address, int playDeviceNumber = 0, int recordDeviceNumber = 0, int maxDataBytes = 1020)
        {
            _physical = new Physical(playDeviceNumber, recordDeviceNumber, maxDataBytes + 3);
            Address = address;
            SubscribePhysical();
            InitNatTable();   
        }

        private void InitNatTable()
        {
            NatTable = new Dictionary<IPEndPoint, IPEndPoint>
            {
                {IPEndPoint.Parse("192.168.1.1:1234"), IPEndPoint.Parse("10.20.223.177:2333")}
            };
        }

        private void SubscribePhysical()
        {
            _physical.DataAvailable += PhysicalOnDataAvailable;
            _physical.PacketDetected += (sender, args) =>
            {
                Trace.WriteLine($"Mp{Address} Set _pingEwh");
                if (_isAck)
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

        private void PhysicalOnDataAvailable(object sender, PhysicalLayer.Receive.DataAvailableEventArgs e)
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

            Debug.WriteLine($"Mx{Address} [{frame.Type}] {frame.Dest} <- {frame.Src}.");

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
                case MacType.Nack when SendReTrans:
                    _ackFrame = frame.Frame;
                    _ackEwh.Set();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AddPayload(byte dest, byte[] payload)
        {
            if (payload.Length > MaxDataBytes)
                throw new InvalidDataException($"bytes have length of {payload.Length}, should be not greater than {MaxDataBytes}");

            var frame = new MacFrame(dest, Address, MacType.Data, payload);
            var failCount = 0;
            
            while (true)
            {
                //Backoff();
                AddData(frame);

                if (!NeedAck) return;

                Trace.WriteLine($"M1. Waiting for ACK.");
                // if (_ackEwh.WaitOne(AckTimeout) && MacFrame.Parse(_ackFrame).Type == MacType.Ack)
                //     return;
                _isAck = true;
                if (_ackEwh.WaitOne(AckTimeout))
                    return;

                if (failCount++ >= 5)
                {
                    throw new ApplicationException("Link error");
                }
                Trace.WriteLine($"M2{Address} ACK not received or ReTransmit. Retransmitting.");
            }
        }

        //private void Backoff()
        //{
        //    while (!_physical.ChannelFree)
        //    {
        //        var waitTime = _backOff.Wait();
        //        Console.WriteLine($"Mb{Address} Wait {waitTime} times");
        //    }

        //    _backOff.Reset();
        //}

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

        private BackOffHandler _backOff = new BackOffHandler();
        private static byte[] _zeroPayload = new byte[0];

        // TODO: change timeout
        public int AckTimeout => _physical.MaxDataBytes + 600;

        private void SendPing(byte dest)
        {
            _physical.SendPing();
        }

        public TimeSpan? Ping(byte dest)
        {
            var before = DateTime.Now;
            _isPing = true;
            SendPing(dest);

            if (_pingEwh.WaitOne(1000))
            {
                // _isPing = false;
                var after = DateTime.Now;
                return after - before;
            }

            return null;
        }

        private void ReplyWithAck(in MacFrame frame)
        {
            //Backoff();
            // AddData(frame.Src, frame.Dest, MacType.Ack, frame.Payload);
            SendPing(1);
        }

        private void ReplyWithReTrans(in MacFrame frame)
        {
            //Backoff();
            AddData(frame.Src, frame.Dest, MacType.Nack, frame.Payload);
        }

        public void StartReceive() => _physical.StartReceive();
        public void StopReceive() => _physical.StopReceive();

        private void OnDataAvailable(byte[] data) =>
            DataAvailable?.Invoke(this, new DataAvailableEventArgs() {Data = data});
    }
}