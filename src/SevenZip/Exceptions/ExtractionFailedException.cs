#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for ArchiveExtractCallback.
    /// </summary>
    [Serializable]
    public class ExtractionFailedException : SevenZipException
    {
        /// <summary>
        /// Exception dafault message which is displayed if no extra information is specified
        /// </summary>
        public const string DEFAULT_MESSAGE = "Could not extract files!";

        /// <summary>
        /// Initializes a new instance of the ExtractionFailedException class
        /// </summary>
        public ExtractionFailedException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// Initializes a new instance of the ExtractionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        public ExtractionFailedException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// Initializes a new instance of the ExtractionFailedException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occured</param>
        public ExtractionFailedException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the ExtractionFailedException class
        /// </summary>
        /// <param name="info">All data needed for serialization or deserialization</param>
        /// <param name="context">Serialized stream descriptor</param>
        protected ExtractionFailedException(
            SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

#endif
