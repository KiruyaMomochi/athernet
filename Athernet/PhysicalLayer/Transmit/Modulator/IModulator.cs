namespace Athernet.PhysicalLayer.Transmit.Modulator
{
    /// <summary>
    /// Modulator interface.
    /// </summary>
    public interface IModulator
    {
        /// <summary>
        /// Number of samples for a bit.
        /// </summary>
        int BitDepth { get; init; }

        /// <summary>
        /// Sample rate of carrier signal.
        /// </summary>
        int SampleRate { get; init; }

        /// <summary>
        /// The number of channels.
        /// </summary>
        int Channel { get; init; }

        public int Frequency { get; set; }
        public int Gain { get; set; }

        /// <summary>
        /// Modulate the given data to float samples.
        /// </summary>
        /// <param name="data">The data to be modulated.</param>
        /// <returns>The modulated samples.</returns>
        float[] Modulate(byte[] data);
    }
}
