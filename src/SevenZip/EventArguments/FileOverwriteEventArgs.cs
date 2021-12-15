#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// EventArgs for FileExists event, stores the file name and asks whether to overwrite it in case it already exists.
    /// </summary>
    public sealed class FileOverwriteEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the FileOverwriteEventArgs class
        /// </summary>
        /// <param name="fileName">The file name.</param>
        public FileOverwriteEventArgs(string fileName)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the value indicating whether to cancel the extraction.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets the file name to extract to. Null means skip.
        /// </summary>
        public string FileName { get; set; }
    }
}

#endif