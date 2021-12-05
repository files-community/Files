#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for fail to create an archive in SevenZipCompressor.
    /// </summary>
    [Serializable]
    public class SevenZipCompressionFailedException : SevenZipException
    {
        /// <summary>
        /// Exception dafault message which is displayed if no extra information is specified
        /// </summary>
        public const string DEFAULT_MESSAGE = "The compression has failed for an unknown reason with code ";

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressionFailedException class
        /// </summary>
        public SevenZipCompressionFailedException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        public SevenZipCompressionFailedException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occured</param>
        public SevenZipCompressionFailedException(string message, Exception inner)
            : base(DEFAULT_MESSAGE, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressionFailedException class
        /// </summary>
        /// <param name="info">All data needed for serialization or deserialization</param>
        /// <param name="context">Serialized stream descriptor</param>
        protected SevenZipCompressionFailedException(
            SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

#endif
