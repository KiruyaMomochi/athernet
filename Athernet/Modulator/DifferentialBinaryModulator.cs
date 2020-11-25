using Athernet.SampleProvider;
using Athernet.Utils;

namespace Athernet.Demodulator
{
    /// <summary>
    /// The differential binary modulator.
    /// This modulator is based on <c>BinaryModulator</c>, but is differential-ed.
    /// That is, the real symbol is obtained by subtracting current symbol with the last symbol.
    /// </summary>
    public abstract class DifferentialBinaryModulator : BinaryModulator
    {
        // We need to add one here since we have one more bit to transmit.
        internal new int FrameSamples (int frameBytes) => base.FrameSamples(frameBytes) + BitDepth * Channel;
        
        /// <summary>
        /// Set frequency and gain of <paramref name="carrier"/> by symbol <value>1</value>.
        /// </summary>
        /// <param name="carrier">The carrier to be changed.</param>
        /// <param name="lastSymbol">The last transmitted symbol.</param>
        private void One(in SineGenerator carrier, ref int lastSymbol)
        {
            // The symbol we transmit is the inverse of last symbol.
            lastSymbol ^= 1;
            carrier.Frequency = Frequency[lastSymbol];
            carrier.Gain = Gain[lastSymbol];
        }


        private static void Zero(in SineGenerator carrier)
        {
            // We transmit last symbol again,
            // so we do nothing here.
        }

        public override float[] Modulate(byte[] data) => Modulate(data, true);

        /// <summary>
        /// Modulate the given <paramref name="data"/>
        /// </summary>
        /// <param name="data">The byte array to modulated</param>
        /// <param name="firstBit">The value of the first bit. This won't affect the demodulation result.</param>
        /// <returns>The modulate result, in float samples.</returns>
        public float[] Modulate(byte[] data, bool firstBit)
        {
            System.Diagnostics.Debug.WriteLine($"Modulate using {this.GetType().Name}.", "Modulator");
            
            // Init a new array to save modulated samples.
            var samples = new float[FrameSamples(data.Length)];
            // Generate a new modulate carrier.
            var modulateCarrier = NewCarrier();
            // The last symbol, used to determine the next symbol to send.
            var lastSymbol = 0;
            // The index of current sample.
            var nSample = 0;

            // Modulate the first it according to firstBit.
            if (firstBit)
                Zero(modulateCarrier);
            else
                One(modulateCarrier, ref lastSymbol);
            nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            
            // Modulate data.
            foreach (var bit in Utils.Maths.ToBits(data, Maths.Endianness.LittleEndian))
            {
                if (bit)
                    One(modulateCarrier, ref lastSymbol);
                else
                    Zero(modulateCarrier);
                nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            }
            
            System.Diagnostics.Debug.Assert(nSample == samples.Length,
                "The index at current sample should at the end of samples array.");

            return samples;
        }
    }
}
