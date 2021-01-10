using System;

namespace Athernet.IPLayer.Packet
{
    [Flags]
    public enum TcpFlags: ushort
    {
        Fin = 1 << 0,
        Syn = 1 << 1,
        Reset = 1 << 2,
        Push = 1 << 3,
        Acknowledgment = 1 << 4,
        Urgent = 1 << 5,
        EcnEcho = 1 << 6,
        CongestionWindowReduced = 1 << 7,
        Nonce = 1 << 8
    }
}