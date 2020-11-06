using Athernet.Modulators;
using System;
using System.IO;
using NAudio.Wave;

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
        /// The channel to play acoustic signal.
        /// </summary>
        public Channel PlayChannel
        {
            get => _transmitter.Channel;
            set => _transmitter.Channel = value;
        }

        /// <summary>
        /// Number of the device to play the sound.
        /// </summary>
        public int PlayDeviceNumber
        {
            get => _transmitter.DeviceNumber;
            set => _transmitter.DeviceNumber = value;
        }

        /// <summary>
        /// Number of the device to record the sound.
        /// </summary>
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
        /// The modulator that should be used for modulation and demodulation.
        /// </summary>
        private IModulator Modulator
        {
            get => _receiver.Modulator; 
            set => _receiver.Modulator = _transmitter.Modulator = value;
        }

        /// <summary>
        /// The length of payload in bytes.
        /// </summary>
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
        /// Physical layer constructor.
        /// </summary>
        /// <param name="modulator">The modulator to use with.</param>
        /// <param name="payloadBytes">The length of payload in bytes.</param>
        public Physical(IModulator modulator, int payloadBytes)
        {
            _transmitter = new Transmitter(modulator);
            _receiver = new Receiver(modulator);
            PayloadBytes = payloadBytes;
        }
        
        /// <summary>
        /// Play a payload.
        /// </summary>
        /// <param name="payload">The payload to be played, whose length should be <c>PayloadBytes</c>.</param>
        /// <exception cref="InvalidDataException">Thrown when the payload length is not equal to <c>PayloadBytes</c>.</exception>
        public void AddPayload(byte[] payload)
        {
            if (payload.Length != PayloadBytes)
            {
                throw new InvalidDataException($"bytes have length of {payload.Length}, should be {PayloadBytes}");
            }
            _transmitter.AddPayload(payload);
        }

        /// <summary>
        /// Start playing. If no payload provided, zero signal will be played.
        /// </summary>
        public void StartPlaying() => _transmitter.Play();
        
        /// <summary>
        /// Stop playing.
        /// </summary>
        public void StopPlaying() => _transmitter.Stop(); 

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
        
    }
}