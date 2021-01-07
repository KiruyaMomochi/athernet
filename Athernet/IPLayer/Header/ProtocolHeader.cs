

// CREDIT: This namespace is modified based from the following source:
// https://www.winsocketdotnetworkprogramming.com/clientserversocketnetworkcommunication8chap.html

namespace Athernet.IPLayer.Header
{
    /// <summary>
    /// The ProtocolHeader class is the base class for all protocol header classes.
    /// It defines one abstract method that each class must implement which returns
    /// a byte array representation of the protocl packet. It also provides common
    /// routines for building the entire protocol packet as well as for computing 
    /// checksums on packets.
    /// 
    /// </summary>
    public abstract class ProtocolHeader
    {
        public abstract int HeaderLength { get; }    // Length of header

        /// <summary>
        /// This is a simple method for computing the 16-bit one's complement
        /// checksum of a byte buffer. The byte buffer will be padded with
        /// a zero byte if an uneven number.
        /// </summary>
        /// <param name="payLoad">Byte array to compute checksum over</param>
        /// <returns></returns>
        protected static ushort ComputeChecksum(byte[] payLoad)
        {
            uint xsum = 0;
            ushort shortval = 0;

            // Sum up the 16-bits
            for (var i = 0; i < payLoad.Length / 2; i++)
            {
                var hiword = (ushort) (payLoad[i * 2] << 8);
                var loword = (ushort) payLoad[i * 2 + 1];

                shortval = (ushort) (hiword | loword);

                xsum += shortval;
            }

            // Pad if necessary
            if (payLoad.Length % 2 != 0)
            {
                xsum += (ushort)(payLoad[^1] << 8);
            }

            xsum = (xsum >> 16) + (xsum & 0xFFFF);
            xsum += xsum >> 16;
            shortval = (ushort) ~xsum;

            return shortval;
        }
    }
}