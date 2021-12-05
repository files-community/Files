#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for ArchiveUpdateCallback.
    /// </summary>
    [Serializable]
    public class CompressionFailedException : SevenZipException
    {
        /// <summary>
        /// Exception dafault message which is displayed if no extra information is specified
        /// </summary>
        public const string DEFAULT_MESSAGE = "Could not pack files!";

        /// <summary>
        /// Initializes a new instance of the CompressionFailedException class
        /// </summary>
        public CompressionFailedException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// Initializes a new instance of the CompressionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        public CompressionFailedException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// Initializes a new instance of the CompressionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occured</param>
        public CompressionFailedException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the CompressionFailedException class
        /// </summary>
        /// <param name="info">All data needed for serialization or deserialization</param>
        /// <param name="context">Serialized stream descriptor</param>
        protected CompressionFailedException(
            SerializationInfo info, StreamingContext context)
            : base(info, context) { }

    }
}

#endif