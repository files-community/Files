namespace SevenZip
{
    /// <summary>
    /// The EventArgs class for accurate progress handling.
    /// </summary>
    public sealed class ProgressEventArgs : PercentDoneEventArgs
    {
        private readonly byte _delta;

        /// <summary>
        /// Initializes a new instance of the ProgressEventArgs class.
        /// </summary>
        /// <param name="percentDone">The percent of finished work.</param>
        /// <param name="percentDelta">The percent of work done after the previous event.</param>
        public ProgressEventArgs(byte percentDone, byte percentDelta)
            : base(percentDone)
        {
            _delta = percentDelta;
        }

        /// <summary>
        /// Gets the change in done work percentage.
        /// </summary>
        public byte PercentDelta => _delta;
    }
}
