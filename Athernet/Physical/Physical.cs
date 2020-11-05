using Athernet.Modulators;
using System;

namespace Athernet.Physical
{
    /// <summary>
    /// The physical layer of Athernet
    /// </summary>
    public sealed class Physical
    {
        private readonly Receiver _receiver;
        private readonly Transmitter _transmitter;

        /// <summary>
        /// The channel to play acoustic signal
        /// </summary>
        public Channel PlayChannel
        {
            get => _transmitter.Channel;
            set => _transmitter.Channel = value;
        }

        public int PlayDeviceNumber
        {
            get => _transmitter.DeviceNumber;
            set => _transmitter.DeviceNumber = value;
        }

        public int RecordDeviceNumber
        {
            get => _receiver.DeviceNumber;
            set => _receiver.DeviceNumber = value;
        }

        /// <summary>
        /// The preamble to prepend before the frame body.
        /// </summary>
        public float[] Preamble
        {
            get => _receiver.Preamble;
            set => _receiver.Preamble = _transmitter.Preamble = value;
        }

        /// <summary>
        /// The modulator that should be used for modulation and demodulation
        /// </summary>
        public IModulator Modulator
        {
            get => _receiver.Modulator; 
            set => _receiver.Modulator = _transmitter.Modulator = value;
        }

        public int PayloadBytes
        {
            get => _receiver.PayloadBytes;
            set => _receiver.PayloadBytes = value;
        }

        /// <summary>
        /// The sample rate of acoustic signal
        /// </summary>
        /// <remarks>
        /// For the best result, use the sample rate that is supported by device
        /// </remarks>
        public int SampleRate => Modulator.SampleRate;

        public TransmitState TransmitState => _transmitter.State;
        public ReceiveState ReceiveState => _receiver.State;
        
        public bool IsRecording => _receiver.State != ReceiveState.Stopped;

        public event EventHandler<DataAvailableEventArgs> DataAvailable
        {
            add => _receiver.DataAvailable += value;
            remove => _receiver.DataAvailable -= value;
        }
        public event EventHandler PacketDetected
        {
            add => _receiver.PacketDetected += value;
            remove => _receiver.PacketDetected -= value;
        }
        public event EventHandler PlayStopped
        {
            add => _transmitter.PlayStopped += value;
            remove => _transmitter.PlayStopped -= value;
        }

        public Physical(IModulator modulator)
        {
            _transmitter = new Transmitter(modulator);
            _receiver = new Receiver(modulator);
        }

        public void Play(byte[] bytes)
        {
            if (bytes.Length != PayloadBytes)
            {
                throw new System.IO.InvalidDataException($"bytes have length of {bytes.Length}, should be {PayloadBytes}");
            }
            _transmitter.Play(bytes);
        }

        public void StartReceive() => _receiver.StartReceive();

        public void StopReceive() => _receiver.StopReceive();
        
    }
}