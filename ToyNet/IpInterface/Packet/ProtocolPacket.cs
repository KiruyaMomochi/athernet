using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ToyNet.IpInterface.Header;

namespace ToyNet.IpInterface.Packet
{
    internal abstract class ProtocolPacket
    {
        /// <summary>
        /// This method builds the entire packet to be sent on the socket. It takes
        /// an ArrayList of all encapsulated headers as well as the payload. The
        /// ArrayList of headers starts with the outermost header towards the
        /// innermost. For example when sending an IPv4/UDP packet, the first entry 
        /// would be the IPv4 header followed by the UDP header. The byte payload of 
        /// the UDP packet is passed as the second parameter.
        /// </summary>
        /// <param name="headerList">An array list of all headers to build the packet from</param>
        /// <param name="payLoad">Data payload appearing after all the headers</param>
        /// <returns>Returns a byte array representing the entire packet</returns>
        public byte[] BuildPacket(IEnumerable<ProtocolHeader> headerList, byte[] payLoad)
        {
            foreach (var protocolHeader in headerList.Reverse())
            {
                var newPayload = protocolHeader.GetProtocolPacketBytes(payLoad);
                // The payLoad for the next iteration of the loop is now any
                //    encapsulated headers plus the original payload data.
                payLoad = newPayload;
            }
            return payLoad;
        }
    }
}