using System;

namespace Athernet.MacLayer
{
    public class DataAvailableEventArgs : EventArgs
    {
        public byte[] Data { get; internal set; }
    }
}
