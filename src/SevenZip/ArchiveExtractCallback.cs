namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

#if UNMANAGED
    /// <summary>
    /// Archive extraction callback to handle the process of unpacking files
    /// </summary>
    internal sealed class ArchiveExtractCallback : CallbackBase, IArchiveExtractCallback, ICryptoGetTextPassword, IDisposable
    {
        private List<uint> _actualIndexes;
        private IInArchive _archive;

        /// <summary>
        /// For Compressing event.
        /// </summary>
        private long _bytesCount;

        private long _bytesWritten;
        private long _bytesWrittenOld;
        private string _directory;

        /// <summary>
        /// Rate of the done work from [0, 1].
        /// </summary>
        private float _doneRate;

        private SevenZipExtractor _extractor;
        private FakeOutStreamWrapper _fakeStream;
        private uint? _fileIndex;
        private int _filesCount;
        private OutStreamWrapper _fileStream;
        private bool _directoryStructure;
        private int _currentIndex;
        private const int MemoryPressure = 64 * 1024 * 1024; //64mb seems to be the maximum value

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ArchiveExtractCallback class
        /// </summary>
        /// <param name="archive">IInArchive interface for the archive</param>
        /// <param name="directory">Directory where files are to be unpacked to</param>
        /// <param name="filesCount">The archive files count</param>'
        /// <param name="extractor">The owner of the callback</param>
        /// <param name="actualIndexes">The list of actual indexes (solid archives support)</param>
        /// <param name="directoryStructure">The value indicating whether to preserve directory structure of extracted files.</param>
        public ArchiveExtractCallback(IInArchive archive, string directory, int filesCount, bool directoryStructure,
            List<uint> actualIndexes, SevenZipExtractor extractor)
        {
            Init(archive, directory, filesCount, directoryStructure, actualIndexes, extractor);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveExtractCallback class
        /// </summary>
        /// <param name="archive">IInArchive interface for the archive</param>
        /// <param name="directory">Directory where files are to be unpacked to</param>
        /// <param name="filesCount">The archive files count</param>
        /// <param name="password">Password for the archive</param>
        /// <param name="extractor">The owner of the callback</param>
        /// <param name="actualIndexes">The list of actual indexes (solid archives support)</param>
        /// <param name="directoryStructure">The value indicating whether to preserve directory structure of extracted files.</param>
        public ArchiveExtractCallback(IInArchive archive, string directory, int filesCount, bool directoryStructure,
            List<uint> actualIndexes, string password, SevenZipExtractor extractor)
            : base(password)
        {
            Init(archive, directory, filesCount, directoryStructure, actualIndexes, extractor);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveExtractCallback class
        /// </summary>
        /// <param name="archive">IInArchive interface for the archive</param>
        /// <param name="stream">The stream where files are to be unpacked to</param>
        /// <param name="filesCount">The archive files count</param>
        /// <param name="fileIndex">The file index for the stream</param>
        /// <param name="extractor">The owner of the callback</param>
        public ArchiveExtractCallback(IInArchive archive, Stream stream, int filesCount, uint fileIndex, SevenZipExtractor extractor)
        {
            Init(archive, stream, filesCount, fileIndex, extractor);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveExtractCallback class
        /// </summary>
        /// <param name="archive">IInArchive interface for the archive</param>
        /// <param name="stream">The stream where files are to be unpacked to</param>
        /// <param name="filesCount">The archive files count</param>
        /// <param name="fileIndex">The file index for the stream</param>
        /// <param name="password">Password for the archive</param>
        /// <param name="extractor">The owner of the callback</param>
        public ArchiveExtractCallback(IInArchive archive, Stream stream, int filesCount, uint fileIndex, string password, SevenZipExtractor extractor)
            : base(password)
        {
            Init(archive, stream, filesCount, fileIndex, extractor);
        }

        private void Init(IInArchive archive, string directory, int filesCount, bool directoryStructure, List<uint> actualIndexes, SevenZipExtractor extractor)
        {
            CommonInit(archive, filesCount, extractor);
            _directory = directory;
            _actualIndexes = actualIndexes;
            _directoryStructure = directoryStructure;
            if (!directory.EndsWith("" + Path.DirectorySeparatorChar, StringComparison.CurrentCulture))
            {
                _directory += Path.DirectorySeparatorChar;
            }
        }

        private void Init(IInArchive archive, Stream stream, int filesCount, uint fileIndex, SevenZipExtractor extractor)
        {
            CommonInit(archive, filesCount, extractor);
            _fileStream = new OutStreamWrapper(stream, false);
            _fileStream.BytesWritten += IntEventArgsHandler;
            _fileIndex = fileIndex;
        }

        private void CommonInit(IInArchive archive, int filesCount, SevenZipExtractor extractor)
        {
            _archive = archive;
            _filesCount = filesCount;
            _fakeStream = new FakeOutStreamWrapper();
            _fakeStream.BytesWritten += IntEventArgsHandler;
            _extractor = extractor;
            GC.AddMemoryPressure(MemoryPressure);
        }
        #endregion

        /// <summary>
        /// Occurs when a new file is going to be unpacked
        /// </summary>
        /// <remarks>Occurs when 7-zip engine requests for an output stream for a new file to unpack in</remarks>
        public event EventHandler<FileInfoEventArgs> FileExtractionStarted;

        /// <summary>
        /// Occurs when a file has been successfully unpacked
        /// </summary>
        public event EventHandler<FileInfoEventArgs> FileExtractionFinished;

        /// <summary>
        /// Occurs when the archive is opened and 7-zip sends the size of unpacked data
        /// </summary>
        public event EventHandler<OpenEventArgs> Open;

        /// <summary>
        /// Occurs when the extraction is performed
        /// </summary>
        public event EventHandler<ProgressEventArgs> Extracting;

        /// <summary>
        /// Occurs during the extraction when a file already exists
        /// </summary>
        public event EventHandler<FileOverwriteEventArgs> FileExists;

        private void IntEventArgsHandler(object sender, IntEventArgs e)
        {
            // If _bytesCount is not set, we can't update the progress.
            if (_bytesCount == 0)
            {
                return;
            }

            var pold = (int)(_bytesWrittenOld * 100 / _bytesCount);
            _bytesWritten += e.Value;
            var pnow = (int)(_bytesWritten * 100 / _bytesCount);

            if (pnow > pold)
            {
                if (pnow > 100)
                {
                    pold = pnow = 0;
                }

                _bytesWrittenOld = _bytesWritten;
                Extracting?.Invoke(this, new ProgressEventArgs((byte)pnow, (byte)(pnow - pold)));
            }
        }

        #region IArchiveExtractCallback Members

        /// <summary>
        /// Gives the size of the unpacked archive files
        /// </summary>
        /// <param name="total">Size of the unpacked archive files (in bytes)</param>
        public void SetTotal(ulong total)
        {
            _bytesCount = (long)total;
            Open?.Invoke(this, new OpenEventArgs(total));
        }

        public void SetCompleted(ref ulong completeValue) { }

        /// <summary>
        /// Sets output stream for writing unpacked data
        /// </summary>
        /// <param name="index">Current file index</param>
        /// <param name="outStream">Output stream pointer</param>
        /// <param name="askExtractMode">Extraction mode</param>
        /// <returns>0 if OK</returns>
        public int GetStream(uint index, out ISequentialOutStream outStream, AskMode askExtractMode)
        {
            outStream = null;

            if (Canceled)
            {
                return -1;
            }

            _currentIndex = (int)index;

            if (askExtractMode == AskMode.Extract)
            {
                var fileName = _directory;

                if (!_fileIndex.HasValue)
                {
                    // Extraction to a file

                    if (_actualIndexes == null || _actualIndexes.Contains(index))
                    {
                        var data = new PropVariant();
                        _archive.GetProperty(index, ItemPropId.Path, ref data);
                        var entryName = NativeMethods.SafeCast(data, "");

                        #region Get entryName

                        if (string.IsNullOrEmpty(entryName))
                        {
                            if (_filesCount == 1)
                            {
                                var archName = Path.GetFileName(_extractor.FileName);
                                archName = archName.Substring(0,
                                                              archName.LastIndexOf('.'));
                                if (!archName.EndsWith(".tar",
                                                       StringComparison.OrdinalIgnoreCase))
                                {
                                    archName += ".tar";
                                }

                                entryName = archName;
                            }
                            else
                            {
                                entryName = "[no name] " + index.ToString(CultureInfo.InvariantCulture);
                            }
                        }

                        #endregion

                        try
                        {
                            fileName = Path.Combine(RemoveIllegalCharacters(_directory, true), RemoveIllegalCharacters(_directoryStructure ? entryName : Path.GetFileName(entryName)));
                            
                            if (string.IsNullOrEmpty(fileName))
                            {
                                throw new SevenZipArchiveException("Some archive name is null or empty.");
                            }
                        }
                        catch (Exception e)
                        {
                            AddException(e);
                            outStream = _fakeStream;

                            return 0;
                        }

                        _archive.GetProperty(index, ItemPropId.IsDirectory, ref data);

                        if (!NativeMethods.SafeCast(data, false))
                        {
                            _archive.GetProperty(index, ItemPropId.LastWriteTime, ref data);
                            var time = NativeMethods.SafeCast(data, DateTime.MinValue);
                            
                            if (File.Exists(fileName))
                            {
                                var fnea = new FileOverwriteEventArgs(fileName);

                                FileExists?.Invoke(this, fnea);
                                
                                if (fnea.Cancel)
                                {
                                    Canceled = true;
                                    return -1;
                                }

                                if (string.IsNullOrEmpty(fnea.FileName))
                                {
                                    outStream = _fakeStream;
                                }
                                else
                                {
                                    fileName = fnea.FileName;
                                }
                            }

                            _doneRate += 1.0f / _filesCount;
                            var iea = new FileInfoEventArgs(_extractor.ArchiveFileData[(int) index], PercentDoneEventArgs.ProducePercentDone(_doneRate));

                            FileExtractionStarted?.Invoke(this, iea);
                            
                            if (iea.Cancel)
                            {
                                Canceled = true;
                                return -1;
                            }

                            if (iea.Skip)
                            {
                                outStream = _fakeStream;
                                return 0;
                            }

                            CreateDirectory(fileName);

                            try
                            {
                                _fileStream = new OutStreamWrapper(File.Create(fileName), fileName, time, true);
                            }
                            catch (Exception e)
                            {
                                AddException(e is FileNotFoundException
                                    ? new IOException($"The file \"{fileName}\" was not extracted due to the File.Create fail.")
                                    : e);

                                outStream = _fakeStream;

                                return 0;
                            }

                            _fileStream.BytesWritten += IntEventArgsHandler;
                            outStream =  _fileStream;
                        }
                        else
                        {
                            _doneRate += 1.0f / _filesCount;
                            var iea = new FileInfoEventArgs(_extractor.ArchiveFileData[(int)index], PercentDoneEventArgs.ProducePercentDone(_doneRate));
                            FileExtractionStarted?.Invoke(this, iea);
                            
                            if (iea.Cancel)
                            {
                                Canceled = true;
                                return -1;
                            }

                            if (iea.Skip)
                            {
                                outStream = _fakeStream;
                                return 0;
                            }

                            if (!Directory.Exists(fileName))
                            {
                                try
                                {
                                    Directory.CreateDirectory(fileName);
                                }
                                catch (Exception e)
                                {
                                    AddException(e);
                                }

                                outStream = _fakeStream;
                            }
                        }
                    }
                    else
                    {
                        outStream = _fakeStream;
                    }
                }
                else
                {
                    // Extraction to a stream.

                    if (index == _fileIndex)
                    {
                        outStream  = _fileStream;
                        _fileIndex = null;
                    }
                    else
                    {
                        outStream = _fakeStream;
                    }
                }
            }

            return 0;
        }

        /// <inheritdoc />
        public void PrepareOperation(AskMode askExtractMode) { }

        /// <inheritdoc />
        public void SetOperationResult(OperationResult operationResult)
        {
            if (operationResult != OperationResult.Ok && ReportErrors)
            {
                switch (operationResult)
                {
                    case OperationResult.CrcError:
                        AddException(new ExtractionFailedException("File is corrupted. Crc check has failed."));
                        break;
                    case OperationResult.DataError:
                        AddException(new ExtractionFailedException("File is corrupted. Data error has occured."));
                        break;
                    case OperationResult.UnsupportedMethod:
                        AddException(new ExtractionFailedException("Unsupported method error has occured."));
                        break;
                    case OperationResult.Unavailable:
                        AddException(new ExtractionFailedException("File is unavailable."));
                        break;
                    case OperationResult.UnexpectedEnd:
                        AddException(new ExtractionFailedException("Unexpected end of file."));
                        break;
                    case OperationResult.DataAfterEnd: 
                        AddException(new ExtractionFailedException("Data after end of archive."));
                        break;
                    case OperationResult.IsNotArc:
                        AddException(new ExtractionFailedException("File is not archive."));
                        break;
                    case OperationResult.HeadersError:
                        AddException(new ExtractionFailedException("Archive headers error."));
                        break;
                    case OperationResult.WrongPassword:
                        AddException(new ExtractionFailedException("Wrong password."));
                        break;
                    default:
                        AddException(new ExtractionFailedException($"Unexpected operation result: {operationResult}"));
                        break;
                }
            }
            else
            {
                if (_fileStream != null && !_fileIndex.HasValue)
                {
                    try
                    {
                        _fileStream.BytesWritten -= IntEventArgsHandler;
                        _fileStream.Dispose();
                    }
                    catch (ObjectDisposedException) { }
                    _fileStream = null;
                }

                var iea = new FileInfoEventArgs(_extractor.ArchiveFileData[_currentIndex], PercentDoneEventArgs.ProducePercentDone(_doneRate));
                FileExtractionFinished?.Invoke(this, iea);
                
                if (iea.Cancel)
                {
                    Canceled = true;
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public int CryptoGetTextPassword(out string password)
        {
            password = Password;
            return 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.RemoveMemoryPressure(MemoryPressure);

            if (_fileStream != null)
            {
                try
                {
                    _fileStream.Dispose();
                }
                catch (ObjectDisposedException) { }
                _fileStream = null;
            }

            if (_fakeStream != null)
            {
                try
                {
                    _fakeStream.Dispose();
                }
                catch (ObjectDisposedException) { }
                _fakeStream = null;
            }
        }

        /// <summary>
        /// Ensures that the directory to the file name is valid and creates intermediate directories if necessary
        /// </summary>
        /// <param name="fileName">File name</param>
        private static void CreateDirectory(string fileName)
        {
            var destinationDirectory = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
        }

        /// <summary>
        /// removes the invalid character in file path.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        private static string RemoveIllegalCharacters(string str, bool isDirectory = false)
        {
            var splitFileName = new List<string>(str.Split(Path.DirectorySeparatorChar));

            foreach (var chr in Path.GetInvalidFileNameChars())
            {
                for (var i = 0; i < splitFileName.Count; i++)
                {
                    if (isDirectory && chr == ':' && i == 0)
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(splitFileName[i]))
                    {
                        continue;
                    }
                    while (splitFileName[i].IndexOf(chr) > -1)
                    {
                        splitFileName[i] = splitFileName[i].Replace(chr, '_');
                    }
                }
            }

            if (str.StartsWith(new string(Path.DirectorySeparatorChar, 2), StringComparison.CurrentCultureIgnoreCase))
            {
                splitFileName.RemoveAt(0);
                splitFileName.RemoveAt(0);
                splitFileName[0] = new string(Path.DirectorySeparatorChar, 2) + splitFileName[0];
            }

            return string.Join(new string(Path.DirectorySeparatorChar, 1), splitFileName.ToArray());
        }
    }
#endif
}