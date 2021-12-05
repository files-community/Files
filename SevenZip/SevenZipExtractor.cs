namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// Class to unpack data from archives supported by 7-Zip.
    /// </summary>
    /// <example>
    /// using (var extr = new SevenZipExtractor(@"C:\Test.7z"))
    /// {
    ///     extr.ExtractArchive(@"C:\TestDirectory");
    /// }
    /// </example>
    public sealed partial class SevenZipExtractor
#if UNMANAGED
        : SevenZipBase, IDisposable
#endif
    {
#if UNMANAGED
        private List<ArchiveFileInfo> _archiveFileData;
        private IInArchive _archive;
        private IInStream _archiveStream;
        private int _offset;
        private ArchiveOpenCallback _openCallback;
        private string _fileName;
        private Stream _inStream;
        private long? _packedSize;
        private long? _unpackedSize;
        private uint? _filesCount;
        private bool? _isSolid;
        private bool _opened;
        private bool _disposed;
        private InArchiveFormat _format = (InArchiveFormat)(-1);
        private ReadOnlyCollection<ArchiveFileInfo> _archiveFileInfoCollection;
        private ReadOnlyCollection<ArchiveProperty> _archiveProperties;
        private ReadOnlyCollection<string> _volumeFileNames;

        /// <summary>
        /// This is used to lock possible Dispose() calls.
        /// </summary>
        private bool _asynchronousDisposeLock;

        #region Constructors

        /// <summary>
        /// General initialization function.
        /// </summary>
        /// <param name="archiveFullName">The archive file name.</param>
        private void Init(string archiveFullName)
        {
            _fileName = archiveFullName;
            var isExecutable = false;
            
            if ((int)_format == -1)
            {
                _format = FileChecker.CheckSignature(archiveFullName, out _offset, out isExecutable);
            }
            
            PreserveDirectoryStructure = true;
            SevenZipLibraryManager.LoadLibrary(this, _format);
            
            try
            {
                _archive = SevenZipLibraryManager.InArchive(_format, this);
            }
            catch (SevenZipLibraryException)
            {
                SevenZipLibraryManager.FreeLibrary(this, _format);
                throw;
            }
            
            if (isExecutable && _format != InArchiveFormat.PE)
            {
                if (!Check())
                {
                    CommonDispose();
                    _format = InArchiveFormat.PE;
                    SevenZipLibraryManager.LoadLibrary(this, _format);
                    
                    try
                    {
                        _archive = SevenZipLibraryManager.InArchive(_format, this);
                    }
                    catch (SevenZipLibraryException)
                    {
                        SevenZipLibraryManager.FreeLibrary(this, _format);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// General initialization function.
        /// </summary>
        /// <param name="stream">The stream to read the archive from.</param>
        private void Init(Stream stream)
        {
            ValidateStream(stream);
            var isExecutable = false;
            
            if ((int)_format == -1)
            {
                _format = FileChecker.CheckSignature(stream, out _offset, out isExecutable);
            }            
            
            PreserveDirectoryStructure = true;
            SevenZipLibraryManager.LoadLibrary(this, _format);
            
            try
            {
                _inStream = new ArchiveEmulationStreamProxy(stream, _offset);
				_packedSize = stream.Length;
                _archive = SevenZipLibraryManager.InArchive(_format, this);
            }
            catch (SevenZipLibraryException)
            {
                SevenZipLibraryManager.FreeLibrary(this, _format);
                throw;
            }
            
            if (isExecutable && _format != InArchiveFormat.PE)
            {
                if (!Check())
                {
                    CommonDispose();
                    _format = InArchiveFormat.PE;
                    
                    try
                    {
                        _inStream = new ArchiveEmulationStreamProxy(stream, _offset);
                        _packedSize = stream.Length;
                        _archive = SevenZipLibraryManager.InArchive(_format, this);
                    }
                    catch (SevenZipLibraryException)
                    {
                        SevenZipLibraryManager.FreeLibrary(this, _format);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveStream">The stream to read the archive from.
        /// Use SevenZipExtractor(string) to extract from disk, though it is not necessary.</param>
        /// <remarks>The archive format is guessed by the signature.</remarks>
        public SevenZipExtractor(Stream archiveStream)
        {
            Init(archiveStream);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveStream">The stream to read the archive from.
        /// Use SevenZipExtractor(string) to extract from disk, though it is not necessary.</param>
        /// <param name="format">Manual archive format setup. You SHOULD NOT normally specify it this way.
        /// Instead, use SevenZipExtractor(Stream archiveStream), that constructor
        /// automatically detects the archive format.</param>
        public SevenZipExtractor(Stream archiveStream, InArchiveFormat format)
        {
            _format = format;
            Init(archiveStream);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveFullName">The archive full file name.</param>
        public SevenZipExtractor(string archiveFullName)
        {
            Init(archiveFullName);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveFullName">The archive full file name.</param>
        /// <param name="format">Manual archive format setup. You SHOULD NOT normally specify it this way.
        /// Instead, use SevenZipExtractor(string archiveFullName), that constructor
        /// automatically detects the archive format.</param>
        public SevenZipExtractor(string archiveFullName, InArchiveFormat format)
        {
            _format = format;
            Init(archiveFullName);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveFullName">The archive full file name.</param>
        /// <param name="password">Password for an encrypted archive.</param>
        public SevenZipExtractor(string archiveFullName, string password)
            : base(password)
        {
            Init(archiveFullName);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveFullName">The archive full file name.</param>
        /// <param name="password">Password for an encrypted archive.</param>
        /// <param name="format">Manual archive format setup. You SHOULD NOT normally specify it this way.
        /// Instead, use SevenZipExtractor(string archiveFullName, string password), that constructor
        /// automatically detects the archive format.</param>
        public SevenZipExtractor(string archiveFullName, string password, InArchiveFormat format)
            : base(password)
        {
            _format = format;
            Init(archiveFullName);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveStream">The stream to read the archive from.</param>
        /// <param name="password">Password for an encrypted archive.</param>
        /// <remarks>The archive format is guessed by the signature.</remarks>
        public SevenZipExtractor(Stream archiveStream, string password)
            : base(password)
        {
            Init(archiveStream);
        }

        /// <summary>
        /// Initializes a new instance of SevenZipExtractor class.
        /// </summary>
        /// <param name="archiveStream">The stream to read the archive from.</param>
        /// <param name="password">Password for an encrypted archive.</param>
        /// <param name="format">Manual archive format setup. You SHOULD NOT normally specify it this way.
        /// Instead, use SevenZipExtractor(Stream archiveStream, string password), that constructor
        /// automatically detects the archive format.</param>
        public SevenZipExtractor(Stream archiveStream, string password, InArchiveFormat format)
            : base(password)
        {
            _format = format;
            Init(archiveStream);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets archive full file name
        /// </summary>
        public string FileName
        {
            get
            {
                DisposedCheck();

                return _fileName;
            }
        }        

        /// <summary>
        /// Gets the size of the archive file
        /// </summary>
        public long PackedSize
        {
            get
            {
                DisposedCheck();

                return _packedSize ?? (_fileName != null ?
                           new FileInfo(_fileName).Length :
                           -1);
            }
        }

        /// <summary>
        /// Gets the size of unpacked archive data
        /// </summary>
        public long UnpackedSize
        {
            get
            {
                DisposedCheck();

                if (!_unpackedSize.HasValue)
                {
                    return -1;
                }

                return _unpackedSize.Value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the archive is solid
        /// </summary>
        public bool IsSolid
        {
            get
            {
                DisposedCheck();
                
                if (!_isSolid.HasValue)
                {
                    GetArchiveInfo(true);
                }
                
                Debug.Assert(_isSolid != null);
                return _isSolid.Value;
            }
        }

        /// <summary>
        /// Gets the number of files in the archive
        /// </summary>
        [CLSCompliant(false)]
        public uint FilesCount
        {
            get
            {
                DisposedCheck();
                
                if (!_filesCount.HasValue)
                {
                    GetArchiveInfo(true);
                }
                
                Debug.Assert(_filesCount != null);
                return _filesCount.Value;                
            }
        }

        /// <summary>
        /// Gets archive format
        /// </summary>
        public InArchiveFormat Format
        {
            get
            {
                DisposedCheck();
                
                return _format;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating whether to preserve the directory structure of extracted files.
        /// </summary>
        public bool PreserveDirectoryStructure { get; set; }
        
        #endregion                

        /// <summary>
        /// Checked whether the class was disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException" />
        private void DisposedCheck()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SevenZipExtractor");
            }

            RecreateInstanceIfNeeded();
        }

        #region Core private functions

        private ArchiveOpenCallback GetArchiveOpenCallback()
        {
            return _openCallback ?? (_openCallback = string.IsNullOrEmpty(Password)
                                    ? new ArchiveOpenCallback(_fileName)
                                    : new ArchiveOpenCallback(_fileName, Password));
        }

        /// <summary>
        /// Gets the archive input stream.
        /// </summary>
        /// <returns>The archive input wrapper stream.</returns>
        private IInStream GetArchiveStream(bool dispose)
        {
            if (_archiveStream != null)
            {
                if (_archiveStream is DisposeVariableWrapper)
                {
                    (_archiveStream as DisposeVariableWrapper).DisposeStream = dispose;
                }
                return _archiveStream;
            }

            if (_inStream != null)
            {
                _inStream.Seek(0, SeekOrigin.Begin);
                _archiveStream = new InStreamWrapper(_inStream, false);
            }
            else
            {
                if (!_fileName.EndsWith(".001", StringComparison.OrdinalIgnoreCase))
                {
                    _archiveStream = new InStreamWrapper(
                        new ArchiveEmulationStreamProxy(new FileStream(
                            _fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                            _offset),
                        dispose);
                }
                else
                {
                    _archiveStream = new InMultiStreamWrapper(_fileName, dispose);
                    _packedSize = (_archiveStream as InMultiStreamWrapper).Length;
                }
            }

            return _archiveStream;
        }

        /// <summary>
        /// Opens the archive and throws exceptions or returns OperationResult.DataError if any error occurs.
        /// </summary>       
        /// <param name="archiveStream">The IInStream compliant class instance, that is, the input stream.</param>
        /// <param name="openCallback">The ArchiveOpenCallback instance.</param>
        /// <returns>OperationResult.Ok if Open() succeeds.</returns>
        private OperationResult OpenArchiveInner(IInStream archiveStream, IArchiveOpenCallback openCallback)
        {
            ulong checkPos = 1 << 15;
            var res = _archive.Open(archiveStream, ref checkPos, openCallback);
            
            return (OperationResult)res;
        }

        /// <summary>
        /// Opens the archive and throws exceptions or returns OperationResult.DataError if any error occurs.
        /// </summary>
        /// <param name="archiveStream">The IInStream compliant class instance, that is, the input stream.</param>
        /// <param name="openCallback">The ArchiveOpenCallback instance.</param>
        /// <returns>True if Open() succeeds; otherwise, false.</returns>
        private bool OpenArchive(IInStream archiveStream, ArchiveOpenCallback openCallback)
        {
            if (!_opened)
            {
                if (OpenArchiveInner(archiveStream, openCallback) != OperationResult.Ok)
                {
                    //if (!ThrowException(null, new SevenZipArchiveException()))
                    {
                        return false;
                    }
                }
                
                _volumeFileNames = new ReadOnlyCollection<string>(openCallback.VolumeFileNames);
                _opened = true;
            }

            return true;
        }

        /// <summary>
        /// Retrieves all information about the archive.
        /// </summary>
        /// <exception cref="SevenZip.SevenZipArchiveException"/>
        private void GetArchiveInfo(bool disposeStream)
        {
            if (_archive == null)
            {
                if (!ThrowException(null, new SevenZipArchiveException()))
                {
                    return;
                }
            }
            else
            {
                IInStream archiveStream;
                
                using ((archiveStream = GetArchiveStream(disposeStream)) as IDisposable)
                {
                    var openCallback = GetArchiveOpenCallback();
                    
                    if (!_opened)
                    {
                        if (!OpenArchive(archiveStream, openCallback))
                        {
                            return;
                        }
                        _opened = !disposeStream;
                    }

                    _filesCount = _archive.GetNumberOfItems();
                    _archiveFileData = new List<ArchiveFileInfo>((int)_filesCount);
                    
                    if (_filesCount != 0)
                    {
                        var data = new PropVariant();
                        
                        try
                        {
                            #region Getting archive items data

                            for (uint i = 0; i < _filesCount; i++)
                            {
                                try
                                {
                                    var fileInfo = new ArchiveFileInfo { Index = (int)i };
                                    _archive.GetProperty(i, ItemPropId.Path, ref data);
                                    fileInfo.FileName = NativeMethods.SafeCast(data, "[no name]");
                                    _archive.GetProperty(i, ItemPropId.LastWriteTime, ref data);
                                    fileInfo.LastWriteTime = NativeMethods.SafeCast(data, DateTime.Now);
                                    _archive.GetProperty(i, ItemPropId.CreationTime, ref data);
                                    fileInfo.CreationTime = NativeMethods.SafeCast(data, DateTime.Now);
                                    _archive.GetProperty(i, ItemPropId.LastAccessTime, ref data);
                                    fileInfo.LastAccessTime = NativeMethods.SafeCast(data, DateTime.Now);
                                    _archive.GetProperty(i, ItemPropId.Size, ref data);
                                    fileInfo.Size = NativeMethods.SafeCast<ulong>(data, 0);
                                    if (fileInfo.Size == 0)
                                    {
                                        fileInfo.Size = NativeMethods.SafeCast<uint>(data, 0);
                                    }
                                    _archive.GetProperty(i, ItemPropId.Attributes, ref data);
                                    fileInfo.Attributes = NativeMethods.SafeCast<uint>(data, 0);
                                    _archive.GetProperty(i, ItemPropId.IsDirectory, ref data);
                                    fileInfo.IsDirectory = NativeMethods.SafeCast(data, false);
                                    _archive.GetProperty(i, ItemPropId.Encrypted, ref data);
                                    fileInfo.Encrypted = NativeMethods.SafeCast(data, false);
                                    _archive.GetProperty(i, ItemPropId.Crc, ref data);
                                    fileInfo.Crc = NativeMethods.SafeCast<uint>(data, 0);
                                    _archive.GetProperty(i, ItemPropId.Comment, ref data);
                                    fileInfo.Comment = NativeMethods.SafeCast(data, "");
                                    _archive.GetProperty(i, ItemPropId.Method, ref data);
                                    fileInfo.Method = NativeMethods.SafeCast(data, "");
                                    _archiveFileData.Add(fileInfo);
                                }
                                catch (InvalidCastException)
                                {
                                    ThrowException(null, new SevenZipArchiveException("probably archive is corrupted."));
                                }
                            }

                            #endregion

                            #region Getting archive properties

                            var numProps = _archive.GetNumberOfArchiveProperties();
                            var archProps = new List<ArchiveProperty>((int)numProps);
                            
                            for (uint i = 0; i < numProps; i++)
                            {
                                _archive.GetArchivePropertyInfo(i, out var propName, out var propId, out var varType);
                                _archive.GetArchiveProperty(propId, ref data);

                                if (propId == ItemPropId.Solid)
                                {
                                    _isSolid = NativeMethods.SafeCast(data, true);
                                }
                                
                                // TODO Add more archive properties
                                if (PropIdToName.PropIdNames.ContainsKey(propId))
                                {
                                    archProps.Add(new ArchiveProperty
                                    {
                                        Name = PropIdToName.PropIdNames[propId],
                                        Value = data.Object
                                    });
                                }
                                else
                                {
                                    Debug.WriteLine($"An unknown archive property encountered (code {((int)propId).ToString(CultureInfo.InvariantCulture)})");
                                }
                            }

                            _archiveProperties = new ReadOnlyCollection<ArchiveProperty>(archProps);

                            if (!_isSolid.HasValue && _format == InArchiveFormat.Zip)
                            {
                                _isSolid = false;
                            }

                            if (!_isSolid.HasValue)
                            {
                                _isSolid = true;
                            }

                            #endregion
                        }
                        catch (Exception)
                        {
                            if (openCallback.ThrowException())
                            {
                                throw;
                            }
                        }
                    }
                }

                if (disposeStream)
                {
                    _archive.Close();
                    _archiveStream = null;
                }

                _archiveFileInfoCollection = new ReadOnlyCollection<ArchiveFileInfo>(_archiveFileData);
            }
        }

        /// <summary>
        /// Ensure that _archiveFileData is loaded.
        /// </summary>
        /// <param name="disposeStream">Dispose the archive stream after this operation.</param>
        private void InitArchiveFileData(bool disposeStream)
        {
            if (_archiveFileData == null)
            {
                GetArchiveInfo(disposeStream);
            }
        }

        /// <summary>
        /// Produces an array of indexes from 0 to the maximum value in the specified array
        /// </summary>
        /// <param name="indexes">The source array</param>
        /// <returns>The array of indexes from 0 to the maximum value in the specified array</returns>
        private static uint[] SolidIndexes(uint[] indexes)
        {
            var max = indexes.Aggregate(0, (current, i) => Math.Max(current, (int) i));

            if (max > 0)
            {
                max++;
                var res = new uint[max];

                for (var i = 0; i < max; i++)
                {
                    res[i] = (uint)i;
                }

                return res;
            }

            return indexes;
        }

        /// <summary>
        /// Checks whether all the indexes are valid.
        /// </summary>
        /// <param name="indexes">The indexes to check.</param>
        /// <returns>True is valid; otherwise, false.</returns>
        private static bool CheckIndexes(params int[] indexes)
        {
            return indexes.All(i => i >= 0);
        }

        private void ArchiveExtractCallbackCommonInit(ArchiveExtractCallback aec)
        {
            aec.Open += ((s, e) => { _unpackedSize = (long)e.TotalSize; });
            aec.FileExtractionStarted += FileExtractionStartedEventProxy;
            aec.FileExtractionFinished += FileExtractionFinishedEventProxy;            
            aec.Extracting += ExtractingEventProxy;
            aec.FileExists += FileExistsEventProxy;
        }

        /// <summary>
        /// Gets the IArchiveExtractCallback callback
        /// </summary>
        /// <param name="directory">The directory where extract the files</param>
        /// <param name="filesCount">The number of files to be extracted</param>
        /// <param name="actualIndexes">The list of actual indexes (solid archives support)</param>
        /// <returns>The ArchiveExtractCallback callback</returns>
        private ArchiveExtractCallback GetArchiveExtractCallback(string directory, int filesCount, List<uint> actualIndexes)
        {
            var aec = string.IsNullOrEmpty(Password) ? 
                new ArchiveExtractCallback(_archive, directory, filesCount, PreserveDirectoryStructure, actualIndexes, this) : 
                new ArchiveExtractCallback(_archive, directory, filesCount, PreserveDirectoryStructure, actualIndexes, Password, this);
            ArchiveExtractCallbackCommonInit(aec);

            return aec;
        }

        /// <summary>
        /// Gets the IArchiveExtractCallback callback
        /// </summary>
        /// <param name="stream">The stream where extract the file</param>
        /// <param name="index">The file index</param>
        /// <param name="filesCount">The number of files to be extracted</param>
        /// <returns>The ArchiveExtractCallback callback</returns>
        private ArchiveExtractCallback GetArchiveExtractCallback(Stream stream, uint index, int filesCount)
        {
            var aec = string.IsNullOrEmpty(Password)
                      ? new ArchiveExtractCallback(_archive, stream, filesCount, index, this)
                      : new ArchiveExtractCallback(_archive, stream, filesCount, index, Password, this);
            ArchiveExtractCallbackCommonInit(aec);

            return aec;
        }

        private void FreeArchiveExtractCallback(ArchiveExtractCallback callback)
        {
            callback.Open -= ((s, e) => { _unpackedSize = (long)e.TotalSize; });
            callback.FileExtractionStarted -= FileExtractionStartedEventProxy;
            callback.FileExtractionFinished -= FileExtractionFinishedEventProxy;
            callback.Extracting -= ExtractingEventProxy;
            callback.FileExists -= FileExistsEventProxy;
        }

        #endregion        
#endif

        /// <summary>
        /// Checks if the specified stream supports extraction.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        private static void ValidateStream(Stream stream)
        {
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

            if (!stream.CanSeek)
            {
                throw new ArgumentException("The specified stream can not seek.", nameof(stream));
            }

            if (stream.Length == 0)
            {
                throw new ArgumentException("The specified stream has zero length.", nameof(stream));
            }
        }

#if UNMANAGED

        #region IDisposable Members

        private void CommonDispose(bool preventDispose = false)
        {
            if (_opened)
            {
                try
                {
                    _archive?.Close();
                }
                catch (Exception) { }
                _opened = false;
            }

            _archive = null;
            _archiveFileData = null;
            _archiveProperties = null;
            _archiveFileInfoCollection = null;
            
	        if (_inStream != null && !preventDispose)
	        {
                _inStream.Dispose();
                _inStream = null;
	        }
                
	        if (_openCallback != null)
            {
                try
                {
                    _openCallback.Dispose();
                }
                catch (ObjectDisposedException) { }
                _openCallback = null;
            }
            
            if (_archiveStream != null)
            {
                if (_archiveStream is IDisposable)
                {
                    try
                    {
                        if (_archiveStream is DisposeVariableWrapper)
                        {
                            (_archiveStream as DisposeVariableWrapper).DisposeStream = !preventDispose;
                        }

                        (_archiveStream as IDisposable).Dispose();
                    }
                    catch (ObjectDisposedException) { }
                    _archiveStream = null;
                }
            }

            SevenZipLibraryManager.FreeLibrary(this, _format);
        }

        /// <summary>
        /// Releases the unmanaged resources used by SevenZipExtractor.
        /// </summary>
        public void Dispose()
        {
            if (_asynchronousDisposeLock)
            {
                throw new InvalidOperationException("SevenZipExtractor instance must not be disposed while making an asynchronous method call.");
            }

            if (!_disposed)
            {                
                CommonDispose();
            }

            _disposed = true;            
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Core public Members

        #region Events

        /// <summary>
        /// Occurs when a new file is going to be unpacked.
        /// </summary>
        /// <remarks>Occurs when 7-zip engine requests for an output stream for a new file to unpack in.</remarks>
        public event EventHandler<FileInfoEventArgs> FileExtractionStarted;        

        /// <summary>
        /// Occurs when a file has been successfully unpacked.
        /// </summary>
        public event EventHandler<FileInfoEventArgs> FileExtractionFinished;

        /// <summary>
        /// Occurs when the archive has been unpacked.
        /// </summary>
        public event EventHandler<EventArgs> ExtractionFinished;

        /// <summary>
        /// Occurs when data are being extracted.
        /// </summary>
        /// <remarks>Use this event for accurate progress handling and various ProgressBar.StepBy(e.PercentDelta) routines.</remarks>
        public event EventHandler<ProgressEventArgs> Extracting;

        /// <summary>
        /// Occurs during the extraction when a file already exists.
        /// </summary>
        public event EventHandler<FileOverwriteEventArgs> FileExists;

        #region Event proxies

        /// <summary>
        /// Event proxy for FileExtractionStarted.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FileExtractionStartedEventProxy(object sender, FileInfoEventArgs e)
        {
            OnEvent(FileExtractionStarted, e, true);
        }

        /// <summary>
        /// Event proxy for FileExtractionFinished.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FileExtractionFinishedEventProxy(object sender, FileInfoEventArgs e)
        {
            OnEvent(FileExtractionFinished, e, true);
        }

        /// <summary>
        /// Event proxy for Extracting.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ExtractingEventProxy(object sender, ProgressEventArgs e)
        {
            OnEvent(Extracting, e, false);
        }

        /// <summary>
        /// Event proxy for FileExists.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FileExistsEventProxy(object sender, FileOverwriteEventArgs e)
        {
            OnEvent(FileExists, e, true);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of ArchiveFileInfo with all information about files in the archive
        /// </summary>
        public ReadOnlyCollection<ArchiveFileInfo> ArchiveFileData
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);

                return _archiveFileInfoCollection;
            }
        }

        /// <summary>
        /// Gets the properties for the current archive
        /// </summary>
        public ReadOnlyCollection<ArchiveProperty> ArchiveProperties
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);

                return _archiveProperties;
            }
        }

        /// <summary>
        /// Gets the collection of all file names contained in the archive.
        /// </summary>
        /// <remarks>
        /// Each get recreates the collection
        /// </remarks>
        public ReadOnlyCollection<string> ArchiveFileNames
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);
                var fileNames = new List<string>(_archiveFileData.Count);

                fileNames.AddRange(_archiveFileData.Select(afi => afi.FileName));

                return new ReadOnlyCollection<string>(fileNames);
            }
        }

        /// <summary>
        /// Gets the list of archive volume file names.
        /// </summary>
        public ReadOnlyCollection<string> VolumeFileNames
        {
            get
            {
                DisposedCheck();
                InitArchiveFileData(true);

                return _volumeFileNames;
            }           
        }
        #endregion

        /// <summary>
        /// Performs the archive integrity test.
        /// </summary>
        /// <returns>True is the archive is ok; otherwise, false.</returns>
        public bool Check()
        {
            DisposedCheck();

            try
            {
                InitArchiveFileData(false);
                var archiveStream = GetArchiveStream(true);
                var openCallback = GetArchiveOpenCallback();
                
                if (!OpenArchive(archiveStream, openCallback))
                {
                    return false;
                }

                using (var aec = GetArchiveExtractCallback("", (int)_filesCount, null))
                {
                    try
                    {
                        CheckedExecute(
                            _archive.Extract(null, uint.MaxValue, 1, aec),
                            SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                    }
                    finally
                    {
                        FreeArchiveExtractCallback(aec);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                _archive?.Close();
                if (_archiveStream is IDisposable)
                    ((IDisposable)_archiveStream).Dispose();
                _archiveStream = null;
                _opened = false;
            }

            return true;
        }

        #region ExtractFile overloads

        /// <summary>
        /// Unpacks the file by its name to the specified stream.
        /// </summary>
        /// <param name="fileName">The file full name in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public void ExtractFile(string fileName, Stream stream)
        {
            DisposedCheck();

            InitArchiveFileData(false);
            var index = -1;
            
            foreach (var afi in _archiveFileData)
            {
                if (afi.FileName == fileName && !afi.IsDirectory)
                {
                    index = afi.Index;
                    break;
                }
            }

            if (index == -1)
            {
                if (!ThrowException(null, new ArgumentOutOfRangeException(
                                              nameof(fileName),
                                              "The specified file name was not found in the archive file table.")))
                {
                    return;
                }
            }
            else
            {
                ExtractFile(index, stream);
            }
        }

        /// <summary>
        /// Unpacks the file by its index to the specified stream.
        /// </summary>
        /// <param name="index">Index in the archive file table.</param>
        /// <param name="stream">The stream where the file is to be unpacked.</param>
        public void ExtractFile(int index, Stream stream)
        {
            DisposedCheck();
            ClearExceptions();
            
            if (!CheckIndexes(index))
            {
                if (!ThrowException(null, new ArgumentException("The index must be more or equal to zero.", nameof(index))))
                {
                    return;
                }
            }

            if (!stream.CanWrite)
            {
                if (!ThrowException(null, new ArgumentException("The specified stream can not be written.", nameof(stream))))
                {
                    return;
                }
            }

            InitArchiveFileData(false);
            
            if (index > _filesCount - 1)
            {
                if (!ThrowException(null, new ArgumentOutOfRangeException(
                                              nameof(index), "The specified index is greater than the archive files count.")))
                {
                    return;
                }
            }
            
            var archiveStream = GetArchiveStream(false);
            var openCallback = GetArchiveOpenCallback();

            if (!OpenArchive(archiveStream, openCallback))
            {
                return;
            }

            try
            {
                var indexes = new[] { (uint)index };
                var entry = _archiveFileData[index];

                if (_isSolid.Value && !entry.Method.Equals("Copy", StringComparison.InvariantCultureIgnoreCase))
                {
                    indexes = SolidIndexes(indexes);
                }
                
                using (var aec = GetArchiveExtractCallback(stream, (uint) index, indexes.Length))
                {
                    try
                    {
                        CheckedExecute(
                            _archive.Extract(indexes, (uint) indexes.Length, 0, aec),
                            SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                    }
                    finally
                    {
                        FreeArchiveExtractCallback(aec);
                    }
                }
            }
            catch (Exception)
            {
                if (openCallback.ThrowException())
                {
                    throw;
                }
            }

            OnEvent(ExtractionFinished, EventArgs.Empty, false);
            ThrowUserException();
        }

        #endregion

        #region ExtractFiles overloads

        /// <summary>
        /// Unpacks files by their indices to the specified directory.
        /// </summary>
        /// <param name="indexes">indexes of the files in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public void ExtractFiles(string directory, params int[] indexes)
        {
            DisposedCheck();
            ClearExceptions();

            if (!CheckIndexes(indexes))
            {
                if (!ThrowException(null, new ArgumentException("The indexes must be more or equal to zero.", nameof(indexes))))
                {
                    return;
                }
            }

            InitArchiveFileData(false);

            #region Indexes stuff

            var uindexes = new uint[indexes.Length];

            for (var i = 0; i < indexes.Length; i++)
            {
                uindexes[i] = (uint) indexes[i];
            }

            if (uindexes.Where(i => i >= _filesCount).Any(
                i => !ThrowException(null, 
                                     new ArgumentOutOfRangeException(nameof(indexes), 
                                                                    $"Index must be less than {_filesCount.Value.ToString(CultureInfo.InvariantCulture)}!"))))
            {
                return;
            }

            var origIndexes = new List<uint>(uindexes);
            origIndexes.Sort();
            uindexes = origIndexes.ToArray();
            
            if (_isSolid.Value)
            {
                uindexes = SolidIndexes(uindexes);
            }

            #endregion

            try
            {
                IInStream archiveStream;
                
                using ((archiveStream = GetArchiveStream(origIndexes.Count != 1)) as IDisposable)
                {
                    var openCallback = GetArchiveOpenCallback();
                    
                    if (!OpenArchive(archiveStream, openCallback))
                    {
                        return;
                    }

                    try
                    {
                        using (var aec = GetArchiveExtractCallback(directory, (int) _filesCount, origIndexes))
                        {
                            try
                            {
                                CheckedExecute(
                                    _archive.Extract(uindexes, (uint) uindexes.Length, 0, aec),
                                    SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                            }
                            finally
                            {
                                FreeArchiveExtractCallback(aec);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (openCallback.ThrowException())
                        {
                            throw;
                        }
                    }
                }

                OnEvent(ExtractionFinished, EventArgs.Empty, false);
            }
            finally
            {
                if (origIndexes.Count > 1)
                {
                    _archive?.Close();
                    _archiveStream = null;
                    _opened = false;
                }
            }

            ThrowUserException();
        }

        /// <summary>
        /// Unpacks files by their full names to the specified directory.
        /// </summary>
        /// <param name="fileNames">Full file names in the archive file table.</param>
        /// <param name="directory">Directory where the files are to be unpacked.</param>
        public void ExtractFiles(string directory, params string[] fileNames)
        {
            DisposedCheck();
            InitArchiveFileData(false);
            var indexes = new List<int>(fileNames.Length);
            var archiveFileNames = new List<string>(ArchiveFileNames);
            
            foreach (var fn in fileNames)
            {
                if (!archiveFileNames.Contains(fn))
                {
                    if (!ThrowException(null, new ArgumentOutOfRangeException(nameof(fileNames), $"File \"{fn}\" was not found in the archive file table.")))
                    {
                        return;
                    }
                }
                else
                {
                    foreach (var afi in _archiveFileData)
                    {
                        if (afi.FileName == fn && !afi.IsDirectory)
                        {
                            indexes.Add(afi.Index);
                            break;
                        }
                    }
                }
            }

            ExtractFiles(directory, indexes.ToArray());
        }

        /// <summary>
        /// Extracts files from the archive, giving a callback the choice what
        /// to do with each file. The order of the files is given by the archive.
        /// 7-Zip (and any other solid) archives are NOT supported.
        /// </summary>
        /// <param name="extractFileCallback">The callback to call for each file in the archive.</param>
        /// <exception cref="SevenZipExtractionFailedException">Thrown when trying to extract from solid archives.</exception>
        public void ExtractFiles(ExtractFileCallback extractFileCallback)
        {
            DisposedCheck();
            InitArchiveFileData(false);

            if (IsSolid)
            {
                throw new SevenZipExtractionFailedException("Solid archives are not supported.");
            }

            foreach (var archiveFileInfo in ArchiveFileData)
            {
                var extractFileCallbackArgs = new ExtractFileCallbackArgs(archiveFileInfo);
                extractFileCallback(extractFileCallbackArgs);

                if (extractFileCallbackArgs.CancelExtraction)
                {
                    break;
                }

                if (extractFileCallbackArgs.ExtractToStream != null || extractFileCallbackArgs.ExtractToFile != null)
                {
                    var  callDone = false;

                    try
                    {
                        if (extractFileCallbackArgs.ExtractToStream != null)
                        {
                            ExtractFile(archiveFileInfo.Index, extractFileCallbackArgs.ExtractToStream);
                        }
                        else
                        {
                            using (var file = new FileStream(extractFileCallbackArgs.ExtractToFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192))
                            {
                                ExtractFile(archiveFileInfo.Index, file);
                            }
                        }

                        callDone = true;
                    }
                    catch (Exception ex)
                    {
                        extractFileCallbackArgs.Exception = ex;
                        extractFileCallbackArgs.Reason = ExtractFileCallbackReason.Failure;
                        extractFileCallback(extractFileCallbackArgs);

                        if (!ThrowException(null, ex))
                        {
                            return;
                        }
                    }

                    if (callDone)
                    {
                        extractFileCallbackArgs.Reason = ExtractFileCallbackReason.Done;
                        extractFileCallback(extractFileCallbackArgs);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Unpacks the whole archive to the specified directory.
        /// </summary>
        /// <param name="directory">The directory where the files are to be unpacked.</param>
        public void ExtractArchive(string directory)
        {
            DisposedCheck();          
            ClearExceptions();
            InitArchiveFileData(false);

            try
            {
                IInStream archiveStream;

                using ((archiveStream = GetArchiveStream(true)) as IDisposable)
                {
                    var openCallback = GetArchiveOpenCallback();

                    if (!OpenArchive(archiveStream, openCallback))
                    {
                        return;
                    }

                    try
                    {
                        using (var aec = GetArchiveExtractCallback(directory, (int) _filesCount, null))
                        {
                            try
                            {
                                CheckedExecute(
                                    _archive.Extract(null, uint.MaxValue, 0, aec),
                                    SevenZipExtractionFailedException.DEFAULT_MESSAGE, aec);
                                OnEvent(ExtractionFinished, EventArgs.Empty, false);
                            }
                            finally
                            {
                                FreeArchiveExtractCallback(aec);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (openCallback.ThrowException())
                        {
                            throw;
                        }
                    }
                }
            }
            finally
            {
                _archive?.Close();
                _archiveStream = null;
                _opened = false;
            }

            ThrowUserException();
        }     
        
        #endregion

#endif

        #region LZMA SDK functions

        internal static byte[] GetLzmaProperties(Stream inStream, out long outSize)
        {
            var lzmAproperties = new byte[5];

            if (inStream.Read(lzmAproperties, 0, 5) != 5)
            {
                throw new LzmaException();
            }

            outSize = 0;

            for (var i = 0; i < 8; i++)
            {
                var b = inStream.ReadByte();

                if (b < 0)
                {
                    throw new LzmaException();
                }

                outSize |= ((long) (byte) b) << (i << 3);
            }

            return lzmAproperties;
        }

        /// <summary>
        /// Decompress the specified stream (C# inside)
        /// </summary>
        /// <param name="inStream">The source compressed stream</param>
        /// <param name="outStream">The destination uncompressed stream</param>
        /// <param name="inLength">The length of compressed data (null for inStream.Length)</param>
        /// <param name="codeProgressEvent">The event for handling the code progress</param>
        public static void DecompressStream(Stream inStream, Stream outStream, int? inLength, EventHandler<ProgressEventArgs> codeProgressEvent)
        {
            if (!inStream.CanRead || !outStream.CanWrite)
            {
                throw new ArgumentException("The specified streams are invalid.");
            }

            var decoder = new Decoder();
            var inSize = (inLength ?? inStream.Length) - inStream.Position;
            decoder.SetDecoderProperties(GetLzmaProperties(inStream, out var outSize));
            decoder.Code(inStream, outStream, inSize, outSize, new LzmaProgressCallback(inSize, codeProgressEvent));
        }

        /// <summary>
        /// Decompress byte array compressed with LZMA algorithm (C# inside)
        /// </summary>
        /// <param name="data">Byte array to decompress</param>
        /// <returns>Decompressed byte array</returns>
        public static byte[] ExtractBytes(byte[] data)
        {
            using (var inStream = new MemoryStream(data))
            {
                var decoder = new Decoder();
                inStream.Seek(0, 0);

                using (var outStream = new MemoryStream())
                {
                    decoder.SetDecoderProperties(GetLzmaProperties(inStream, out var outSize));
                    decoder.Code(inStream, outStream, inStream.Length - inStream.Position, outSize, null);
                    return outStream.ToArray();
                }
            }
        }

        #endregion
    }
}
