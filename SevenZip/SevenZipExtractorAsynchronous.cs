namespace SevenZip
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    partial class SevenZipExtractor
    {
        #region Asynchronous core methods

        /// <summary>
        /// Recreates the instance of the SevenZipExtractor class.
        /// Used in asynchronous methods.
        /// </summary>
        private void RecreateInstanceIfNeeded()
        {
            if (NeedsToBeRecreated)
            {
                NeedsToBeRecreated = false;
                Stream backupStream = null;
                string backupFileName = null;
                if (String.IsNullOrEmpty(_fileName))
                {
                    backupStream = _inStream;
                }
                else
                {
                    backupFileName = _fileName;
                }
                CommonDispose(String.IsNullOrEmpty(_fileName));
                if (backupStream == null)
                {
                    Init(backupFileName);
                }
                else
                {
                    Init(backupStream);
                }
            }
        }

        internal override void SaveContext()
        {
            DisposedCheck();
            _asynchronousDisposeLock = true;
            base.SaveContext();
        }

        internal override void ReleaseContext()
        {
            base.ReleaseContext();
            _asynchronousDisposeLock = false;
        }

        #endregion

        #region Delegates

        /// <summary>
        /// The delegate to use in BeginExtractArchive.
        /// </summary>
        /// <param name="directory">The directory where the files are to be unpacked.</param>
        private delegate void ExtractArchiveDelegate(string directory);

        /// <summary>
        /// The delegate to use in BeginExtractFile (by file name).
        /// </summary>
        /// <param name="fileName">The file full name in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        private delegate void ExtractFileByFileNameDelegate(string fileName, Stream stream);

        /// <summary>
        /// The delegate to use in BeginExtractFile (by index).
        /// </summary>
        /// <param name="index">Index in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        private delegate void ExtractFileByIndexDelegate(int index, Stream stream);

        /// <summary>
        /// The delegate to use in BeginExtractFiles(string directory, params int[] indexes).
        /// </summary>
        /// <param name="indexes">indexes of the files in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        private delegate void ExtractFiles1Delegate(string directory, int[] indexes);

        /// <summary>
        /// The delegate to use in BeginExtractFiles(string directory, params string[] fileNames).
        /// </summary>
        /// <param name="fileNames">Full file names in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        private delegate void ExtractFiles2Delegate(string directory, string[] fileNames);

        /// <summary>
        /// The delegate to use in BeginExtractFiles(ExtractFileCallback extractFileCallback).
        /// </summary>
        /// <param name="extractFileCallback">The callback to call for each file in the archive.</param>
        private delegate void ExtractFiles3Delegate(ExtractFileCallback extractFileCallback);
        #endregion

        /// <summary>
        /// Unpacks the whole archive asynchronously to the specified directory name at the specified priority.
        /// </summary>
        /// <param name="directory">The directory where the files are to be unpacked.</param>
        public void BeginExtractArchive(string directory)
        {
            SaveContext();
            Task.Run(() => new ExtractArchiveDelegate(ExtractArchive).Invoke(directory))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// Unpacks the whole archive asynchronously to the specified directory name at the specified priority.
        /// </summary>
        /// <param name="directory">The directory where the files are to be unpacked.</param>
        public async Task ExtractArchiveAsync(string directory)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractArchiveDelegate(ExtractArchive).Invoke(directory));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// Unpacks the file asynchronously by its name to the specified stream.
        /// </summary>
        /// <param name="fileName">The file full name in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public void BeginExtractFile(string fileName, Stream stream)
        {
            SaveContext();
            Task.Run(() => new ExtractFileByFileNameDelegate(ExtractFile).Invoke(fileName, stream))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// Unpacks the file asynchronously by its name to the specified stream.
        /// </summary>
        /// <param name="fileName">The file full name in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public async Task ExtractFileAsync(string fileName, Stream stream)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFileByFileNameDelegate(ExtractFile).Invoke(fileName, stream));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// Unpacks the file asynchronously by its index to the specified stream.
        /// </summary>
        /// <param name="index">Index in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public void BeginExtractFile(int index, Stream stream)
        {
            SaveContext();
            Task.Run(() => new ExtractFileByIndexDelegate(ExtractFile).Invoke(index, stream))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// Unpacks the file asynchronously by its name to the specified stream.
        /// </summary>
        /// <param name="index">Index in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public async Task ExtractFileAsync(int index, Stream stream)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFileByIndexDelegate(ExtractFile).Invoke(index, stream));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// Unpacks files asynchronously by their indices to the specified directory.
        /// </summary>
        /// <param name="indexes">indexes of the files in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public void BeginExtractFiles(string directory, params int[] indexes)
        {
            SaveContext();
            Task.Run(() => new ExtractFiles1Delegate(ExtractFiles).Invoke(directory, indexes))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// Unpacks files asynchronously by their indices to the specified directory.
        /// </summary>
        /// <param name="indexes">indexes of the files in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public async Task ExtractFilesAsync(string directory, params int[] indexes)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFiles1Delegate(ExtractFiles).Invoke(directory, indexes));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// Unpacks files asynchronously by their full names to the specified directory.
        /// </summary>
        /// <param name="fileNames">Full file names in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public void BeginExtractFiles(string directory, params string[] fileNames)
        {
            SaveContext();
            Task.Run(() => new ExtractFiles2Delegate(ExtractFiles).Invoke(directory, fileNames))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// Unpacks files asynchronously by their full names to the specified directory.
        /// </summary>
        /// <param name="fileNames">Full file names in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public async Task ExtractFilesAsync(string directory, params string[] fileNames)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFiles2Delegate(ExtractFiles).Invoke(directory, fileNames));
            }
            finally
            {
                ReleaseContext();
            }
        }

        /// <summary>
        /// Extracts files from the archive asynchronously, giving a callback the choice what
        /// to do with each file. The order of the files is given by the archive.
        /// 7-Zip (and any other solid) archives are NOT supported.
        /// </summary>
        /// <param name="extractFileCallback">The callback to call for each file in the archive.</param>
        public void BeginExtractFiles(ExtractFileCallback extractFileCallback)
        {
            SaveContext();
            Task.Run(() => new ExtractFiles3Delegate(ExtractFiles).Invoke(extractFileCallback))
                .ContinueWith(_ => ReleaseContext());
        }

        /// <summary>
        /// Extracts files from the archive asynchronously, giving a callback the choice what
        /// to do with each file. The order of the files is given by the archive.
        /// 7-Zip (and any other solid) archives are NOT supported.
        /// </summary>
        /// <param name="extractFileCallback">The callback to call for each file in the archive.</param>
        public async Task ExtractFilesAsync(ExtractFileCallback extractFileCallback)
        {
            try
            {
                SaveContext();
                await Task.Run(() => new ExtractFiles3Delegate(ExtractFiles).Invoke(extractFileCallback));
            }
            finally
            {
                ReleaseContext();
            }
        }
    }
}
