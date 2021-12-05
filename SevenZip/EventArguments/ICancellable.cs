namespace SevenZip
{
    /// <summary>
    /// The definition of the interface which supports the cancellation of a process.
    /// </summary>
    public interface ICancellable
    {
        /// <summary>
        /// Gets or sets whether to stop the current archive operation.
        /// </summary>
        bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets whether to skip the current file.
        /// </summary>
        bool Skip { get; set; }
    }
}
