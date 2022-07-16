#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.IO;

    /// <summary>
    /// The arguments passed to <see cref="ExtractFileCallback"/>.
    /// </summary>
    /// <remarks>
    /// For each file, <see cref="ExtractFileCallback"/> is first called with <see cref="Reason"/>
    /// set to <see cref="ExtractFileCallbackReason.Start"/>. If the callback chooses to extract the
    /// file data by setting <see cref="ExtractToFile"/> or <see cref="ExtractToStream"/>, the callback
    /// will be called a second time with <see cref="Reason"/> set to
    /// <see cref="ExtractFileCallbackReason.Done"/> or <see cref="ExtractFileCallbackReason.Failure"/>
    /// to allow for any cleanup task like closing the stream.
    /// </remarks>
    public class ExtractFileCallbackArgs : EventArgs
    {
        private readonly ArchiveFileInfo _archiveFileInfo;
        private Stream _extractToStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractFileCallbackArgs"/> class.
        /// </summary>
        /// <param name="archiveFileInfo">The information about file in the archive.</param>
        public ExtractFileCallbackArgs(ArchiveFileInfo archiveFileInfo)
        {
            Reason = ExtractFileCallbackReason.Start;
            _archiveFileInfo = archiveFileInfo;
        }

        /// <summary>
        /// Information about file in the archive.
        /// </summary>
        /// <value>Information about file in the archive.</value>
        public ArchiveFileInfo ArchiveFileInfo => _archiveFileInfo;

        /// <summary>
        /// The reason for calling <see cref="ExtractFileCallback"/>.
        /// </summary>
        /// <remarks>
        /// If neither <see cref="ExtractToFile"/> nor <see cref="ExtractToStream"/> is set,
        ///  <see cref="ExtractFileCallback"/> will not be called after <see cref="ExtractFileCallbackReason.Start"/>.
        /// </remarks>
        /// <value>The reason.</value>
        public ExtractFileCallbackReason Reason { get; internal set; }

        /// <summary>
        /// The exception that occurred during extraction.
        /// </summary>
        /// <value>The _Exception.</value>
        /// <remarks>
        /// If the callback is called with <see cref="Reason"/> set to <see cref="ExtractFileCallbackReason.Failure"/>,
        /// this member contains the _Exception that occurred.
        /// The default behavior is to rethrow the _Exception after return of the callback.
        /// However the callback can set <see cref="Exception"/> to <c>null</c> to swallow the _Exception
        /// and continue extraction with the next file.
        /// </remarks>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to cancel the extraction.
        /// </summary>
        /// <value><c>true</c> to cancel the extraction; <c>false</c> to continue. The default is <c>false</c>.</value>
        public bool CancelExtraction { get; set; }

        /// <summary>
        /// Gets or sets whether and where to extract the file.
        /// </summary>
        /// <value>The path where to extract the file to.</value>
        /// <remarks>
        /// If <see cref="ExtractToStream"/> is set, this mmember will be ignored.
        /// </remarks>
        public string ExtractToFile { get; set; }

        /// <summary>
        /// Gets or sets whether and where to extract the file.
        /// </summary>
        /// <value>The the extracted data is written to.</value>
        /// <remarks>
        /// If both this member and <see cref="ExtractToFile"/> are <c>null</c> (the defualt), the file
        /// will not be extracted and the callback will be be executed a second time with the <see cref="Reason"/>
        /// set to <see cref="ExtractFileCallbackReason.Done"/> or <see cref="ExtractFileCallbackReason.Failure"/>.
        /// </remarks>
        public Stream ExtractToStream
        {
            get => _extractToStream;
            set
            {
                if (_extractToStream != null && !_extractToStream.CanWrite)
                {
                    throw new ExtractionFailedException("The specified stream is not writable!");
                }

                _extractToStream = value;
            }
        }

        /// <summary>
        /// Gets or sets any data that will be preserved between the <see cref="ExtractFileCallbackReason.Start"/> callback call
        /// and the <see cref="ExtractFileCallbackReason.Done"/> or <see cref="ExtractFileCallbackReason.Failure"/> calls.
        /// </summary>
        /// <value>The data.</value>
        public object ObjectData { get; set; }
    }
}

#endif
