#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for fail to extract an archive in SevenZipExtractor.
    /// </summary>
    [Serializable]
    public class SevenZipExtractionFailedException : SevenZipException
    {
        /// <summary>
        /// Exception default message which is displayed if no extra information is specified
        /// </summary>
        public const string DEFAULT_MESSAGE = "The extraction has failed for an unknown reason with code ";

        /// <summary>
        /// Initializes a new instance of the SevenZipExtractionFailedException class
        /// </summary>
        public SevenZipExtractionFailedException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipExtractionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        public SevenZipExtractionFailedException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipExtractionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occured</param>
        public SevenZipExtractionFailedException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipExtractionFailedException class
        /// </summary>
        /// <param name="info">All data needed for serialization or deserialization</param>
        /// <param name="context">Serialized stream descriptor</param>
        protected SevenZipExtractionFailedException(
            SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

#endif
