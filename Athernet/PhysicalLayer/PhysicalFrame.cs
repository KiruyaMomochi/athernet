using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Force.Crc32;

namespace Athernet.PhysicalLayer
{
    public readonly ref struct PhysicalFrame
    {
        public readonly byte[] Frame;

        public ref byte LengthByte => ref Frame[0];
        public int Length => 1 << LengthByte;

        public readonly Span<byte> CrcSpan;

        public PhysicalFrame(byte[] data)
        {
            Debug.Assert(
                data.Length != Utils.Maths.MostSignificantBitMask(data.Length),
                "The length of data should be the power of 2!"
            );

            Frame = new byte[data.Length + 1 + 4];
            CrcSpan = new Span<byte>(Frame, 1 + data.Length, 4);

            Buffer.BlockCopy(data, 0, Frame, 1, data.Length);
            Crc32Algorithm.ComputeAndWriteToEnd(Frame);
        }

        public static bool Validate(byte[] frame)
        {
            return Crc32Algorithm.IsValidWithCrcAtEnd(frame);
        }
    }
}
