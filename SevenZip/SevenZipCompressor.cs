namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
#if NET45 || NETSTANDARD2_0
    using System.Security.Permissions;
#endif

    using SevenZip.Sdk;
    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// Class to pack data into archives supported by 7-Zip.
    /// </summary>
    /// <example>
    /// var compr = new SevenZipCompressor();
    /// compr.CompressDirectory(@"C:\Dir", @"C:\Archive.7z");
    /// </example>
    public sealed partial class SevenZipCompressor
#if UNMANAGED
        : SevenZipBase
#endif
    {
#if UNMANAGED

        #region Fields

        private bool _compressingFilesOnDisk;

        /// <summary>
        /// Gets or sets the archiving compression level.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; }

        private OutArchiveFormat _archiveFormat = OutArchiveFormat.Zip;
        private CompressionMethod _compressionMethod = CompressionMethod.Default;

        /// <summary>
        /// Gets the custom compression parameters - for advanced users only.
        /// </summary>
        public Dictionary<string, string> CustomParameters { get; private set; }

        private long _volumeSize;
        private string _archiveName;
        private Stream _inStream;
        private Stream InStream
        {
            get => _inStream;
            set
            {
                _inStream = value;
                _archiveFormat = Formats.OutForInFormats[FileChecker.CheckSignature(_inStream, out _, out _)];
                _inStream.Position = 0;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating whether to include empty directories to archives. Default is true.
        /// </summary>
        public bool IncludeEmptyDirectories { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether to preserve the directory root for CompressDirectory.
        /// </summary>
        public bool PreserveDirectoryRoot { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether to preserve the directory structure.
        /// </summary>
        public bool DirectoryStructure { get; set; }

        private bool _directoryCompress;

        /// <summary>
        /// Gets or sets the compression mode.
        /// </summary>
        public CompressionMode CompressionMode { get; set; }

        private UpdateData _updateData;
        private uint _oldFilesCount;

        /// <summary>
        /// Gets or sets the value indicating whether to encrypt 7-Zip archive headers.
        /// </summary>
        public bool EncryptHeaders { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether to compress files only open for writing.
        /// </summary>
        public bool ScanOnlyWritable { get; set; }

        /// <summary>
        /// Gets or sets the encryption method for zip archives.
        /// </summary>
        public ZipEncryptionMethod ZipEncryptionMethod { get; set; }

        /// <summary>
        /// Gets or sets the temporary folder path.
        /// </summary>
        public string TempFolderPath { get; set; }

        /// <summary>
        /// Gets or sets the default archive item name used when an item to be compressed has no name, 
        /// for example, when you compress a MemoryStream instance.
        /// </summary>
        public string DefaultItemName { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether to compress as fast as possible, without calling events.
        /// </summary>
        public bool FastCompression { get; set; }

#endregion

#endif
        private static volatile int _lzmaDictionarySize = 1 << 22;

#if UNMANAGED

        private void CommonInit()
        {
            DirectoryStructure = true;
            IncludeEmptyDirectories = true;
            CompressionLevel = CompressionLevel.Normal;
            CompressionMode = CompressionMode.Create;
            ZipEncryptionMethod = ZipEncryptionMethod.ZipCrypto;
            CustomParameters = new Dictionary<string, string>();
            _updateData = new UpdateData();
            DefaultItemName = "default";
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressor class. 
        /// </summary>
        public SevenZipCompressor()
        {
            try
            {
                TempFolderPath = Path.GetTempPath();
            }
            catch (System.Security.SecurityException) // Registry access is not allowed, etc.
            {
                throw new SevenZipCompressionFailedException("Path.GetTempPath() threw a System.Security.SecurityException. You must call SevenZipCompressor constructor overload with your own temporary path.");
            }

            CommonInit();
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressor class. 
        /// </summary>
        /// <param name="temporaryPath">Your own temporary path (default is set in the parameterless constructor overload.)</param>
        public SevenZipCompressor(string temporaryPath)
        {
            TempFolderPath = temporaryPath;

            if (!Directory.Exists(TempFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(TempFolderPath);
                }
                catch (Exception)
                {
                    throw new SevenZipCompressionFailedException("The specified temporary path is invalid.");
                }
            }

            CommonInit();
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipCompressor class. 
        /// </summary>
        /// <param name="inStream">Your own existing archive stream.</param>
        public SevenZipCompressor(Stream inStream)
        {
            InStream = inStream;

            CommonInit();
        }
#endif

        /// <summary>
        /// Checks if the specified stream supports compression.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        private static void ValidateStream(Stream stream)
        {
            if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new ArgumentException("The specified stream can not seek or is not writable.", nameof(stream));
            }
        }

#if UNMANAGED

        #region Private functions

        private IOutArchive MakeOutArchive(IInStream inArchiveStream)
        {
            var inArchive = SevenZipLibraryManager.InArchive(Formats.InForOutFormats[_archiveFormat], this);

            using (var openCallback = GetArchiveOpenCallback())
            {
                ulong checkPos = 1 << 15;

                if (inArchive.Open(inArchiveStream, ref checkPos, openCallback) != (int) OperationResult.Ok)
                {
                    if (!ThrowException(null, new SevenZipArchiveException("Can not update the archive: Open() failed.")))
                    {
                        return null;
                    }
                }

                _oldFilesCount = inArchive.GetNumberOfItems();
            }

            return (IOutArchive) inArchive;
        }

        /// <summary>
        /// Guaranties the correct work of the SetCompressionProperties function
        /// </summary>
        /// <param name="method">The compression method to check</param>
        /// <returns>The value indicating whether the specified method is valid for the current ArchiveFormat</returns>
        private bool MethodIsValid(CompressionMethod method)
        {
            if (method == CompressionMethod.Default)
            {
                return true;
            }

            // TODO: Decide what to do with a returned "false" from this method!

            switch (_archiveFormat)
            {
                case OutArchiveFormat.GZip:
                {
                    return method == CompressionMethod.Deflate;
                }
                case OutArchiveFormat.BZip2:
                {
                    return method == CompressionMethod.BZip2;
                }
                case OutArchiveFormat.SevenZip:
                {
                    return method != CompressionMethod.Deflate && method != CompressionMethod.Deflate64;
                }
                case OutArchiveFormat.Tar:
                {
                    return method == CompressionMethod.Copy;
                }
                case OutArchiveFormat.Zip:
                {
                    return method != CompressionMethod.Lzma2;
                }
                default:
                {
                    return true;
                }
            }
        }

        private bool SwitchIsInCustomParameters(string name)
        {
            return CustomParameters.ContainsKey(name);
        }

        /// <summary>
        /// Sets the compression properties
        /// </summary>
        private void SetCompressionProperties()
        {
            switch (_archiveFormat)
            {
                case OutArchiveFormat.Tar:
                {
                    break;
                }
                default:
                {
                    var setter =
                        CompressionMode == CompressionMode.Create && _updateData.FileNamesToModify == null ? 
                            (ISetProperties) SevenZipLibraryManager.OutArchive(_archiveFormat, this) : 
                            (ISetProperties) SevenZipLibraryManager.InArchive(Formats.InForOutFormats[_archiveFormat], this);
                    
                    if (setter == null)
                    {
                        if (!ThrowException(null, new CompressionFailedException("The specified archive format is unsupported.")))
                        {
                            return;
                        }
                    }

                    if (_volumeSize > 0 && ArchiveFormat != OutArchiveFormat.SevenZip)
                    {
                        throw new CompressionFailedException("Unfortunately, the creation of multi-volume non-7Zip archives is not implemented.");
                    }

#region Check for "forbidden" parameters

                    if (CustomParameters.ContainsKey("x"))
                    {
                        if (!ThrowException(null, new CompressionFailedException("Use the \"CompressionLevel\" property instead of the \"x\" parameter.")))
                        {
                            return;
                        }
                    }

                    if (CustomParameters.ContainsKey("em"))
                    {
                        if (!ThrowException(null, new CompressionFailedException("Use the \"ZipEncryptionMethod\" property instead of the \"em\" parameter.")))
                        {
                            return;
                        }
                    }

                    if (CustomParameters.ContainsKey("m"))
                    {
                        if (!ThrowException(null, new CompressionFailedException("Use the \"CompressionMethod\" property instead of the \"m\" parameter.")))
                        {
                            return;
                        }
                    }

#endregion

                    var names = new List<IntPtr>(2 + CustomParameters.Count);
                    var values = new List<PropVariant>(2 + CustomParameters.Count);

#if NET45 || NETSTANDARD2_0
                    var sp = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
                    sp.Demand();
#endif

#region Initialize compression properties

                        names.Add(Marshal.StringToBSTR("x"));
                    values.Add(new PropVariant());

                    if (_compressionMethod != CompressionMethod.Default)
                    {
                        names.Add(_archiveFormat == OutArchiveFormat.Zip ? 
                            Marshal.StringToBSTR("m") : 
                            Marshal.StringToBSTR("0"));

                        var pv = new PropVariant
                        {
                            VarType = VarEnum.VT_BSTR,
                            Value = Marshal.StringToBSTR(Formats.MethodNames[_compressionMethod])
                        };

                        values.Add(pv);
                    }

                    foreach (var pair in CustomParameters)
                    {
                        #region Validate parameters against compression method.

                        if (_compressionMethod != CompressionMethod.Ppmd && (pair.Key.Equals("mem") || pair.Key.Equals("o")))
                        {
                            ThrowException(null, new CompressionFailedException($"Parameter \"{pair.Key}\" is only valid with the PPMd compression method."));
                        }

                        #endregion

                        names.Add(Marshal.StringToBSTR(pair.Key));
                        var pv = new PropVariant();

                        if (pair.Value.All(char.IsDigit))
                        {
                            pv.VarType = VarEnum.VT_UI4;
                            pv.UInt32Value = Convert.ToUInt32(pair.Value, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            pv.VarType = VarEnum.VT_BSTR;
                            pv.Value = Marshal.StringToBSTR(pair.Value);
                        }

                        values.Add(pv);
                    }

#endregion

#region Set compression level

                    var clpv = values[0];
                    clpv.VarType = VarEnum.VT_UI4;

                    switch (CompressionLevel)
                    {
                        case CompressionLevel.None:
                        {
                            clpv.UInt32Value = 0;
                            break;
                        }
                        case CompressionLevel.Fast:
                        {
                            clpv.UInt32Value = 1;
                            break;
                        }
                        case CompressionLevel.Low:
                        {
                            clpv.UInt32Value = 3;
                            break;
                        }
                        case CompressionLevel.Normal:
                        {
                            clpv.UInt32Value = 5;
                            break;
                        }
                        case CompressionLevel.High:
                        {
                            clpv.UInt32Value = 7;
                            break;
                        }
                        case CompressionLevel.Ultra:
                        {
                            clpv.UInt32Value = 9;
                            break;
                        }
                    }

                    values[0] = clpv;

#endregion

#region Encrypt headers

                    if (EncryptHeaders && _archiveFormat == OutArchiveFormat.SevenZip && !SwitchIsInCustomParameters("he"))
                    {
                        names.Add(Marshal.StringToBSTR("he"));
                        var tmp = new PropVariant {VarType = VarEnum.VT_BSTR, Value = Marshal.StringToBSTR("on")};
                        values.Add(tmp);
                    }

#endregion

#region Zip Encryption

                    if (_archiveFormat == OutArchiveFormat.Zip &&
                        ZipEncryptionMethod != ZipEncryptionMethod.ZipCrypto &&
                        !SwitchIsInCustomParameters("em"))
                    {
                        names.Add(Marshal.StringToBSTR("em"));

                        var tmp = new PropVariant
                        {
                            VarType = VarEnum.VT_BSTR,
                            Value = Marshal.StringToBSTR(Enum.GetName(typeof(ZipEncryptionMethod), ZipEncryptionMethod))
                        };

                        values.Add(tmp);
                    }

#endregion

                    var namesHandle = GCHandle.Alloc(names.ToArray(), GCHandleType.Pinned);
                    var valuesHandle = GCHandle.Alloc(values.ToArray(), GCHandleType.Pinned);

                    try
                    {
                        setter?.SetProperties(namesHandle.AddrOfPinnedObject(), valuesHandle.AddrOfPinnedObject(), names.Count);
                    }
                    finally
                    {
                        namesHandle.Free();
                        valuesHandle.Free();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Finds the common root of file names
        /// </summary>
        /// <param name="files">Array of file names</param>
        /// <returns>Common root</returns>
        private static int CommonRoot(ICollection<string> files)
        {
            var splitFileNames = new List<string[]>(files.Count);

            splitFileNames.AddRange(files.Select(fn => fn.Split(Path.DirectorySeparatorChar)));
            var minSplitLength = splitFileNames[0].Length - 1;

            if (files.Count > 1)
            {
                for (var i = 1; i < files.Count; i++)
                {
                    if (minSplitLength > splitFileNames[i].Length)
                    {
                        minSplitLength = splitFileNames[i].Length;
                    }
                }
            }

            var res = "";

            for (var i = 0; i < minSplitLength; i++)
            {
                var common = true;

                for (var j = 1; j < files.Count; j++)
                {
                    if (!(common &= splitFileNames[j - 1][i] == splitFileNames[j][i]))
                    {
                        break;
                    }
                }

                if (common)
                {
                    res += splitFileNames[0][i] + Path.DirectorySeparatorChar;
                }
                else
                {
                    break;
                }
            }

            return res.Length;
        }

        /// <summary>
        /// Validates the common root
        /// </summary>
        /// <param name="commonRootLength">The length of the common root of the file names.</param>
        /// <param name="files">Array of file names</param>
        private static void CheckCommonRoot(IReadOnlyList<string> files, ref int commonRootLength)
        {
            string commonRoot;

            try
            {
                commonRoot = files[0].Substring(0, commonRootLength);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new SevenZipInvalidFileNamesException("invalid common root.");
            }

            if (commonRoot.EndsWith(new string(Path.DirectorySeparatorChar, 1), StringComparison.CurrentCulture))
            {
                commonRoot = commonRoot.Substring(0, commonRootLength - 1);
                commonRootLength--;
            }

            if (files.Any(fn => !fn.StartsWith(commonRoot, StringComparison.CurrentCulture)))
            {
                throw new SevenZipInvalidFileNamesException("invalid common root.");
            }
        }

        /// <summary>
        /// Ensures that directory directory is not empty
        /// </summary>
        /// <param name="directory">Directory name</param>
        /// <returns>False if is not empty</returns>
        private static bool RecursiveDirectoryEmptyCheck(string directory)
        {
            var di = new DirectoryInfo(directory);

            if (di.GetFiles().Length > 0)
            {
                return false;
            }

            var empty = true;

            foreach (var cdi in di.GetDirectories())
            {
                empty &= RecursiveDirectoryEmptyCheck(cdi.FullName);
                if (!empty)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Makes special FileInfo array for the archive file table.
        /// </summary>
        /// <param name="files">Array of files to pack.</param>
        /// <param name="commonRootLength">The length of the common root of file names</param>
        /// <param name="directoryCompress">The value indicating whether to produce the array for files in a particular directory or just for an array of files.</param>
        /// <param name="directoryStructure">Preserve directory structure.</param>
        /// <returns>Special FileInfo array for the archive file table.</returns>
        private static FileInfo[] ProduceFileInfoArray(
            IReadOnlyList<string> files, int commonRootLength,
            bool directoryCompress, bool directoryStructure)
        {
            var fis = new List<FileInfo>(files.Count);
            var commonRoot = files[0].Substring(0, commonRootLength);

            if (directoryCompress)
            {
                fis.AddRange(files.Select(fn => new FileInfo(fn)));
            }
            else
            {
                if (!directoryStructure)
                {
                    fis.AddRange(from fn in files where !Directory.Exists(fn) select new FileInfo(fn));
                }
                else
                {
                    var fns = new List<string>(files.Count);
                    CheckCommonRoot(files, ref commonRootLength);

                    if (commonRootLength > 0)
                    {
                        commonRootLength++;

                        foreach (var f in files)
                        {
                            var splitAfn = f.Substring(commonRootLength).Split(Path.DirectorySeparatorChar);
                            var cfn = commonRoot;

                            foreach (var t in splitAfn)
                            {
                                cfn += Path.DirectorySeparatorChar + t;

                                if (!fns.Contains(cfn))
                                {
                                    fis.Add(new FileInfo(cfn));
                                    fns.Add(cfn);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var f in files)
                        {
                            var splitAfn = f.Substring(commonRootLength).Split(Path.DirectorySeparatorChar);
                            var cfn = splitAfn[0];

                            for (var i = 1; i < splitAfn.Length; i++)
                            {
                                cfn += Path.DirectorySeparatorChar + splitAfn[i];

                                if (!fns.Contains(cfn))
                                {
                                    fis.Add(new FileInfo(cfn));
                                    fns.Add(cfn);
                                }
                            }
                        }
                    }
                }
            }

            return fis.ToArray();
        }

        /// <summary>
        /// Recursive function for adding files in directory
        /// </summary>
        /// <param name="directory">Directory directory</param>
        /// <param name="files">List of files</param>
        /// <param name="searchPattern">Search string, such as "*.txt"</param>
        private void AddFilesFromDirectory(string directory, ICollection<string> files, string searchPattern)
        {
            var di = new DirectoryInfo(directory);

            foreach (var fi in di.GetFiles(searchPattern))
            {
                if (!ScanOnlyWritable)
                {
                    files.Add(fi.FullName);
                }
                else
                {
                    try
                    {
                        using (fi.OpenWrite())
                        {
                        }

                        files.Add(fi.FullName);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            foreach (var cdi in di.GetDirectories())
            {
                if (IncludeEmptyDirectories)
                {
                    files.Add(cdi.FullName);
                }

                AddFilesFromDirectory(cdi.FullName, files, searchPattern);
            }
        }

#endregion

        #region GetArchiveUpdateCallback overloads

        /// <summary>
        /// Performs the common ArchiveUpdateCallback initialization.
        /// </summary>
        /// <param name="auc">The ArchiveUpdateCallback instance to initialize.</param>
        private void CommonUpdateCallbackInit(ArchiveUpdateCallback auc)
        {
            auc.FileCompressionStarted += FileCompressionStartedEventProxy;
            auc.Compressing += CompressingEventProxy;
            auc.FileCompressionFinished += FileCompressionFinishedEventProxy;
            auc.DefaultItemName = DefaultItemName;
            auc.FastCompression = FastCompression;
        }

        private float GetDictionarySize()
        {
            var dictionarySize = 0.001f;

            switch (_compressionMethod)
            {
                case CompressionMethod.Default:
                case CompressionMethod.Lzma:
                case CompressionMethod.Lzma2:
                {
                    switch (CompressionLevel)
                    {
                        case CompressionLevel.None:
                        {
                            dictionarySize = 0.001f;
                            break;
                        }
                        case CompressionLevel.Fast:
                        {
                            dictionarySize = 1.0f / 16 * 7.5f + 4;
                            break;
                        }
                        case CompressionLevel.Low:
                        {
                            dictionarySize = 7.5f * 11.5f + 4;
                            break;
                        }
                        case CompressionLevel.Normal:
                        {
                            dictionarySize = 16 * 11.5f + 4;
                            break;
                        }
                        case CompressionLevel.High:
                        {
                            dictionarySize = 32 * 11.5f + 4;
                            break;
                        }
                        case CompressionLevel.Ultra:
                        {
                            dictionarySize = 64 * 11.5f + 4;
                            break;
                        }
                    }

                    break;
                }
                case CompressionMethod.BZip2:
                {
                    switch (CompressionLevel)
                    {
                        case CompressionLevel.None:
                        {
                            dictionarySize = 0;
                            break;
                        }
                        case CompressionLevel.Fast:
                        {
                            dictionarySize = 0.095f;
                            break;
                        }
                        case CompressionLevel.Low:
                        {
                            dictionarySize = 0.477f;
                            break;
                        }
                        case CompressionLevel.Normal:
                        case CompressionLevel.High:
                        case CompressionLevel.Ultra:
                        {
                            dictionarySize = 0.858f;
                            break;
                        }
                    }

                    break;
                }
                case CompressionMethod.Deflate:
                case CompressionMethod.Deflate64:
                {
                    dictionarySize = 32;
                    break;
                }
                case CompressionMethod.Ppmd:
                {
                    dictionarySize = 16;
                    break;
                }
            }

            return dictionarySize;
        }

        /// <summary>
        /// Produces  a new instance of ArchiveUpdateCallback class.
        /// </summary>
        /// <param name="files">Array of FileInfo - files to pack</param>
        /// <param name="rootLength">Length of the common root of file names</param>
        /// <param name="password">The archive password</param>
        /// <returns></returns>
        private ArchiveUpdateCallback GetArchiveUpdateCallback(
            FileInfo[] files, int rootLength, string password)
        {
            SetCompressionProperties();
            var auc = (string.IsNullOrEmpty(password))
                ? new ArchiveUpdateCallback(files, rootLength, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()}
                : new ArchiveUpdateCallback(files, rootLength, password, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()};
            CommonUpdateCallbackInit(auc);

            return auc;
        }

        /// <summary>
        /// Produces  a new instance of ArchiveUpdateCallback class.
        /// </summary>
        /// <param name="inStream">The archive input stream.</param>
        /// <param name="password">The archive password.</param>
        /// <returns></returns>
        private ArchiveUpdateCallback GetArchiveUpdateCallback(Stream inStream, string password)
        {
            SetCompressionProperties();
            var auc = (string.IsNullOrEmpty(password))
                ? new ArchiveUpdateCallback(inStream, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()}
                : new ArchiveUpdateCallback(inStream, password, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()};
            CommonUpdateCallbackInit(auc);

            return auc;
        }

        /// <summary>
        /// Produces  a new instance of ArchiveUpdateCallback class.
        /// </summary>
        /// <param name="streamDict">Dictionary&lt;name of the archive entry, stream&gt;.</param>
        /// <param name="password">The archive password</param>
        /// <returns></returns>
        private ArchiveUpdateCallback GetArchiveUpdateCallback(
            IDictionary<string, Stream> streamDict, string password)
        {
            SetCompressionProperties();
            var auc = (string.IsNullOrEmpty(password))
                ? new ArchiveUpdateCallback(streamDict, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()}
                : new ArchiveUpdateCallback(streamDict, password, this, GetUpdateData(), DirectoryStructure)
                    {DictionarySize = GetDictionarySize()};
            CommonUpdateCallbackInit(auc);

            return auc;
        }

#endregion

        #region Service "Get" functions

        private void FreeCompressionCallback(ArchiveUpdateCallback callback)
        {
            callback.FileCompressionStarted -= FileCompressionStartedEventProxy;
            callback.Compressing -= CompressingEventProxy;
            callback.FileCompressionFinished -= FileCompressionFinishedEventProxy;
        }

        private string GetTempArchiveFileName(string archiveName)
        {
            return Path.Combine(TempFolderPath, Path.GetFileName(archiveName) + ".~");
        }

        private Stream GetArchiveFileStream()
        {
            if ((CompressionMode != CompressionMode.Create || _updateData.FileNamesToModify != null) && _inStream == null && !File.Exists(_archiveName))
            {
                if (
                    !ThrowException(null,
                        new CompressionFailedException("file \"" + _archiveName + "\" does not exist.")))
                {
                    return null;
                }
            }

            return _volumeSize == 0
                ? CompressionMode == CompressionMode.Create && _updateData.FileNamesToModify == null
                    ? File.Create(_archiveName)
                    : File.Create(GetTempArchiveFileName(_archiveName))
                : null;
        }

        private void FinalizeUpdate()
        {
            if (_volumeSize == 0 && (CompressionMode != CompressionMode.Create || _updateData.FileNamesToModify != null))
            {
                File.Move(GetTempArchiveFileName(_archiveName), _archiveName);
            }
        }

        private UpdateData GetUpdateData()
        {
            if (_updateData.FileNamesToModify == null)
            {
                var updateData = new UpdateData {Mode = (InternalCompressionMode) ((int) CompressionMode)};
                switch (CompressionMode)
                {
                    case CompressionMode.Create:
                    {
                        updateData.FilesCount = uint.MaxValue;
                        break;
                    }
                    case CompressionMode.Append:
                    {
                        updateData.FilesCount = _oldFilesCount;
                        break;
                    }
                }

                return updateData;
            }

            return _updateData;
        }

        private ISequentialOutStream GetOutStream(Stream outStream)
        {
            if (!_compressingFilesOnDisk)
            {
                return new OutStreamWrapper(outStream, false);
            }

            if (_volumeSize == 0 || CompressionMode != CompressionMode.Create || _updateData.FileNamesToModify != null)
            {
                return new OutStreamWrapper(outStream, true);
            }

            return new OutMultiStreamWrapper(_archiveName, _volumeSize);
        }

        private IInStream GetInStream()
        {
            if (_inStream != null)
            {
                return new InStreamWrapper(_inStream, true);
            }
            else
            {
                return File.Exists(_archiveName) &&
                   (CompressionMode != CompressionMode.Create && _compressingFilesOnDisk ||
                    _updateData.FileNamesToModify != null)
                ? new InStreamWrapper(
                    new FileStream(_archiveName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                    true)
                : null;
            }
        }

        private ArchiveOpenCallback GetArchiveOpenCallback()
        {
            return string.IsNullOrEmpty(Password)
                ? new ArchiveOpenCallback(_archiveName)
                : new ArchiveOpenCallback(_archiveName, Password);
        }

#endregion

#region Core public Members

        #region Events

        /// <summary>
        /// Occurs when the next file is going to be packed.
        /// </summary>
        /// <remarks>Occurs when 7-zip engine requests for an input stream for the next file to pack it</remarks>
        public event EventHandler<FileNameEventArgs> FileCompressionStarted;

        /// <summary>
        /// Occurs when the current file was compressed.
        /// </summary>
        public event EventHandler<EventArgs> FileCompressionFinished;

        /// <summary>
        /// Occurs when data are being compressed
        /// </summary>
        /// <remarks>Use this event for accurate progress handling and various ProgressBar.StepBy(e.PercentDelta) routines</remarks>
        public event EventHandler<ProgressEventArgs> Compressing;

        /// <summary>
        /// Occurs when all files information was determined and SevenZipCompressor is about to start to compress them.
        /// </summary>
        /// <remarks>The incoming int value indicates the number of scanned files.</remarks>
        public event EventHandler<IntEventArgs> FilesFound;

        /// <summary>
        /// Occurs when the compression procedure is finished
        /// </summary>
        public event EventHandler<EventArgs> CompressionFinished;

        #region Event proxies

        /// <summary>
        /// Event proxy for FileCompressionStarted.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FileCompressionStartedEventProxy(object sender, FileNameEventArgs e)
        {
            OnEvent(FileCompressionStarted, e, false);
        }

        /// <summary>
        /// Event proxy for FileCompressionFinished.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FileCompressionFinishedEventProxy(object sender, EventArgs e)
        {
            OnEvent(FileCompressionFinished, e, false);
        }

        /// <summary>
        /// Event proxy for Compressing.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void CompressingEventProxy(object sender, ProgressEventArgs e)
        {
            OnEvent(Compressing, e, false);
        }

        /// <summary>
        /// Event proxy for FilesFound.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FilesFoundEventProxy(object sender, IntEventArgs e)
        {
            OnEvent(FilesFound, e, false);
        }

#endregion

#endregion

        #region Properties

        /// <summary>
        /// Gets or sets the archive format
        /// </summary>
        public OutArchiveFormat ArchiveFormat
        {
            get => _archiveFormat;

            set
            {
                _archiveFormat = value;

                if (!MethodIsValid(_compressionMethod))
                {
                    _compressionMethod = CompressionMethod.Default;
                }
            }
        }

        /// <summary>
        /// Gets or sets the compression method
        /// </summary>
        public CompressionMethod CompressionMethod
        {
            get => _compressionMethod;

            set => _compressionMethod = !MethodIsValid(value) ? CompressionMethod.Default : value;
        }

        /// <summary>
        /// Gets or sets the size in bytes of an archive volume (0 for no volumes).
        /// </summary>
        public long VolumeSize
        {
            get => _volumeSize;

            set => _volumeSize = value > 0 ? value : 0;
        }

#endregion

        #region CompressFiles overloads

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="archiveName">The archive file name.</param>
        public void CompressFiles(
            string archiveName, params string[] fileFullNames)
        {
            CompressFilesEncrypted(archiveName, string.Empty, fileFullNames);
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="archiveStream">The archive output stream. 
        /// Use CompressFiles(string archiveName ... ) overloads for archiving to disk.</param>       
        public void CompressFiles(
            Stream archiveStream, params string[] fileFullNames)
        {
            CompressFilesEncrypted(archiveStream, string.Empty, fileFullNames);
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="commonRootLength">The length of the common root of the file names.</param>
        /// <param name="archiveName">The archive file name.</param>
        public void CompressFiles(
            string archiveName, int commonRootLength, params string[] fileFullNames)
        {
            CompressFilesEncrypted(archiveName, commonRootLength, string.Empty, fileFullNames);
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="commonRootLength">The length of the common root of the file names.</param>
        /// <param name="archiveStream">The archive output stream.
        /// Use CompressFiles(string archiveName, ... ) overloads for archiving to disk.</param>
        public void CompressFiles(
            Stream archiveStream, int commonRootLength, params string[] fileFullNames)
        {
            fileFullNames = GetFullFilePaths(fileFullNames);
            CompressFilesEncrypted(archiveStream, commonRootLength, string.Empty, fileFullNames);
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="archiveName">The archive file name.</param>
        /// <param name="password">The archive password.</param>
        public void CompressFilesEncrypted(
            string archiveName, string password, params string[] fileFullNames)
        {
            fileFullNames = GetFullFilePaths(fileFullNames);
            CompressFilesEncrypted(archiveName, CommonRoot(fileFullNames), password, fileFullNames);
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="archiveStream">The archive output stream.
        /// Use CompressFiles( ... string archiveName ... ) overloads for archiving to disk.</param>
        /// <param name="password">The archive password.</param>
        public void CompressFilesEncrypted(
            Stream archiveStream, string password, params string[] fileFullNames)
        {
            fileFullNames = GetFullFilePaths(fileFullNames);
            CompressFilesEncrypted(archiveStream, CommonRoot(fileFullNames), password, fileFullNames);
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="commonRootLength">The length of the common root of the file names.</param>
        /// <param name="archiveName">The archive file name.</param>
        /// <param name="password">The archive password.</param>
        public void CompressFilesEncrypted(string archiveName, int commonRootLength, string password, params string[] fileFullNames)
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream())
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressFilesEncrypted(fs, commonRootLength, password, fileFullNames);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// Packs files into the archive.
        /// </summary>
        /// <param name="fileFullNames">Array of file names to pack.</param>
        /// <param name="commonRootLength">The length of the common root of the file names.</param>
        /// <param name="archiveStream">The archive output stream.
        /// Use CompressFiles( ... string archiveName ... ) overloads for archiving to disk.</param>
        /// <param name="password">The archive password.</param>
        public void CompressFilesEncrypted(
            Stream archiveStream, int commonRootLength, string password, params string[] fileFullNames)
        {
            ClearExceptions();

            if (fileFullNames.Length > 1 &&
                (_archiveFormat == OutArchiveFormat.BZip2 || _archiveFormat == OutArchiveFormat.GZip ||
                 _archiveFormat == OutArchiveFormat.XZ))
            {
                if (!ThrowException(null,
                    new CompressionFailedException("Can not compress more than one file in this format.")))
                {
                    return;
                }
            }

            if (_volumeSize == 0 || !_compressingFilesOnDisk)
            {
                ValidateStream(archiveStream);
            }

            FileInfo[] files = null;

            try
            {
                files = ProduceFileInfoArray(fileFullNames, commonRootLength, _directoryCompress, DirectoryStructure);
            }
            catch (Exception e)
            {
                if (!ThrowException(null, e))
                {
                    return;
                }
            }

            _directoryCompress = false;

            FilesFound?.Invoke(this, new IntEventArgs(fileFullNames.Length));

            try
            {
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(archiveStream)) as IDisposable)
                {
                    IInStream inArchiveStream;

                    using ((inArchiveStream = GetInStream()) as IDisposable)
                    {
                        IOutArchive outArchive;

                        if (CompressionMode == CompressionMode.Create)
                        {
                            SevenZipLibraryManager.LoadLibrary(this, _archiveFormat);
                            outArchive = SevenZipLibraryManager.OutArchive(_archiveFormat, this);
                        }
                        else
                        {
                            // Create IInArchive, read it and convert to IOutArchive
                            SevenZipLibraryManager.LoadLibrary(this, Formats.InForOutFormats[_archiveFormat]);

                            if ((outArchive = MakeOutArchive(inArchiveStream)) == null)
                            {
                                return;
                            }
                        }

                        using (var auc = GetArchiveUpdateCallback(files, commonRootLength, password))
                        {
                            try
                            {
                                if (files != null)
                                    CheckedExecute(
                                        outArchive.UpdateItems(
                                            sequentialArchiveStream, (uint) files.Length + _oldFilesCount, auc),
                                        SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                            }
                            finally
                            {
                                FreeCompressionCallback(auc);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (CompressionMode == CompressionMode.Create)
                {
                    SevenZipLibraryManager.FreeLibrary(this, _archiveFormat);
                }
                else
                {
                    SevenZipLibraryManager.FreeLibrary(this, Formats.InForOutFormats[_archiveFormat]);
                }

                _compressingFilesOnDisk = false;
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

        #region CompressDirectory overloads

        /// <summary>
        /// Packs all files in the specified directory.
        /// </summary>
        /// <param name="directory">The directory to compress.</param>
        /// <param name="archiveName">The archive file name.</param>
        /// <param name="password">The archive password.</param>
        /// <param name="searchPattern">Search string, such as "*.txt".</param>
        /// <param name="recursion">If true, files will be searched for recursively; otherwise, not.</param>
        public void CompressDirectory(string directory, string archiveName, string password = "", string searchPattern = "*", bool recursion = true)
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream())
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressDirectory(directory, fs, password, searchPattern, recursion);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// Packs all files in the specified directory.
        /// </summary>
        /// <param name="directory">The directory to compress.</param>
        /// <param name="archiveStream">The archive output stream.
        /// Use CompressDirectory( ... string archiveName ... ) overloads for archiving to disk.</param>        
        /// <param name="password">The archive password.</param>
        /// <param name="searchPattern">Search string, such as "*.txt".</param>
        /// <param name="recursion">If true, files will be searched for recursively; otherwise, not.</param>
        public void CompressDirectory(string directory, Stream archiveStream, string password = "", string searchPattern = "*", bool recursion = true)
        {
            var files = new List<string>();

            if (!Directory.Exists(directory))
            {
                throw new ArgumentException("Directory \"" + directory + "\" does not exist!");
            }

            // Get full path, in case this is eg. an SFN path.
            directory = Path.GetFullPath(directory);

            if (RecursiveDirectoryEmptyCheck(directory))
            {
                throw new SevenZipInvalidFileNamesException("the specified directory is empty!");
            }

            if (recursion)
            {
                AddFilesFromDirectory(directory, files, searchPattern);
            }
            else
            {
                files.AddRange((new DirectoryInfo(directory)).GetFiles(searchPattern).Select(fi => fi.FullName));
            }

            var commonRootLength = directory.Length;

            if (directory.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                directory = directory.Substring(0, directory.Length - 1);
            }
            else
            {
                commonRootLength++;
            }

            if (PreserveDirectoryRoot)
            {
                var upperRoot = Path.GetDirectoryName(directory);
                commonRootLength = upperRoot.Length + (upperRoot.EndsWith("\\", StringComparison.OrdinalIgnoreCase) ? 0 : 1);
            }

            _directoryCompress = true;
            CompressFilesEncrypted(archiveStream, commonRootLength, password, files.ToArray());
        }

#endregion

        #region CompressFileDictionary overloads

        /// <summary>
        /// Packs the specified file dictionary.
        /// </summary>
        /// <param name="fileDictionary">Dictionary&lt;name of the archive entry, file name&gt;.
        /// If a file name is null, the corresponding archive entry becomes a directory.</param>
        /// <param name="archiveName">The archive file name.</param>
        /// <param name="password">The archive password.</param>
        public void CompressFileDictionary(
            IDictionary<string, string> fileDictionary, string archiveName, string password = "")
        {
            _compressingFilesOnDisk = true;
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream())
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressFileDictionary(fileDictionary, fs, password);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// Packs the specified file dictionary.
        /// </summary>
        /// <param name="fileDictionary">Dictionary&lt;name of the archive entry, file name&gt;.
        /// If a file name is null, the corresponding archive entry becomes a directory.</param>
        /// <param name="archiveStream">The archive output stream.
        /// Use CompressStreamDictionary( ... string archiveName ... ) overloads for archiving to disk.</param>
        /// <param name="password">The archive password.</param>
        public void CompressFileDictionary(IDictionary<string, string> fileDictionary, Stream archiveStream, string password = "")
        {
            var streamDict = new Dictionary<string, Stream>(fileDictionary.Count);

            foreach (var pair in fileDictionary)
            {
                if (pair.Value == null)
                {
                    streamDict.Add(pair.Key, null);
                }
                else
                {
                    if (!File.Exists(pair.Value))
                    {
                        throw new CompressionFailedException(
                            "The file corresponding to the archive entry \"" + pair.Key + "\" does not exist.");
                    }

                    streamDict.Add(
                        pair.Key,
                        new FileStream(pair.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                }
            }

            //The created streams will be automatically disposed inside.
            CompressStreamDictionary(streamDict, archiveStream, password);
        }

#endregion

        #region CompressStreamDictionary overloads

        /// <summary>
        /// Packs the specified stream dictionary.
        /// </summary>
        /// <param name="streamDictionary">Dictionary&lt;name of the archive entry, stream&gt;.
        /// If a stream is null, the corresponding string becomes a directory name.</param>
        /// <param name="archiveName">The archive file name.</param>
        /// <param name="password">The archive password.</param>
        public void CompressStreamDictionary(IDictionary<string, Stream> streamDictionary, string archiveName, string password = "")
        {
            _archiveName = archiveName;

            using (var fs = GetArchiveFileStream())
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                CompressStreamDictionary(streamDictionary, fs, password);
            }

            FinalizeUpdate();
        }

        /// <summary>
        /// Packs the specified stream dictionary.
        /// </summary>
        /// <param name="streamDictionary">Dictionary&lt;name of the archive entry, stream&gt;.
        /// If a stream is null, the corresponding string becomes a directory name.</param>
        /// <param name="archiveStream">The archive output stream.
        /// Use CompressStreamDictionary( ... string archiveName ... ) overloads for archiving to disk.</param>
        /// <param name="password">The archive password.</param>
        public void CompressStreamDictionary(IDictionary<string, Stream> streamDictionary, Stream archiveStream, string password = "")
        {
            ClearExceptions();

            if (streamDictionary.Count > 1 &&
                (_archiveFormat == OutArchiveFormat.BZip2 || _archiveFormat == OutArchiveFormat.GZip ||
                 _archiveFormat == OutArchiveFormat.XZ))
            {
                if (!ThrowException(null,
                    new CompressionFailedException("Can not compress more than one file/stream in this format.")))
                {
                    return;
                }
            }

            if (_volumeSize == 0 || !_compressingFilesOnDisk)
            {
                ValidateStream(archiveStream);
            }

            if (streamDictionary.Where(
                pair => pair.Value != null && (!pair.Value.CanSeek || !pair.Value.CanRead)).Any(
                pair => !ThrowException(null,
                    new ArgumentException(
                        $"The specified stream dictionary contains an invalid stream corresponding to the archive entry \"{pair.Key}\".", 
                        nameof(streamDictionary)))))
            {
                return;
            }

            try
            {
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(archiveStream)) as IDisposable)
                {
                    IInStream inArchiveStream;

                    using ((inArchiveStream = GetInStream()) as IDisposable)
                    {
                        IOutArchive outArchive;

                        if (CompressionMode == CompressionMode.Create)
                        {
                            SevenZipLibraryManager.LoadLibrary(this, _archiveFormat);
                            outArchive = SevenZipLibraryManager.OutArchive(_archiveFormat, this);
                        }
                        else
                        {
                            // Create IInArchive, read it and convert to IOutArchive
                            SevenZipLibraryManager.LoadLibrary(
                                this, Formats.InForOutFormats[_archiveFormat]);
                            if ((outArchive = MakeOutArchive(inArchiveStream)) == null)
                            {
                                return;
                            }
                        }

                        using (var auc = GetArchiveUpdateCallback(streamDictionary, password))
                        {
                            try
                            {
                                CheckedExecute(outArchive.UpdateItems(sequentialArchiveStream,
                                        (uint) streamDictionary.Count + _oldFilesCount, auc),
                                    SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                            }
                            finally
                            {
                                FreeCompressionCallback(auc);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (CompressionMode == CompressionMode.Create)
                {
                    SevenZipLibraryManager.FreeLibrary(this, _archiveFormat);
                }
                else
                {
                    SevenZipLibraryManager.FreeLibrary(this, Formats.InForOutFormats[_archiveFormat]);
                }

                _compressingFilesOnDisk = false;
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

        #region CompressStream overloads

        /// <summary>
        /// Compresses the specified stream.
        /// </summary>
        /// <param name="inStream">The source uncompressed stream.</param>
        /// <param name="outStream">The destination compressed stream.</param>
        /// <param name="password">The archive password.</param>
        /// <exception cref="ArgumentException">ArgumentException: at least one of the specified streams is invalid.</exception>
        public void CompressStream(Stream inStream, Stream outStream, string password = "")
        {
            ClearExceptions();

            if (!inStream.CanSeek || !inStream.CanRead || !outStream.CanWrite)
            {
                if (!ThrowException(null, new ArgumentException("The specified streams are invalid.")))
                {
                    return;
                }
            }

            try
            {
                SevenZipLibraryManager.LoadLibrary(this, _archiveFormat);
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(outStream)) as IDisposable)
                {
                    using (var auc = GetArchiveUpdateCallback(inStream, password))
                    {
                        try
                        {
                            CheckedExecute(
                                SevenZipLibraryManager.OutArchive(_archiveFormat, this).UpdateItems(
                                    sequentialArchiveStream, 1, auc),
                                SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                        }
                        finally
                        {
                            FreeCompressionCallback(auc);
                        }
                    }
                }
            }
            finally
            {
                SevenZipLibraryManager.FreeLibrary(this, _archiveFormat);
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

        #endregion

        #region ModifyArchive overloads

        /// <summary>
        /// Modifies the existing archive (renames files or deletes them).
        /// </summary>
        /// <param name="archiveName">The archive file name.</param>
        /// <param name="newFileNames">New file names. Null value to delete the corresponding index.</param>
        /// <param name="password">The archive password.</param>
        public void ModifyArchive(string archiveName, IDictionary<int, string> newFileNames, string password = "")
        {
            ClearExceptions();

            if (!SevenZipLibraryManager.ModifyCapable)
            {
                throw new SevenZipLibraryException("The specified 7zip native library does not support this method.");
            }

            if (!File.Exists(archiveName))
            {
                if (!ThrowException(null,
                    new ArgumentException("The specified archive does not exist.", nameof(archiveName))))
                {
                    return;
                }
            }

            if (newFileNames == null || newFileNames.Count == 0)
            {
                if (!ThrowException(null, new ArgumentException("Invalid new file names.", nameof(newFileNames))))
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(password) && string.IsNullOrEmpty(Password))
            {
                // When modifying an encrypted archive, Password is not set in the SevenZipCompressor.
                Password = password;
            }

            try
            {
                using (var extractor = new SevenZipExtractor(archiveName, password))
                {
                    _updateData = new UpdateData();
                    var archiveData = new ArchiveFileInfo[extractor.ArchiveFileData.Count];
                    extractor.ArchiveFileData.CopyTo(archiveData, 0);
                    _updateData.ArchiveFileData = new List<ArchiveFileInfo>(archiveData);
                }

                _updateData.FileNamesToModify = newFileNames;
                _updateData.Mode = InternalCompressionMode.Modify;
            }
            catch (SevenZipException e)
            {
                if (!ThrowException(null, e))
                {
                    return;
                }
            }

            _archiveName = archiveName;
            _compressingFilesOnDisk = true;

            using (var fs = GetArchiveFileStream())
            {
                if (fs == null && _volumeSize == 0)
                {
                    return;
                }

                ModifyArchiveInner(fs, password);
            }

            FinalizeUpdate();
        }

        public void ModifyArchive(Stream archiveStream, IDictionary<int, string> newFileNames, string password = "")
        {
            ClearExceptions();

            if (!SevenZipLibraryManager.ModifyCapable)
            {
                throw new SevenZipLibraryException("The specified 7zip native library does not support this method.");
            }

            if (newFileNames == null || newFileNames.Count == 0)
            {
                if (!ThrowException(null, new ArgumentException("Invalid new file names.", nameof(newFileNames))))
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(password) && string.IsNullOrEmpty(Password))
            {
                // When modifying an encrypted archive, Password is not set in the SevenZipCompressor.
                Password = password;
            }

            try
            {
                using (var extractor = new SevenZipExtractor(InStream, password, true))
                {
                    _updateData = new UpdateData();
                    var archiveData = new ArchiveFileInfo[extractor.ArchiveFileData.Count];
                    extractor.ArchiveFileData.CopyTo(archiveData, 0);
                    _updateData.ArchiveFileData = new List<ArchiveFileInfo>(archiveData);
                }

                _updateData.FileNamesToModify = newFileNames;
                _updateData.Mode = InternalCompressionMode.Modify;
            }
            catch (SevenZipException e)
            {
                if (!ThrowException(null, e))
                {
                    return;
                }
            }

            ModifyArchiveInner(archiveStream, password);
        }

        private void ModifyArchiveInner(Stream archiveStream, string password)
        {
            try
            {
                ISequentialOutStream sequentialArchiveStream;

                using ((sequentialArchiveStream = GetOutStream(archiveStream)) as IDisposable)
                {
                    IInStream inArchiveStream;

                    using ((inArchiveStream = GetInStream()) as IDisposable)
                    {
                        IOutArchive outArchive;
                        // Create IInArchive, read it and convert to IOutArchive
                        SevenZipLibraryManager.LoadLibrary(
                            this, Formats.InForOutFormats[_archiveFormat]);
                        if ((outArchive = MakeOutArchive(inArchiveStream)) == null)
                        {
                            return;
                        }

                        using (var auc = GetArchiveUpdateCallback(null, 0, password))
                        {
                            uint deleteCount = 0;

                            if (_updateData.FileNamesToModify != null)
                            {
                                deleteCount = (uint)_updateData.FileNamesToModify.Sum(
                                    pairDeleted => pairDeleted.Value == null ? 1 : 0);
                            }

                            try
                            {
                                CheckedExecute(
                                    outArchive.UpdateItems(
                                        sequentialArchiveStream, _oldFilesCount - deleteCount, auc),
                                    SevenZipCompressionFailedException.DEFAULT_MESSAGE, auc);
                            }
                            finally
                            {
                                FreeCompressionCallback(auc);
                            }
                        }
                    }
                }
            }
            finally
            {
                SevenZipLibraryManager.FreeLibrary(this, Formats.InForOutFormats[_archiveFormat]);
                _compressingFilesOnDisk = false;
                _updateData.FileNamesToModify = null;
                _updateData.ArchiveFileData = null;
                OnEvent(CompressionFinished, EventArgs.Empty, false);
            }

            ThrowUserException();
        }

#endregion

#endregion

#endif

        /// <summary>
        /// Gets or sets the dictionary size for the managed LZMA algorithm.
        /// </summary>
        public static int LzmaDictionarySize
        {
            get => _lzmaDictionarySize;
            set => _lzmaDictionarySize = value;
        }

        internal static void WriteLzmaProperties(Encoder encoder)
        {
            #region LZMA properties definition

            CoderPropId[] propIDs =
            {
                CoderPropId.DictionarySize,
                CoderPropId.PosStateBits,
                CoderPropId.LitContextBits,
                CoderPropId.LitPosBits,
                CoderPropId.Algorithm,
                CoderPropId.NumFastBytes,
                CoderPropId.MatchFinder,
                CoderPropId.EndMarker
            };
            object[] properties =
            {
                _lzmaDictionarySize,
                2,
                3,
                0,
                2,
                256,
                "bt4",
                false
            };

#endregion

            encoder.SetCoderProperties(propIDs, properties);
        }

        /// <summary>
        /// Compresses the specified stream with LZMA algorithm (C# inside)
        /// </summary>
        /// <param name="inStream">The source uncompressed stream</param>
        /// <param name="outStream">The destination compressed stream</param>
        /// <param name="inLength">The length of uncompressed data (null for inStream.Length)</param>
        /// <param name="codeProgressEvent">The event for handling the code progress</param>
        public static void CompressStream(Stream inStream, Stream outStream, int? inLength,
            EventHandler<ProgressEventArgs> codeProgressEvent)
        {
            if (!inStream.CanRead || !outStream.CanWrite)
            {
                throw new ArgumentException("The specified streams are invalid.");
            }

            var encoder = new Encoder();
            WriteLzmaProperties(encoder);
            encoder.WriteCoderProperties(outStream);
            var streamSize = inLength ?? inStream.Length;

            for (var i = 0; i < 8; i++)
            {
                outStream.WriteByte((byte) (streamSize >> (8 * i)));
            }

            encoder.Code(inStream, outStream, -1, -1, new LzmaProgressCallback(streamSize, codeProgressEvent));
        }

        /// <summary>
        /// Compresses byte array with LZMA algorithm (C# inside)
        /// </summary>
        /// <param name="data">Byte array to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressBytes(byte[] data)
        {
            using (var inStream = new MemoryStream(data))
            {
                using (var outStream = new MemoryStream())
                {
                    var encoder = new Encoder();
                    WriteLzmaProperties(encoder);
                    encoder.WriteCoderProperties(outStream);
                    var streamSize = inStream.Length;

                    for (var i = 0; i < 8; i++)
                    {
                        outStream.WriteByte((byte) (streamSize >> (8 * i)));
                    }

                    encoder.Code(inStream, outStream, -1, -1, null);
                    return outStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Ensures an array of file names is the full path to that file.
        /// </summary>
        /// <param name="fileFullNames">Array of file names.</param>
        /// <returns>Array of file names with full paths.</returns>
        private static string[] GetFullFilePaths(IEnumerable<string> fileFullNames)
        {
            return fileFullNames.Select(Path.GetFullPath).ToArray();
        }
    }
}
