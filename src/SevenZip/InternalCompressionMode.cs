#if UNMANAGED

namespace SevenZip
{
    internal enum InternalCompressionMode
    {
        /// <summary>
        /// Create a new archive; overwrite the existing one.
        /// </summary>
        Create,
        /// <summary>
        /// Add data to the archive.
        /// </summary>
        Append,
        /// <summary>
        /// Modify archive data.
        /// </summary>
        Modify
    }
}

#endif
