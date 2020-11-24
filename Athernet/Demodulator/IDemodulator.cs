namespace Athernet.Demodulator
{
    /// <summary>
    /// Demodulator interface.
    /// </summary>
    internal interface IDemodulator
    {
        /// <summary>
        /// Number of samples for a bit.
        /// </summary>
        int BitDepth { get; set; }

        /// <summary>
        /// Sample rate of carrier signal.
        /// </summary>
        int SampleRate { get; set; }

        /// <summary>
        /// Demodulate the given samples to byte array.
        /// </summary>
        /// <param name="samples">The samples to be demodulated.</param>
        /// <param name="maxFrameBytes">The maximum number of bytes in the frame.</param>
        /// <returns>The demodulated bytes.</returns>
        byte[] Demodulate(float[] samples, int maxFrameBytes);
    }
}
