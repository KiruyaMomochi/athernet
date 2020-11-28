using System.Diagnostics;
using Athernet.SampleProvider;

namespace Athernet.PhysicalLayer.Transmit.Modulator
{
    /// <summary>
    /// The differential PSK binary modulator.
    /// For PSK modulator, symbol <value>0</value> and <value>1</value> has different phase.
    /// Here tht phase difference is 180 degree, so we achieve this by
    /// taking the opposite of <c>Gain</c>.
    /// </summary>
    public sealed class DpskModulator: IModulator
    {
        /// <summary>
        /// Number of samples for a bit.
        /// </summary>
        public int BitDepth { get; init; } = 3;
        public int SampleRate { get; init; } = 48000;
        public int Channel { get; init; } = 1;
        public int Frequency { get; set; } = 8000;
        public int Gain { get; set; } = 1;

        /// <summary>
        /// The number of samples in a frame.
        /// </summary>
        /// <param name="frameBytes">The number of bytes in a frame.</param>
        /// <returns>The number of samples in a frame.</returns>
        internal int FrameSamples(int frameBytes) => (frameBytes * Utils.Const.ByteBits + 1)* BitDepth;
        
        /// <summary>
        /// Set <paramref name="carrier"/> by symbol <value>1</value>.
        /// </summary>
        /// <param name="carrier">The carrier to be changed.</param>
        /// <param name="lastSymbol">The last transmitted symbol.</param>
        private static void One(in SineGenerator carrier, ref int lastSymbol)
        {
            // The symbol we transmit is the inverse of last symbol.
            lastSymbol ^= 1;
            carrier.Gain = -carrier.Gain;
        }

        public float[] Modulate(byte[] data) => Modulate(data, false);

        /// <summary>
        /// Modulate the given <paramref name="data"/>
        /// </summary>
        /// <param name="data">The byte array to modulated</param>
        /// <param name="firstBit">The value of the first bit. This won't affect the demodulation result.</param>
        /// <returns>The modulate result, in float samples.</returns>
        public float[] Modulate(byte[] data, bool firstBit)
        {
            Debug.Assert(BitDepth != 0);
            Debug.Assert(SampleRate != 0);

            Debug.WriteLine($"Modulate using {this.GetType().Name}.", "Modulator");
            
            // Init a new array to save modulated samples.
            var samples = new float[FrameSamples(data.Length)];
            // Generate a new modulate carrier.
            var modulateCarrier = new SineGenerator(SampleRate, Channel)
            {
                Frequency = Frequency,
                Gain = Gain
            };
            // The last symbol, used to determine the next symbol to send.
            var lastSymbol = 0;
            // The index of current sample.
            var nSample = 0;

            // Modulate the first it according to firstBit.
            if (firstBit)
                One(modulateCarrier, ref lastSymbol);
            nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            
            // Modulate data.
            foreach (var bit in Utils.Maths.ToBits(data, Utils.Maths.Endianness.LittleEndian))
            {
                if (bit)
                    One(modulateCarrier, ref lastSymbol);
                nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            }
            
            Debug.Assert(nSample == samples.Length,
                "The index at current sample should at the end of samples array.");

            return samples;
        }
    }
}
