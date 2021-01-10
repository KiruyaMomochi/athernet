using System;

namespace Athernet.IPLayer.Header
{
    public abstract class TransportHeader: ProtocolHeader
    {
        /// <summary>
        /// This abstracted method returns a byte array that is the protocl
        /// header and the payload. This is used by teh BuildPacket method
        /// to build the entire packet which may consist of multiple headers
        /// and data payload.
        /// </summary>
        /// <param name="payLoad">The byte array of the data encapsulated in this header</param>
        /// <returns>A byte array of the serialized header and payload</returns>
        public abstract byte[] GetProtocolPacketBytes(
            ReadOnlySpan<byte> payLoad
        );
    }
}