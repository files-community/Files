namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

#if UNMANAGED
    /// <summary>
    /// Archive update callback to handle the process of packing files
    /// </summary>
    internal sealed class ArchiveUpdateCallback : CallbackBase, IArchiveUpdateCallback, ICryptoGetTextPassword2,
                                                  IDisposable
    {
        #region Fields
        /// <summary>
        /// _files.Count if do not count directories
        /// </summary>
        private int _actualFilesCount;

        /// <summary>
        /// For Compressing event.
        /// </summary>
        private long _bytesCount;

        private long _bytesWritten;
        private long _bytesWrittenOld;
        private SevenZipCompressor _compressor;

        /// <summary>
        /// No directories.
        /// </summary>
        private bool _directoryStructure;

        /// <summary>
        /// Rate of the done work from [0, 1]
        /// </summary>
        private float _doneRate;

        /// <summary>
        /// The names of the archive entries
        /// </summary>
        private string[] _entries;

        /// <summary>
        /// Array of files to pack
        /// </summary>
        private FileInfo[] _files;

        private InStreamWrapper _fileStream;

        private uint _indexInArchive;
        private uint _indexOffset;

        /// <summary>
        /// Common root of file names length.
        /// </summary>
        private int _rootLength;

        /// <summary>
        /// Input streams to be compressed.
        /// </summary>
        private Stream[] _streams;

        private UpdateData _updateData;
        private List<InStreamWrapper> _wrappersToDispose;

        /// <summary>
        /// Gets or sets the default item name used in MemoryStream compression.
        /// </summary>
        public string DefaultItemName { private get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether to compress as fast as possible, without calling events.
        /// </summary>
        public bool FastCompression { private get; set; } 

        private int _memoryPressure;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ArchiveUpdateCallback class
        /// </summary>
        /// <param name="files">Array of files to pack</param>
        /// <param name="rootLength">Common file names root length</param>
        /// <param name="compressor">The owner of the callback</param>
        /// <param name="updateData">The compression parameters.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        public ArchiveUpdateCallback(
            FileInfo[] files, int rootLength,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            Init(files, rootLength, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveUpdateCallback class
        /// </summary>
        /// <param name="files">Array of files to pack</param>
        /// <param name="rootLength">Common file names root length</param>
        /// <param name="password">The archive password</param>
        /// <param name="compressor">The owner of the callback</param>
        /// <param name="updateData">The compression parameters.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        public ArchiveUpdateCallback(
            FileInfo[] files, int rootLength, string password,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
            : base(password)
        {
            Init(files, rootLength, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveUpdateCallback class
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <param name="compressor">The owner of the callback</param>
        /// <param name="updateData">The compression parameters.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        public ArchiveUpdateCallback(
            Stream stream, SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            Init(stream, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveUpdateCallback class
        /// </summary>
        /// <param name="stream">The input stream</param>
        /// <param name="password">The archive password</param>
        /// <param name="compressor">The owner of the callback</param>
        /// <param name="updateData">The compression parameters.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        public ArchiveUpdateCallback(
            Stream stream, string password, SevenZipCompressor compressor, UpdateData updateData,
            bool directoryStructure)
            : base(password)
        {
            Init(stream, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveUpdateCallback class
        /// </summary>
        /// <param name="streamDict">Dictionary&lt;file stream, name of the archive entry&gt;</param>
        /// <param name="compressor">The owner of the callback</param>
        /// <param name="updateData">The compression parameters.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        public ArchiveUpdateCallback(
            IDictionary<string, Stream> streamDict,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            Init(streamDict, compressor, updateData, directoryStructure);
        }

        /// <summary>
        /// Initializes a new instance of the ArchiveUpdateCallback class
        /// </summary>
        /// <param name="streamDict">Dictionary&lt;file stream, name of the archive entry&gt;</param>
        /// <param name="password">The archive password</param>
        /// <param name="compressor">The owner of the callback</param>
        /// <param name="updateData">The compression parameters.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        public ArchiveUpdateCallback(
            IDictionary<string, Stream> streamDict, string password,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
            : base(password)
        {
            Init(streamDict, compressor, updateData, directoryStructure);
        }

        private void CommonInit(SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            _compressor = compressor;
            _indexInArchive = updateData.FilesCount;
            _indexOffset = updateData.Mode != InternalCompressionMode.Append ? 0 : _indexInArchive;
            if (_compressor.ArchiveFormat == OutArchiveFormat.Zip)
            {
                _wrappersToDispose = new List<InStreamWrapper>();
            }
            _updateData = updateData;
            _directoryStructure = directoryStructure;
            DefaultItemName = "default";            
        }

        private void Init(
            FileInfo[] files, int rootLength, SevenZipCompressor compressor,
            UpdateData updateData, bool directoryStructure)
        {
            _files = files;
            _rootLength = rootLength;
            if (files != null)
            {
                foreach (var fi in files)
                {
                    if (fi.Exists)
                    {
                        _bytesCount += fi.Length;
                        if ((fi.Attributes & FileAttributes.Directory) == 0)
                        {
                            _actualFilesCount++;
                        }
                    }
                }
            }
            CommonInit(compressor, updateData, directoryStructure);
        }

        private void Init(
            Stream stream, SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            _fileStream = new InStreamWrapper(stream, false);
            _fileStream.BytesRead += IntEventArgsHandler;
            _actualFilesCount = 1;

            try
            {
                _bytesCount = stream.Length;
            }
            catch (NotSupportedException)
            {
                _bytesCount = -1;
            }
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (NotSupportedException)
            {
                _bytesCount = -1;
            }
            CommonInit(compressor, updateData, directoryStructure);
        }

        private void Init(
            IDictionary<string, Stream> streamDict,
            SevenZipCompressor compressor, UpdateData updateData, bool directoryStructure)
        {
            _streams = new Stream[streamDict.Count];
            streamDict.Values.CopyTo(_streams, 0);
            _entries = new string[streamDict.Count];
            streamDict.Keys.CopyTo(_entries, 0);
            _actualFilesCount = streamDict.Count;
            foreach (Stream str in _streams)
            {
                if (str != null)
                {
                    _bytesCount += str.Length;
                }
            }
            _bytesCount = _bytesCount > 0 ? _bytesCount : -1;
            CommonInit(compressor, updateData, directoryStructure);
        }

        #endregion

        /// <summary>
        /// Gets or sets the dictionary size.
        /// </summary>
        public float DictionarySize
        {
            set
            {
                _memoryPressure = (int)(value * 1024 * 1024);
                GC.AddMemoryPressure(_memoryPressure);
            }
        }

        /// <summary>
        /// Raises events for the GetStream method.
        /// </summary>
        /// <param name="index">The current item index.</param>
        /// <returns>True if not cancelled; otherwise, false.</returns>
        private bool EventsForGetStream(uint index)
        {
            if (!FastCompression)
            {
                if (_fileStream != null)
                {
                    _fileStream.BytesRead += IntEventArgsHandler;
                }
                _doneRate += 1.0f / _actualFilesCount;
                var fiea = new FileNameEventArgs(_files != null? _files[index].Name : _entries[index],
                                                 PercentDoneEventArgs.ProducePercentDone(_doneRate));
                OnFileCompression(fiea);
                if (fiea.Cancel)
                {
                    Canceled = true;
                    return false;
                }
            }
            return true;
        }

        #region Events

        /// <summary>
        /// Occurs when the next file is going to be packed.
        /// </summary>
        /// <remarks>Occurs when 7-zip engine requests for an input stream for the next file to pack it</remarks>
        public event EventHandler<FileNameEventArgs> FileCompressionStarted;

        /// <summary>
        /// Occurs when data are being compressed.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Compressing;

        /// <summary>
        /// Occurs when the current file was compressed.
        /// </summary>
        public event EventHandler FileCompressionFinished;

        private void OnFileCompression(FileNameEventArgs e)
        {
            if (FileCompressionStarted != null)
            {
                FileCompressionStarted(this, e);
            }
        }

        private void OnCompressing(ProgressEventArgs e)
        {
            if (Compressing != null)
            {
                Compressing(this, e);
            }
        }

        private void OnFileCompressionFinished(EventArgs e)
        {
            if (FileCompressionFinished != null)
            {
                FileCompressionFinished(this, e);
            }
        }

        #endregion

        #region IArchiveUpdateCallback Members

        public void SetTotal(ulong total) {}

        public void SetCompleted(ref ulong completeValue) {}

        public int GetUpdateItemInfo(uint index, ref int newData, ref int newProperties, ref uint indexInArchive)
        {
            switch (_updateData.Mode)
            {
                case InternalCompressionMode.Create:
                    newData = 1;
                    newProperties = 1;
                    indexInArchive = UInt32.MaxValue;
                    break;
                case InternalCompressionMode.Append:
                    if (index < _indexInArchive)
                    {
                        newData = 0;
                        newProperties = 0;
                        indexInArchive = index;
                    }
                    else
                    {
                        newData = 1;
                        newProperties = 1;
                        indexInArchive = UInt32.MaxValue;
                    }
                    break;
                case InternalCompressionMode.Modify:
                    newData = 0;
                    newProperties = Convert.ToInt32(_updateData.FileNamesToModify.ContainsKey((int)index)
                        && _updateData.FileNamesToModify[(int)index] != null);
                    if (_updateData.FileNamesToModify.ContainsKey((int)index)
                        && _updateData.FileNamesToModify[(int)index] == null)
                    {
                        indexInArchive = (UInt32)_updateData.ArchiveFileData.Count;
                        foreach (KeyValuePair<Int32, string> pairModification in _updateData.FileNamesToModify)
                            if ((pairModification.Key <= index) && (pairModification.Value == null))
                            {
                                do
                                {
                                    indexInArchive--;
                                }
                                while ((indexInArchive > 0) && _updateData.FileNamesToModify.ContainsKey((Int32)indexInArchive)
                                    && (_updateData.FileNamesToModify[(Int32)indexInArchive] == null));
                            }
                    }
                    else
                    {
                        indexInArchive = index;
                    }
                    break;
            }
            return 0;
        }

        public int GetProperty(uint index, ItemPropId propID, ref PropVariant value)
        {
            index -= _indexOffset;
            try
            {
                switch (propID)
                {
                    case ItemPropId.IsAnti:
                        value.VarType = VarEnum.VT_BOOL;
                        value.UInt64Value = 0;
                        break;
                    case ItemPropId.Path:
                        #region Path

                        value.VarType = VarEnum.VT_BSTR;
                        string val = DefaultItemName;

                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_entries != null)
                                {
                                    val = _entries[index];
                                }
                            }
                            else
                            {
                                if (_directoryStructure)
                                {
                                    if (_rootLength > 0)
                                    {
                                        val = _files[index].FullName.Substring(_rootLength);
                                    }
                                    else
                                    {
                                        val = _files[index].FullName[0] + _files[index].FullName.Substring(2);
                                    }
                                }
                                else
                                {
                                    val = _files[index].Name;
                                }
                            }
                        }
                        else
                        {
                            val = _updateData.FileNamesToModify[(int) index];
                        }
                        value.Value = Marshal.StringToBSTR(val);
                        #endregion
                        break;
                    case ItemPropId.IsDirectory:
                        value.VarType = VarEnum.VT_BOOL;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_streams == null)
                                {
                                    value.UInt64Value = 0;
                                }
                                else
                                {
                                    value.UInt64Value = (ulong)(_streams[index] == null ? 1 : 0);
                                }
                            }
                            else
                            {
                                value.UInt64Value = (byte)(_files[index].Attributes & FileAttributes.Directory);
                            }
                        }
                        else
                        {
                            value.UInt64Value = Convert.ToUInt64(_updateData.ArchiveFileData[(int) index].IsDirectory);
                        }
                        break;
                    case ItemPropId.Size:
                        #region Size

                        value.VarType = VarEnum.VT_UI8;
                        UInt64 size;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_streams == null)
                                {
                                    size = _bytesCount > 0 ? (ulong) _bytesCount : 0;
                                }
                                else
                                {
                                    size = (ulong) (_streams[index] == null? 0 : _streams[index].Length);
                                }
                            }
                            else
                            {
                                size = (_files[index].Attributes & FileAttributes.Directory) == 0
                                           ? (ulong) _files[index].Length
                                           : 0;
                            }
                        }
                        else
                        {
                            size = _updateData.ArchiveFileData[(int) index].Size;
                        }
                        value.UInt64Value = size;

                        #endregion
                        break;
                    case ItemPropId.Attributes:
                        value.VarType = VarEnum.VT_UI4;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            if (_files == null)
                            {
                                if (_streams == null)
                                {
                                    value.UInt32Value = (uint)FileAttributes.Normal;
                                }
                                else
                                {
                                    value.UInt32Value = (uint)(_streams[index] == null ? FileAttributes.Directory : FileAttributes.Normal);
                                }
                            }
                            else
                            {
                                value.UInt32Value = (uint) _files[index].Attributes;
                            }
                        }
                        else
                        {
                            value.UInt32Value = _updateData.ArchiveFileData[(int) index].Attributes;
                        }
                        break;
                    #region Times
                    case ItemPropId.CreationTime:
                        value.VarType = VarEnum.VT_FILETIME;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            value.Int64Value = _files == null
                                               ? DateTime.Now.ToFileTime()
                                               : _files[index].CreationTime.ToFileTime();
                        }
                        else
                        {
                            value.Int64Value = _updateData.ArchiveFileData[(int) index].CreationTime.ToFileTime();
                        }
                        break;
                    case ItemPropId.LastAccessTime:
                        value.VarType = VarEnum.VT_FILETIME;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            value.Int64Value = _files == null
                                               ? DateTime.Now.ToFileTime()
                                               : _files[index].LastAccessTime.ToFileTime();
                        }
                        else
                        {
                            value.Int64Value = _updateData.ArchiveFileData[(int) index].LastAccessTime.ToFileTime();
                        }
                        break;
                    case ItemPropId.LastWriteTime:
                        value.VarType = VarEnum.VT_FILETIME;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            value.Int64Value = _files == null
                                               ? DateTime.Now.ToFileTime()
                                               : _files[index].LastWriteTime.ToFileTime();
                        }
                        else
                        {
                            value.Int64Value = _updateData.ArchiveFileData[(int) index].LastWriteTime.ToFileTime();
                        }
                        break;
                    #endregion
                    case ItemPropId.Extension:
                        #region Extension

                        value.VarType = VarEnum.VT_BSTR;
                        if (_updateData.Mode != InternalCompressionMode.Modify)
                        {
                            try
                            {
                                val = _files != null
                                      ? _files[index].Extension.Substring(1)
                                      : _entries == null
                                          ? ""
                                          : Path.GetExtension(_entries[index]);
                                value.Value = Marshal.StringToBSTR(val);
                            }
                            catch (ArgumentException)
                            {
                                value.Value = Marshal.StringToBSTR("");
                            }
                        }
                        else
                        {
                            val = Path.GetExtension(_updateData.ArchiveFileData[(int) index].FileName);
                            value.Value = Marshal.StringToBSTR(val);
                        }

                        #endregion
                        break;
                }
            }
            catch (Exception e)
            {
                AddException(e);
            }
            return 0;
        }

        /// <summary>
        /// Gets the stream for 7-zip library.
        /// </summary>
        /// <param name="index">File index</param>
        /// <param name="inStream">Input file stream</param>
        /// <returns>Zero if Ok</returns>
        public int GetStream(uint index, out ISequentialInStream inStream)
        {
            index -= _indexOffset;

            if (_files != null)
            {
                _fileStream = null;

                try
                {
                    if (File.Exists(_files[index].FullName))
                    {
                        _fileStream = new InStreamWrapper(
                            new FileStream(_files[index].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                            true);
                    }
                }
                catch (Exception e)
                {
                    AddException(e);
                    inStream = null;
                    return -1;
                }

                inStream = _fileStream;

                if (!EventsForGetStream(index))
                {
                    return -1;
                }
            }
            else
            {
                if (_streams == null)
                {
                    inStream = _fileStream;
                }
                else
                {
                    _fileStream = new InStreamWrapper(_streams[index], true);
                    inStream = _fileStream;
                    if (!EventsForGetStream(index))
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }

        public long EnumProperties(IntPtr enumerator)
        {
            //Not implemented HRESULT
            return 0x80004001L;
        }

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
                        AddException(new ExtractionFailedException("File is corrupted. Data error has occurred."));
                        break;
                    case OperationResult.UnsupportedMethod:
                        AddException(new ExtractionFailedException("Unsupported method error has occurred."));
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
            if (_fileStream != null)
            {
                _fileStream.BytesRead -= IntEventArgsHandler;

                //Specific Zip implementation - can not Dispose files for Zip.
                if (_compressor.ArchiveFormat != OutArchiveFormat.Zip)
                {
                    try
                    {
                        _fileStream.Dispose();                            
                    }
                    catch (ObjectDisposedException) {}
                }
                else
                {
                    _wrappersToDispose.Add(_fileStream);
                }                                
                
                _fileStream = null;
            }
            
            OnFileCompressionFinished(EventArgs.Empty);
        }

        #endregion

        #region ICryptoGetTextPassword2 Members

        public int CryptoGetTextPassword2(ref int passwordIsDefined, out string password)
        {
            passwordIsDefined = String.IsNullOrEmpty(Password) ? 0 : 1;
            password = Password;
            return 0;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            GC.RemoveMemoryPressure(_memoryPressure);

            if (_fileStream != null)
            {
                try
                {
                    _fileStream.Dispose();
                }
                catch (ObjectDisposedException) {}
            }

            if (_wrappersToDispose != null)
            {
                foreach (var wrapper in _wrappersToDispose)
                {
                    try
                    {
                        wrapper.Dispose();
                    }
                    catch (ObjectDisposedException) {}
                }
            }

            GC.SuppressFinalize(this);
        }

        #endregion

        private void IntEventArgsHandler(object sender, IntEventArgs e)
        {
            var lockObject = ((object) _files ?? _streams) ?? _fileStream;

            lock (lockObject)
            {
                var pold = (byte) (_bytesWrittenOld*100/_bytesCount);
                _bytesWritten += e.Value;
                byte pnow;

                if (_bytesCount < _bytesWritten) //Holy shit, this check for ZIP is golden
                {
                    pnow = 100;
                }
                else
                {
                    pnow = (byte)((_bytesWritten * 100) / _bytesCount);
                }

                if (pnow > pold)
                {
                    _bytesWrittenOld = _bytesWritten;
                    OnCompressing(new ProgressEventArgs(pnow, (byte) (pnow - pold)));
                }
            }
        }
    }
#endif
}
