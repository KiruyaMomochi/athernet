using System;

namespace ToyNet.IpInterface
{
    public static class Utils
    {
        /// <summary>
        /// Utility function for printing a byte array into a series of 4 byte hex digits with
        /// four such hex digits displayed per line.
        /// </summary>
        /// <param name="printBytes">Byte array to display</param>
        public static void PrintByteArray(byte[] printBytes)
        {
            var index = 0;

            while (index < printBytes.Length)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (index >= printBytes.Length)
                        break;

                    for (var j = 0; j < 4; j++)
                    {
                        if (index >= printBytes.Length)
                            break;
                        Console.Write($"{printBytes[index++]:x2}");
                    }
                    Console.Write(" ");
                }
                Console.WriteLine("");
            }
        }
    }
}