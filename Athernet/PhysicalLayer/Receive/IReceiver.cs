using System;
using Athernet.PhysicalLayer.Receive.Demodulator;

namespace Athernet.PhysicalLayer.Receive
{
    /// <summary>
    /// The interface of receiver.
    /// </summary>
    internal interface IReceiver
    {
        /// <summary>
        /// Start receive new data.
        /// </summary>
        void StartReceive();

        /// <summary>
        /// Stop receive new data.
        /// </summary>
        void StopReceive();

        ///// <summary>
        ///// True if the channel is free.
        ///// </summary>
        //bool ChannelFree { get; }

        /// <summary>
        /// The receive state.
        /// </summary>
        ReceiveState State { get; }

        /// <summary>
        /// The max number of bytes in a payload.
        /// </summary>
        int MaxPayloadBytes { get; set; }

        /// <summary>
        /// The demodulator class.
        /// </summary>
        IDemodulator Demodulator { get; set; }

        float[] Preamble { get; set; }
        int DeviceNumber { get; set; }

        event EventHandler PacketDetected;
        event EventHandler<DataAvailableEventArgs> DataAvailable;
    }
}