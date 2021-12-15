#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception class for 7-zip archive open or read operations.
    /// </summary>
    [Serializable]
    public class SevenZipArchiveException : SevenZipException
    {
        /// <summary>
        /// Exception default message which is displayed if no extra information is specified
        /// </summary>
        public static string DefaultMessage =
            $"Invalid archive: open/read error! Is it encrypted and a wrong password was provided?{Environment.NewLine}" +
            "If your archive is an exotic one, it is possible that SevenZipSharp has no signature for " +
            "its format and thus decided it is TAR by mistake.";

        /// <summary>
        /// Initializes a new instance of the SevenZipArchiveException class
        /// </summary>
        public SevenZipArchiveException() : base(DefaultMessage) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipArchiveException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        public SevenZipArchiveException(string message) : base(DefaultMessage, message) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipArchiveException class
        /// </summary>
        /// <param name="message">Additional detailed message</param>
        /// <param name="inner">Inner exception occurred</param>
        public SevenZipArchiveException(string message, Exception inner) : base(DefaultMessage, message, inner) { }

        /// <summary>
        /// Initializes a new instance of the SevenZipArchiveException class
        /// </summary>
        /// <param name="info">All data needed for serialization or deserialization</param>
        /// <param name="context">Serialized stream descriptor</param>
        protected SevenZipArchiveException(
            SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}

#endif
