using Athernet.IPLayer.Header;

namespace Athernet.IPLayer
{
    public static class PacketBuilder
    {
        /// <summary>
        /// This method builds the entire packet to be sent on the socket. It takes
        /// an ArrayList of all encapsulated headers as well as the payload. The
        /// ArrayList of headers starts with the outermost header towards the
        /// innermost. For example when sending an IPv4/UDP packet, the first entry 
        /// would be the IPv4 header followed by the UDP header. The byte payload of 
        /// the UDP packet is passed as the second parameter.
        /// </summary>
        /// <param name="ipv4Header">The IPv4 header to build the packet from</param>
        /// <param name="transportHeader">The TCP header to build the packet from</param>
        /// <param name="payLoad">Data payload appearing after all the headers.</param>
        /// <returns>Returns a byte array representing the entire packet</returns>
        public static byte[] BuildPacket(Ipv4Header ipv4Header, TransportHeader transportHeader, byte[] payLoad)
        {
            return ipv4Header.GetProtocolPacketBytes(transportHeader, payLoad);
        }
    }
}