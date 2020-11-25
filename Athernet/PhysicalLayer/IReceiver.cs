using System;
using Athernet.Demodulator;

namespace Athernet.PhysicalLayer
{
    internal interface IReceiver
    {
        void StartReceive();
        void StopReceive();
        bool ChannelFree { get; }
        ReceiveState State { get; }
        int PayloadBytes { get; set; }
        IDemodulator Demodulator { get; set; }
        float[] Preamble { get; set; }
        int DeviceNumber { get; set; }

        event EventHandler PacketDetected;
        event EventHandler<DataAvailableEventArgs> DataAvailable;
    }
}