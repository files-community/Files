#if UNMANAGED

namespace SevenZip
{
    /// <summary>
    /// The reason for calling <see cref="ExtractFileCallback"/>.
    /// </summary>
    public enum ExtractFileCallbackReason
    {
        /// <summary>
        /// <see cref="ExtractFileCallback"/> is called the first time for a file.
        /// </summary>
        Start,

        /// <summary>
        /// All data has been written to the target without any exceptions.
        /// </summary>
        Done,

        /// <summary>
        /// An exception occured during extraction of the file.
        /// </summary>
        Failure
    }
}

#endif
