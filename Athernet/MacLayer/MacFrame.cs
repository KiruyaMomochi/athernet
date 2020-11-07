using System;

namespace Athernet.MacLayer
{
    public readonly ref struct MacFrame
    {
        public readonly byte[] Frame;

        public ref byte Dest => ref Frame[0];

        public ref byte Src => ref Frame[1];

        public MacType Type
        {
            get => (MacType) Frame[2];
            set => Frame[2] = (byte) value;
        }
        
        public readonly Span<byte> Payload;

        public MacFrame(byte[] frame)
        {
            Frame = frame;
            Payload = new Span<byte>(Frame).Slice(3);
        }

        public MacFrame(byte dest, byte src, MacType type, byte[] payload) : this()
        {
            Frame = new byte[payload.Length + 3];
            Dest = dest;
            Src = src;
            Type = type;
            Payload = new Span<byte>(Frame).Slice(3);
            Buffer.BlockCopy(payload, 0, Frame, 3, payload.Length);
        }
        
        
        public MacFrame(byte dest, byte src, MacType type, Span<byte> payload) : this()
        {
            Frame = new byte[payload.Length + 3];
            Dest = dest;
            Src = src;
            Type = type;
            Payload = new Span<byte>(Frame).Slice(3);
            payload.CopyTo(Payload);
        }

    public static MacFrame Parse(byte[] frame)
        {
            return new MacFrame(frame);
        }
    }
}
