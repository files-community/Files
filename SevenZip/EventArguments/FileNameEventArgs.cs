#if UNMANAGED

namespace SevenZip
{
    using System;

    /// <summary>
    /// EventArgs class which stores the file name.
    /// </summary>
    public sealed class FileNameEventArgs : PercentDoneEventArgs, ICancellable
    {
        private readonly string _fileName;

        /// <summary>
        /// Initializes a new instance of the FileNameEventArgs class.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="percentDone">The percent of finished work</param>
        public FileNameEventArgs(string fileName, byte percentDone) :
            base(percentDone)
        {
            _fileName = fileName;
        }

        /// <summary>
        /// Gets or sets whether to stop the current archive operation.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets whether to stop the current archive operation.
        /// </summary>
        public bool Skip
        {
            get => false;
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        public string FileName => _fileName;
    }
}

#endif