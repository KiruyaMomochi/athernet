using System;
using System.IO;
using Athernet.PhysicalLayer.PreambleBuilder;
using Athernet.PhysicalLayer.Receive;
using Athernet.PhysicalLayer.Receive.Rx;
using Athernet.PhysicalLayer.Receive.Rx.Demodulator;
using Athernet.PhysicalLayer.Transmit;
using Athernet.PhysicalLayer.Transmit.Modulator;
using Athernet.Utils;
using NAudio.Wave;

namespace Athernet.PhysicalLayer
{
    /// <summary>
    /// The physical layer of Athernet
    /// </summary>
    public sealed class Physical
    {
        private readonly ReceiverRx _receiver;
        private readonly Transmitter _transmitter;


        /// <summary>
        /// Number of the device to play the sound.
        /// </summary>
        public int PlayDeviceNumber => _transmitter.DeviceNumber;

        /// <summary>
        /// Number of the device to record the sound.
        /// </summary>
        public int RecordDeviceNumber => _receiver.DeviceNumber;

        /// <summary>
        /// The length of payload in bytes.
        /// </summary>
        public int PayloadBytes => _receiver.MaxFrameBytes - 5;

        /// <summary>
        /// The transmit state of the physical layer.
        /// </summary>
        public TransmitState TransmitState => _transmitter.State;
        /// <summary>
        /// The receive state of the physical layer.
        /// </summary>
        public ReceiveState ReceiveState => _receiver.State;

        /// <summary>
        /// Returns true when recording.
        /// </summary>
        public bool IsRecording => _receiver.State != ReceiveState.Stopped;

        /// <summary>
        /// A new payload data is available.
        /// </summary>
        public event EventHandler<DataAvailableEventArgs> DataAvailable
        {
            add => _receiver.DataAvailable += value;
            remove => _receiver.DataAvailable -= value;
        }

        /// <summary>
        /// Indicate A new packet is found by detecting the frame. 
        /// </summary>
        public event EventHandler PacketDetected
        {
            add => _receiver.PacketDetected += value;
            remove => _receiver.PacketDetected -= value;
        }

        /// <summary>
        /// Indicate the playing process is stopped.
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayStopped
        {
            add => _transmitter.PlayStopped += value;
            remove => _transmitter.PlayStopped -= value;
        }

        /// <summary>
        /// Indicate the playing process is complete.
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlayComplete
        {
            add => _transmitter.PlayComplete += value;
            remove => _transmitter.PlayComplete -= value;
        }

        public Physical(int playDeviceNumber, int recordDeviceNumber, int maxDataBytes, IModulator modulator,
                        IDemodulatorRx demodulatorRx, WuPreambleBuilder preambleBuilder)
        {
            MaxDataBytes = maxDataBytes;

            _transmitter = new Transmitter(playDeviceNumber, modulator, preambleBuilder);
            _receiver = new ReceiverRx(recordDeviceNumber, MaxDataBytes + 4 + 1, demodulatorRx, preambleBuilder);
        }


        public Physical(int playDeviceNumber, int recordDeviceNumber, int maxDataBytes)
        {
            MaxDataBytes = maxDataBytes;

            _transmitter = new Transmitter(playDeviceNumber);
            _receiver = new ReceiverRx(recordDeviceNumber, MaxDataBytes + 4 + 1);
        }

        public int MaxDataBytes { get; }

        public int BufferLength
        {
            get => _transmitter.BufferLength;
            set => _transmitter.BufferLength = value;
        }

        /// <summary>
        /// Play a payload.
        /// </summary>
        /// <param name="payload">The payload to be played, whose length should be <c>PayloadBytes</c>.</param>
        /// <exception cref="InvalidDataException">Thrown when the payload length is not equal to <c>PayloadBytes</c>.</exception>
        public void AddPayload(byte[] payload)
        {
            if (payload.Length > PayloadBytes)
            {
                throw new InvalidDataException($"bytes have length of {payload.Length}, should be less or equal than {PayloadBytes}");
            }
            Debug.UpdateTimeSpan();
            _transmitter.AddPayload(payload);
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        public void CompletePlaying() => _transmitter.Complete();

        /// <summary>
        /// Start receive new frames.
        /// When new frame arrived, DataAvailable will raise.
        /// </summary>
        public void StartReceive() => _receiver.StartReceive();

        /// <summary>
        /// Stop receive new frames.
        /// When new frame arrived, DataAvailable will raise.
        /// </summary>
        public void StopReceive() => _receiver.StopReceive();

        //internal bool ChannelFree => _receiver.ChannelFree;

        public void SendPing()
        {
            _transmitter.SendPing();
        }
    }
}