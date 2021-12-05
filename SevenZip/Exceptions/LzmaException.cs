namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for LZMA operations.
    /// </summary>
    [Serializable]
    public class LzmaException : SevenZipException
    {
        /// <summary>
        /// Exception dafault message which is displayed if no extra information is specified
        /// </summary>
        public const string DEFAULT_MESSAGE = "Specified stream is not a valid LZMA compressed stream!";

        /// <summary>
        /// Initializes a new instance of the LzmaException class
        /// </summary>
        public LzmaException() : base(DEFAULT_MESSAGE) { }

        /// <summary>
        /// Initializes a new instance of the LzmaException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        public LzmaException(string message) : base(DEFAULT_MESSAGE, message) { }

        /// <summary>
        /// Initializes a new instance of the LzmaException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occured</param>
        public LzmaException(string message, Exception inner) : base(DEFAULT_MESSAGE, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the LzmaException class
        /// </summary>
        /// <param name="info">All data needed for serialization or deserialization</param>
        /// <param name="context">Serialized stream descriptor</param>
        protected LzmaException(
            SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
