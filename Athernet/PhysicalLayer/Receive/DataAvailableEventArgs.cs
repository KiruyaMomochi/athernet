using System;

namespace Athernet.PhysicalLayer.Receive
{
    public class DataAvailableEventArgs : EventArgs
    {
        public DataAvailableEventArgs(byte[] data, bool crcResult)
        {
            Data = data;
            CrcResult = crcResult;
        }

        public byte[] Data { get; }
        public bool CrcResult { get; }
    }
}