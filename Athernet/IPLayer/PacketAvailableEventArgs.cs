using System;
using Athernet.IPLayer.Packet;

namespace Athernet.IPLayer
{
    public class PacketAvailableEventArgs : EventArgs
    {
        public Ipv4Packet Packet { get; internal set; }
    }
}