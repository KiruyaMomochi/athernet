using Athernet.SampleProvider;
using Athernet.Utils;

namespace Athernet.Modulator
{
    /// <summary>
    /// The binary modulator class.
    /// In this modulator, there are only two symbols: 0 and 1.
    /// Each symbol have the length of <c>BitDepth</c>.
    /// The little endian is used.
    /// </summary>
    public abstract class BinaryModulator : IModulator
    {
        /// <summary>
        /// The number of bits in a byte, that is 8.
        /// </summary>
        private const int ByteBits = 8;

        /// <summary>
        /// The frequency array.
        /// The first frequency will be used for symbol 1.
        /// The second frequency will be used for symbol 2.
        /// </summary>
        public virtual double[] Frequency { get; protected set; }

        /// <summary>
        /// The gain array.
        /// The first gain will be used for symbol 1.
        /// The second gain will be used for symbol 2.
        /// </summary>
        public virtual double[] Gain { get; protected set; }

        public virtual int BitDepth { get; set; }
        public virtual int SampleRate { get; set; }
        public int Channel { get; set; } = 1;

        /// <summary>
        /// The number of samples in a frame.
        /// </summary>
        /// <param name="frameBytes">The number of bytes in a frame.</param>
        /// <returns>The number of samples in a frame.</returns>
        internal int FrameSamples(int frameBytes) => frameBytes * ByteBits * BitDepth * Channel;

        /// <summary>
        /// Modulate input bytes to samples.
        /// </summary>
        /// <param name="data">The bytes to be modulated.</param>
        /// <returns>The modulated samples.</returns>
        public virtual float[] Modulate(byte[] data)
        {
            // Init a new array to save modulated samples.
            var samples = new float[FrameSamples(data.Length)];
            // Generate a new modulate carrier.
            var modulateCarrier = NewCarrier();
            // The current index of sample.
            var nSample = 0;

            foreach (var bit in Maths.ToBits(data, Maths.Endianness.LittleEndian))
            {
                if (bit)
                    One(modulateCarrier);
                else
                    Zero(modulateCarrier);

                // Add the sample.
                nSample += modulateCarrier.Read(samples, nSample, BitDepth);
            }

            System.Diagnostics.Debug.Assert(nSample == samples.Length,
                "The index at current sample should at the end of samples array.");

            return samples;
        }

        /// <summary>
        /// Return a new sine signal.
        /// It's needed since function <c>Modulate</c> can run simultaneously.
        /// </summary>
        /// <returns></returns>
        internal virtual SineGenerator NewCarrier()
        {
            var signal = new SineGenerator(SampleRate, Channel)
            {
                Frequency = Frequency[0],
                Gain = Gain[0]
            };
            return signal;
        }

        /// <summary>
        /// Set frequency and gain of <paramref name="carrier"/> by symbol <value>1</value>.
        /// </summary>
        /// <param name="carrier">The carrier to be changed.</param>
        private void One(in SineGenerator carrier)
        {
            carrier.Frequency = Frequency[0];
            carrier.Gain = Gain[0];
        }

        /// <summary>
        /// Set frequency and gain of <paramref name="carrier"/> by symbol <value>0</value>.
        /// </summary>
        /// <param name="carrier">The carrier to be changed.</param>
        private void Zero(in SineGenerator carrier)
        {
            carrier.Frequency = Frequency[1];
            carrier.Gain = Gain[1];
        }
    }
}