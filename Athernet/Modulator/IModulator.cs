namespace Athernet.Modulator
{
    /// <summary>
    /// Modulator interface.
    /// </summary>
    internal interface IModulator
    {
        /// <summary>
        /// Number of samples for a bit.
        /// </summary>
        int BitDepth { get; set; }

        /// <summary>
        /// Sample rate of result signal.
        /// </summary>
        int SampleRate { get; set; }

        /// <summary>
        /// The number of channels.
        /// </summary>
        int Channel { get; set; }

        /// <summary>
        /// Modulate the given data to float samples
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        float[] Modulate(byte[] data);
    }
}
