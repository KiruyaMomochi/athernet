using System;
using System.Runtime.InteropServices;

namespace Athernet.Mac
{
    public class MacFrame
    {
        public byte[] Frame;
        
        public byte Dest => Frame[0];
        public byte Src => Frame[1];
        public byte Type => Frame[2];
        // public Span<byte> Payload;
    }
}