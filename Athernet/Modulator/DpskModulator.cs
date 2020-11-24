namespace Athernet.Modulator
{
    /// <summary>
    /// The differential PSK binary modulator.
    /// For PSK modulator, symbol <value>0</value> and <value>1</value> has different phase.
    /// Here tht phase difference is 180 degree, so we achieve this by
    /// taking the opposite of <c>Gain</c>.
    /// </summary>
    public sealed class DpskModulator : DifferentialBinaryModulator
    {
        public override double[] Frequency { get; protected set; }
        public override double[] Gain { get; protected set; }

        /// <summary>
        /// Construct a new DPSK Modulator.
        /// </summary>
        /// <param name="sampleRate">The sample rate of carrier signal.</param>
        /// <param name="frequency">The frequency of carrier signal.</param>
        /// <param name="gain">The gain of carrier signal, when symbol is <value>0</value>.</param>
        public DpskModulator(in int sampleRate, in double frequency, in double gain = 1)
        {
            SampleRate = sampleRate;
            Frequency = new[] {frequency, frequency};
            Gain = new[] {gain, -gain};
        }
    }
}
