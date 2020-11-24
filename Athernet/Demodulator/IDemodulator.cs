namespace Athernet.Demodulator
{
    /// <summary>
    /// Demodulator interface.
    /// 
    /// </summary>
    internal interface IDemodulator
    {
        int BitDepth { get; set; }
        int SampleRate { get; set; }
        public int FrameSamples(int frameBytes);
        byte[] Demodulate(float[] samples, int frameBytes);
    }
}
