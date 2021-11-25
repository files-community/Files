using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Encryption;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	#region Keys Required Event Args

	/// <summary>
	/// Arguments used with KeysRequiredEvent
	/// </summary>
	public class KeysRequiredEventArgs : EventArgs
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="KeysRequiredEventArgs"></see>
		/// </summary>
		/// <param name="name">The name of the file for which keys are required.</param>
		public KeysRequiredEventArgs(string name)
		{
			fileName = name;
		}

		/// <summary>
		/// Initialise a new instance of <see cref="KeysRequiredEventArgs"></see>
		/// </summary>
		/// <param name="name">The name of the file for which keys are required.</param>
		/// <param name="keyValue">The current key value.</param>
		public KeysRequiredEventArgs(string name, byte[] keyValue)
		{
			fileName = name;
			key = keyValue;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Gets the name of the file for which keys are required.
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}

		/// <summary>
		/// Gets or sets the key value
		/// </summary>
		public byte[] Key
		{
			get { return key; }
			set { key = value; }
		}

		#endregion Properties

		#region Instance Fields

		private readonly string fileName;
		private byte[] key;

		#endregion Instance Fields
	}

	#endregion Keys Required Event Args

	#region Test Definitions

	/// <summary>
	/// The strategy to apply to testing.
	/// </summary>
	public enum TestStrategy
	{
		/// <summary>
		/// Find the first error only.
		/// </summary>
		FindFirstError,

		/// <summary>
		/// Find all possible errors.
		/// </summary>
		FindAllErrors,
	}

	/// <summary>
	/// The operation in progress reported by a <see cref="ZipTestResultHandler"/> during testing.
	/// </summary>
	/// <seealso cref="ZipFile.TestArchive(bool)">TestArchive</seealso>
	public enum TestOperation
	{
		/// <summary>
		/// Setting up testing.
		/// </summary>
		Initialising,

		/// <summary>
		/// Testing an individual entries header
		/// </summary>
		EntryHeader,

		/// <summary>
		/// Testing an individual entries data
		/// </summary>
		EntryData,

		/// <summary>
		/// Testing an individual entry has completed.
		/// </summary>
		EntryComplete,

		/// <summary>
		/// Running miscellaneous tests
		/// </summary>
		MiscellaneousTests,

		/// <summary>
		/// Testing is complete
		/// </summary>
		Complete,
	}

	/// <summary>
	/// Status returned by <see cref="ZipTestResultHandler"/> during testing.
	/// </summary>
	/// <seealso cref="ZipFile.TestArchive(bool)">TestArchive</seealso>
	public class TestStatus
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="TestStatus"/>
		/// </summary>
		/// <param name="file">The <see cref="ZipFile"/> this status applies to.</param>
		public TestStatus(ZipFile file)
		{
			file_ = file;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Get the current <see cref="TestOperation"/> in progress.
		/// </summary>
		public TestOperation Operation
		{
			get { return operation_; }
		}

		/// <summary>
		/// Get the <see cref="ZipFile"/> this status is applicable to.
		/// </summary>
		public ZipFile File
		{
			get { return file_; }
		}

		/// <summary>
		/// Get the current/last entry tested.
		/// </summary>
		public ZipEntry Entry
		{
			get { return entry_; }
		}

		/// <summary>
		/// Get the number of errors detected so far.
		/// </summary>
		public int ErrorCount
		{
			get { return errorCount_; }
		}

		/// <summary>
		/// Get the number of bytes tested so far for the current entry.
		/// </summary>
		public long BytesTested
		{
			get { return bytesTested_; }
		}

		/// <summary>
		/// Get a value indicating whether the last entry test was valid.
		/// </summary>
		public bool EntryValid
		{
			get { return entryValid_; }
		}

		#endregion Properties

		#region Internal API

		internal void AddError()
		{
			errorCount_++;
			entryValid_ = false;
		}

		internal void SetOperation(TestOperation operation)
		{
			operation_ = operation;
		}

		internal void SetEntry(ZipEntry entry)
		{
			entry_ = entry;
			entryValid_ = true;
			bytesTested_ = 0;
		}

		internal void SetBytesTested(long value)
		{
			bytesTested_ = value;
		}

		#endregion Internal API

		#region Instance Fields

		private readonly ZipFile file_;
		private ZipEntry entry_;
		private bool entryValid_;
		private int errorCount_;
		private long bytesTested_;
		private TestOperation operation_;

		#endregion Instance Fields
	}

	/// <summary>
	/// Delegate invoked during <see cref="ZipFile.TestArchive(bool, TestStrategy, ZipTestResultHandler)">testing</see> if supplied indicating current progress and status.
	/// </summary>
	/// <remarks>If the message is non-null an error has occured.  If the message is null
	/// the operation as found in <see cref="TestStatus">status</see> has started.</remarks>
	public delegate void ZipTestResultHandler(TestStatus status, string message);

	#endregion Test Definitions

	#region Update Definitions

	/// <summary>
	/// The possible ways of <see cref="ZipFile.CommitUpdate()">applying updates</see> to an archive.
	/// </summary>
	public enum FileUpdateMode
	{
		/// <summary>
		/// Perform all updates on temporary files ensuring that the original file is saved.
		/// </summary>
		Safe,

		/// <summary>
		/// Update the archive directly, which is faster but less safe.
		/// </summary>
		Direct,
	}

	#endregion Update Definitions

	#region ZipFile Class

	/// <summary>
	/// This class represents a Zip archive.  You can ask for the contained
	/// entries, or get an input stream for a file entry.  The entry is
	/// automatically decompressed.
	///
	/// You can also update the archive adding or deleting entries.
	///
	/// This class is thread safe for input:  You can open input streams for arbitrary
	/// entries in different threads.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	/// <example>
	/// <code>
	/// using System;
	/// using System.Text;
	/// using System.Collections;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.Zip;
	///
	/// class MainClass
	/// {
	/// 	static public void Main(string[] args)
	/// 	{
	/// 		using (ZipFile zFile = new ZipFile(args[0])) {
	/// 			Console.WriteLine("Listing of : " + zFile.Name);
	/// 			Console.WriteLine("");
	/// 			Console.WriteLine("Raw Size    Size      Date     Time     Name");
	/// 			Console.WriteLine("--------  --------  --------  ------  ---------");
	/// 			foreach (ZipEntry e in zFile) {
	/// 				if ( e.IsFile ) {
	/// 					DateTime d = e.DateTime;
	/// 					Console.WriteLine("{0, -10}{1, -10}{2}  {3}   {4}", e.Size, e.CompressedSize,
	/// 						d.ToString("dd-MM-yy"), d.ToString("HH:mm"),
	/// 						e.Name);
	/// 				}
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class ZipFile : IEnumerable, IDisposable
	{
		#region KeyHandling

		/// <summary>
		/// Delegate for handling keys/password setting during compression/decompression.
		/// </summary>
		public delegate void KeysRequiredEventHandler(
			object sender,
			KeysRequiredEventArgs e
		);

		/// <summary>
		/// Event handler for handling encryption keys.
		/// </summary>
		public KeysRequiredEventHandler KeysRequired;

		/// <summary>
		/// Handles getting of encryption keys when required.
		/// </summary>
		/// <param name="fileName">The file for which encryption keys are required.</param>
		private void OnKeysRequired(string fileName)
		{
			if (KeysRequired != null)
			{
				var krea = new KeysRequiredEventArgs(fileName, key);
				KeysRequired(this, krea);
				key = krea.Key;
			}
		}

		/// <summary>
		/// Get/set the encryption key value.
		/// </summary>
		private byte[] Key
		{
			get { return key; }
			set { key = value; }
		}

		/// <summary>
		/// Password to be used for encrypting/decrypting files.
		/// </summary>
		/// <remarks>Set to null if no password is required.</remarks>
		public string Password
		{
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					key = null;
				}
				else
				{
					key = PkzipClassic.GenerateKeys(ZipCryptoEncoding.GetBytes(value));
				}

				rawPassword_ = value;
			}
		}

		/// <summary>
		/// Get a value indicating whether encryption keys are currently available.
		/// </summary>
		private bool HaveKeys
		{
			get { return key != null; }
		}

		#endregion KeyHandling

		#region Constructors

		/// <summary>
		/// Opens a Zip file with the given name for reading.
		/// </summary>
		/// <param name="name">The name of the file to open.</param>
		/// <param name="stringCodec"></param>
		/// <exception cref="ArgumentNullException">The argument supplied is null.</exception>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(string name, StringCodec stringCodec = null)
		{
			name_ = name ?? throw new ArgumentNullException(nameof(name));

			baseStream_ = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read);
			isStreamOwner = true;

			if (stringCodec != null)
			{
				_stringCodec = stringCodec;
			}

			try
			{
				ReadEntries();
			}
			catch
			{
				DisposeInternal(true);
				throw;
			}
		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="FileStream"/>.
		/// </summary>
		/// <param name="file">The <see cref="FileStream"/> to read archive data from.</param>
		/// <exception cref="ArgumentNullException">The supplied argument is null.</exception>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(FileStream file) :
			this(file, false)
		{

		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="FileStream"/>.
		/// </summary>
		/// <param name="file">The <see cref="FileStream"/> to read archive data from.</param>
		/// <param name="leaveOpen">true to leave the <see cref="FileStream">file</see> open when the ZipFile is disposed, false to dispose of it</param>
		/// <exception cref="ArgumentNullException">The supplied argument is null.</exception>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// The file doesn't contain a valid zip archive.
		/// </exception>
		public ZipFile(FileStream file, bool leaveOpen)
		{
			if (file == null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			if (!file.CanSeek)
			{
				throw new ArgumentException("Stream is not seekable", nameof(file));
			}

			baseStream_ = file;
			name_ = file.Name;
			isStreamOwner = !leaveOpen;

			try
			{
				ReadEntries();
			}
			catch
			{
				DisposeInternal(true);
				throw;
			}
		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read archive data from.</param>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The stream doesn't contain a valid zip archive.<br/>
		/// </exception>
		/// <exception cref="ArgumentException">
		/// The <see cref="Stream">stream</see> doesnt support seeking.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// The <see cref="Stream">stream</see> argument is null.
		/// </exception>
		public ZipFile(Stream stream) :
			this(stream, false)
		{

		}

		/// <summary>
		/// Opens a Zip file reading the given <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to read archive data from.</param>
		/// <param name="leaveOpen">true to leave the <see cref="Stream">stream</see> open when the ZipFile is disposed, false to dispose of it</param>
		/// <exception cref="IOException">
		/// An i/o error occurs
		/// </exception>
		/// <exception cref="ZipException">
		/// The stream doesn't contain a valid zip archive.<br/>
		/// </exception>
		/// <exception cref="ArgumentException">
		/// The <see cref="Stream">stream</see> doesnt support seeking.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// The <see cref="Stream">stream</see> argument is null.
		/// </exception>
		public ZipFile(Stream stream, bool leaveOpen)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (!stream.CanSeek)
			{
				throw new ArgumentException("Stream is not seekable", nameof(stream));
			}

			baseStream_ = stream;
			isStreamOwner = !leaveOpen;

			if (baseStream_.Length > 0)
			{
				try
				{
					ReadEntries();
				}
				catch
				{
					DisposeInternal(true);
					throw;
				}
			}
			else
			{
				entries_ = Empty.Array<ZipEntry>();
				isNewArchive_ = true;
			}
		}

		/// <summary>
		/// Initialises a default <see cref="ZipFile"/> instance with no entries and no file storage.
		/// </summary>
		internal ZipFile()
		{
			entries_ = Empty.Array<ZipEntry>();
			isNewArchive_ = true;
		}

		#endregion Constructors

		#region Destructors and Closing

		/// <summary>
		/// Finalize this instance.
		/// </summary>
		~ZipFile()
		{
			Dispose(false);
		}

		/// <summary>
		/// Closes the ZipFile.  If the stream is <see cref="IsStreamOwner">owned</see> then this also closes the underlying input stream.
		/// Once closed, no further instance methods should be called.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An i/o error occurs.
		/// </exception>
		public void Close()
		{
			DisposeInternal(true);
			GC.SuppressFinalize(this);
		}

		#endregion Destructors and Closing

		#region Creators

		/// <summary>
		/// Create a new <see cref="ZipFile"/> whose data will be stored in a file.
		/// </summary>
		/// <param name="fileName">The name of the archive to create.</param>
		/// <returns>Returns the newly created <see cref="ZipFile"/></returns>
		/// <exception cref="ArgumentNullException"><paramref name="fileName"></paramref> is null</exception>
		public static ZipFile Create(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			FileStream fs = File.Create(fileName);

			return new ZipFile
			{
				name_ = fileName,
				baseStream_ = fs,
				isStreamOwner = true
			};
		}

		/// <summary>
		/// Create a new <see cref="ZipFile"/> whose data will be stored on a stream.
		/// </summary>
		/// <param name="outStream">The stream providing data storage.</param>
		/// <returns>Returns the newly created <see cref="ZipFile"/></returns>
		/// <exception cref="ArgumentNullException"><paramref name="outStream"> is null</paramref></exception>
		/// <exception cref="ArgumentException"><paramref name="outStream"> doesnt support writing.</paramref></exception>
		public static ZipFile Create(Stream outStream)
		{
			if (outStream == null)
			{
				throw new ArgumentNullException(nameof(outStream));
			}

			if (!outStream.CanWrite)
			{
				throw new ArgumentException("Stream is not writeable", nameof(outStream));
			}

			if (!outStream.CanSeek)
			{
				throw new ArgumentException("Stream is not seekable", nameof(outStream));
			}

			var result = new ZipFile
			{
				baseStream_ = outStream
			};
			return result;
		}

		#endregion Creators

		#region Properties

		/// <summary>
		/// Get/set a flag indicating if the underlying stream is owned by the ZipFile instance.
		/// If the flag is true then the stream will be closed when <see cref="Close">Close</see> is called.
		/// </summary>
		/// <remarks>
		/// The default value is true in all cases.
		/// </remarks>
		public bool IsStreamOwner
		{
			get { return isStreamOwner; }
			set { isStreamOwner = value; }
		}

		/// <summary>
		/// Get a value indicating whether
		/// this archive is embedded in another file or not.
		/// </summary>
		public bool IsEmbeddedArchive
		{
			// Not strictly correct in all circumstances currently
			get { return offsetOfFirstEntry > 0; }
		}

		/// <summary>
		/// Get a value indicating that this archive is a new one.
		/// </summary>
		public bool IsNewArchive
		{
			get { return isNewArchive_; }
		}

		/// <summary>
		/// Gets the comment for the zip file.
		/// </summary>
		public string ZipFileComment
		{
			get { return comment_; }
		}

		/// <summary>
		/// Gets the name of this zip file.
		/// </summary>
		public string Name
		{
			get { return name_; }
		}

		/// <summary>
		/// Gets the number of entries in this zip file.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The Zip file has been closed.
		/// </exception>
		[Obsolete("Use the Count property instead")]
		public int Size
		{
			get
			{
				return entries_.Length;
			}
		}

		/// <summary>
		/// Get the number of entries contained in this <see cref="ZipFile"/>.
		/// </summary>
		public long Count
		{
			get
			{
				return entries_.Length;
			}
		}

		/// <summary>
		/// Indexer property for ZipEntries
		/// </summary>
		[System.Runtime.CompilerServices.IndexerNameAttribute("EntryByIndex")]
		public ZipEntry this[int index]
		{
			get
			{
				return (ZipEntry)entries_[index].Clone();
			}
		}


		/// <inheritdoc cref="StringCodec.ZipCryptoEncoding"/>
		public Encoding ZipCryptoEncoding
		{
			get => _stringCodec.ZipCryptoEncoding;
			set => _stringCodec.ZipCryptoEncoding = value;
		}

		/// <inheritdoc cref="StringCodec"/>
		public StringCodec StringCodec
		{
			get => _stringCodec;
			set => _stringCodec = value;
		}

		#endregion Properties

		#region Input Handling

		/// <summary>
		/// Gets an enumerator for the Zip entries in this Zip file.
		/// </summary>
		/// <returns>Returns an <see cref="IEnumerator"/> for this archive.</returns>
		/// <exception cref="ObjectDisposedException">
		/// The Zip file has been closed.
		/// </exception>
		public IEnumerator GetEnumerator()
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			return new ZipEntryEnumerator(entries_);
		}

		/// <summary>
		/// Return the index of the entry with a matching name
		/// </summary>
		/// <param name="name">Entry name to find</param>
		/// <param name="ignoreCase">If true the comparison is case insensitive</param>
		/// <returns>The index position of the matching entry or -1 if not found</returns>
		/// <exception cref="ObjectDisposedException">
		/// The Zip file has been closed.
		/// </exception>
		public int FindEntry(string name, bool ignoreCase)
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			// TODO: This will be slow as the next ice age for huge archives!
			for (int i = 0; i < entries_.Length; i++)
			{
				if (string.Compare(name, entries_[i].Name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) == 0)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Searches for a zip entry in this archive with the given name.
		/// String comparisons are case insensitive
		/// </summary>
		/// <param name="name">
		/// The name to find. May contain directory components separated by slashes ('/').
		/// </param>
		/// <returns>
		/// A clone of the zip entry, or null if no entry with that name exists.
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		/// The Zip file has been closed.
		/// </exception>
		public ZipEntry GetEntry(string name)
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			int index = FindEntry(name, true);
			return (index >= 0) ? (ZipEntry)entries_[index].Clone() : null;
		}

		/// <summary>
		/// Gets an input stream for reading the given zip entry data in an uncompressed form.
		/// Normally the <see cref="ZipEntry"/> should be an entry returned by GetEntry().
		/// </summary>
		/// <param name="entry">The <see cref="ZipEntry"/> to obtain a data <see cref="Stream"/> for</param>
		/// <returns>An input <see cref="Stream"/> containing data for this <see cref="ZipEntry"/></returns>
		/// <exception cref="ObjectDisposedException">
		/// The ZipFile has already been closed
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The compression method for the entry is unknown
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The entry is not found in the ZipFile
		/// </exception>
		public Stream GetInputStream(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			long index = entry.ZipFileIndex;
			if ((index < 0) || (index >= entries_.Length) || (entries_[index].Name != entry.Name))
			{
				index = FindEntry(entry.Name, true);
				if (index < 0)
				{
					throw new ZipException("Entry cannot be found");
				}
			}
			return GetInputStream(index);
		}

		/// <summary>
		/// Creates an input stream reading a zip entry
		/// </summary>
		/// <param name="entryIndex">The index of the entry to obtain an input stream for.</param>
		/// <returns>
		/// An input <see cref="Stream"/> containing data for this <paramref name="entryIndex"/>
		/// </returns>
		/// <exception cref="ObjectDisposedException">
		/// The ZipFile has already been closed
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The compression method for the entry is unknown
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// The entry is not found in the ZipFile
		/// </exception>
		public Stream GetInputStream(long entryIndex)
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			long start = LocateEntry(entries_[entryIndex]);
			CompressionMethod method = entries_[entryIndex].CompressionMethod;
			Stream result = new PartialInputStream(this, start, entries_[entryIndex].CompressedSize);

			if (entries_[entryIndex].IsCrypted == true)
			{
				result = CreateAndInitDecryptionStream(result, entries_[entryIndex]);
				if (result == null)
				{
					throw new ZipException("Unable to decrypt this entry");
				}
			}

			switch (method)
			{
				case CompressionMethod.Stored:
					// read as is.
					break;

				case CompressionMethod.Deflated:
					// No need to worry about ownership and closing as underlying stream close does nothing.
					result = new InflaterInputStream(result, new Inflater(true));
					break;

				case CompressionMethod.BZip2:
					result = new BZip2.BZip2InputStream(result);
					break;

				default:
					throw new ZipException("Unsupported compression method " + method);
			}

			return result;
		}

		#endregion Input Handling

		#region Archive Testing

		/// <summary>
		/// Test an archive for integrity/validity
		/// </summary>
		/// <param name="testData">Perform low level data Crc check</param>
		/// <returns>true if all tests pass, false otherwise</returns>
		/// <remarks>Testing will terminate on the first error found.</remarks>
		public bool TestArchive(bool testData)
		{
			return TestArchive(testData, TestStrategy.FindFirstError, null);
		}

		/// <summary>
		/// Test an archive for integrity/validity
		/// </summary>
		/// <param name="testData">Perform low level data Crc check</param>
		/// <param name="strategy">The <see cref="TestStrategy"></see> to apply.</param>
		/// <param name="resultHandler">The <see cref="ZipTestResultHandler"></see> handler to call during testing.</param>
		/// <returns>true if all tests pass, false otherwise</returns>
		/// <exception cref="ObjectDisposedException">The object has already been closed.</exception>
		public bool TestArchive(bool testData, TestStrategy strategy, ZipTestResultHandler resultHandler)
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			var status = new TestStatus(this);

			resultHandler?.Invoke(status, null);

			HeaderTest test = testData ? (HeaderTest.Header | HeaderTest.Extract) : HeaderTest.Header;

			bool testing = true;

			try
			{
				int entryIndex = 0;

				while (testing && (entryIndex < Count))
				{
					if (resultHandler != null)
					{
						status.SetEntry(this[entryIndex]);
						status.SetOperation(TestOperation.EntryHeader);
						resultHandler(status, null);
					}

					try
					{
						TestLocalHeader(this[entryIndex], test);
					}
					catch (ZipException ex)
					{
						status.AddError();

						resultHandler?.Invoke(status, $"Exception during test - '{ex.Message}'");

						testing &= strategy != TestStrategy.FindFirstError;
					}

					if (testing && testData && this[entryIndex].IsFile)
					{
						// Don't check CRC for AES encrypted archives
						var checkCRC = this[entryIndex].AESKeySize == 0;

						if (resultHandler != null)
						{
							status.SetOperation(TestOperation.EntryData);
							resultHandler(status, null);
						}

						var crc = new Crc32();

						using (Stream entryStream = this.GetInputStream(this[entryIndex]))
						{
							byte[] buffer = new byte[4096];
							long totalBytes = 0;
							int bytesRead;
							while ((bytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								if (checkCRC)
								{
									crc.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
								}

								if (resultHandler != null)
								{
									totalBytes += bytesRead;
									status.SetBytesTested(totalBytes);
									resultHandler(status, null);
								}
							}
						}

						if (checkCRC && this[entryIndex].Crc != crc.Value)
						{
							status.AddError();

							resultHandler?.Invoke(status, "CRC mismatch");

							testing &= strategy != TestStrategy.FindFirstError;
						}

						if ((this[entryIndex].Flags & (int)GeneralBitFlags.Descriptor) != 0)
						{
							var data = new DescriptorData();
							ZipFormat.ReadDataDescriptor(baseStream_, this[entryIndex].LocalHeaderRequiresZip64, data);
							if (checkCRC && this[entryIndex].Crc != data.Crc)
							{
								status.AddError();
								resultHandler?.Invoke(status, "Descriptor CRC mismatch");
							}

							if (this[entryIndex].CompressedSize != data.CompressedSize)
							{
								status.AddError();
								resultHandler?.Invoke(status, "Descriptor compressed size mismatch");
							}

							if (this[entryIndex].Size != data.Size)
							{
								status.AddError();
								resultHandler?.Invoke(status, "Descriptor size mismatch");
							}
						}
					}

					if (resultHandler != null)
					{
						status.SetOperation(TestOperation.EntryComplete);
						resultHandler(status, null);
					}

					entryIndex += 1;
				}

				if (resultHandler != null)
				{
					status.SetOperation(TestOperation.MiscellaneousTests);
					resultHandler(status, null);
				}

				// TODO: the 'Corrina Johns' test where local headers are missing from
				// the central directory.  They are therefore invisible to many archivers.
			}
			catch (Exception ex)
			{
				status.AddError();

				resultHandler?.Invoke(status, $"Exception during test - '{ex.Message}'");
			}

			if (resultHandler != null)
			{
				status.SetOperation(TestOperation.Complete);
				status.SetEntry(null);
				resultHandler(status, null);
			}

			return (status.ErrorCount == 0);
		}

		[Flags]
		private enum HeaderTest
		{
			Extract = 0x01,     // Check that this header represents an entry whose data can be extracted
			Header = 0x02,     // Check that this header contents are valid
		}

		/// <summary>
		/// Test a local header against that provided from the central directory
		/// </summary>
		/// <param name="entry">
		/// The entry to test against
		/// </param>
		/// <param name="tests">The type of <see cref="HeaderTest">tests</see> to carry out.</param>
		/// <returns>The offset of the entries data in the file</returns>
		private long TestLocalHeader(ZipEntry entry, HeaderTest tests)
		{
			lock (baseStream_)
			{
				bool testHeader = (tests & HeaderTest.Header) != 0;
				bool testData = (tests & HeaderTest.Extract) != 0;

				var entryAbsOffset = offsetOfFirstEntry + entry.Offset;

				baseStream_.Seek(entryAbsOffset, SeekOrigin.Begin);
				var signature = (int)ReadLEUint();

				if (signature != ZipConstants.LocalHeaderSignature)
				{
					throw new ZipException(string.Format("Wrong local header signature at 0x{0:x}, expected 0x{1:x8}, actual 0x{2:x8}",
						entryAbsOffset, ZipConstants.LocalHeaderSignature, signature));
				}

				var extractVersion = (short)(ReadLEUshort() & 0x00ff);
				var localFlags = (short)ReadLEUshort();
				var compressionMethod = (short)ReadLEUshort();
				var fileTime = (short)ReadLEUshort();
				var fileDate = (short)ReadLEUshort();
				uint crcValue = ReadLEUint();
				long compressedSize = ReadLEUint();
				long size = ReadLEUint();
				int storedNameLength = ReadLEUshort();
				int extraDataLength = ReadLEUshort();

				byte[] nameData = new byte[storedNameLength];
				StreamUtils.ReadFully(baseStream_, nameData);

				byte[] extraData = new byte[extraDataLength];
				StreamUtils.ReadFully(baseStream_, extraData);

				var localExtraData = new ZipExtraData(extraData);

				// Extra data / zip64 checks
				if (localExtraData.Find(1))
				{
					// 2010-03-04 Forum 10512: removed checks for version >= ZipConstants.VersionZip64
					// and size or compressedSize = MaxValue, due to rogue creators.

					size = localExtraData.ReadLong();
					compressedSize = localExtraData.ReadLong();

					if ((localFlags & (int)GeneralBitFlags.Descriptor) != 0)
					{
						// These may be valid if patched later
						if ((size != -1) && (size != entry.Size))
						{
							throw new ZipException("Size invalid for descriptor");
						}

						if ((compressedSize != -1) && (compressedSize != entry.CompressedSize))
						{
							throw new ZipException("Compressed size invalid for descriptor");
						}
					}
				}
				else
				{
					// No zip64 extra data but entry requires it.
					if ((extractVersion >= ZipConstants.VersionZip64) &&
						(((uint)size == uint.MaxValue) || ((uint)compressedSize == uint.MaxValue)))
					{
						throw new ZipException("Required Zip64 extended information missing");
					}
				}

				if (testData)
				{
					if (entry.IsFile)
					{
						if (!entry.IsCompressionMethodSupported())
						{
							throw new ZipException("Compression method not supported");
						}

						if ((extractVersion > ZipConstants.VersionMadeBy)
							|| ((extractVersion > 20) && (extractVersion < ZipConstants.VersionZip64)))
						{
							throw new ZipException(string.Format("Version required to extract this entry not supported ({0})", extractVersion));
						}

						if ((localFlags & (int)(GeneralBitFlags.Patched | GeneralBitFlags.StrongEncryption | GeneralBitFlags.EnhancedCompress | GeneralBitFlags.HeaderMasked)) != 0)
						{
							throw new ZipException("The library does not support the zip version required to extract this entry");
						}
					}
				}

				if (testHeader)
				{
					if ((extractVersion <= 63) &&   // Ignore later versions as we dont know about them..
						(extractVersion != 10) &&
						(extractVersion != 11) &&
						(extractVersion != 20) &&
						(extractVersion != 21) &&
						(extractVersion != 25) &&
						(extractVersion != 27) &&
						(extractVersion != 45) &&
						(extractVersion != 46) &&
						(extractVersion != 50) &&
						(extractVersion != 51) &&
						(extractVersion != 52) &&
						(extractVersion != 61) &&
						(extractVersion != 62) &&
						(extractVersion != 63)
						)
					{
						throw new ZipException(string.Format("Version required to extract this entry is invalid ({0})", extractVersion));
					}

					var localEncoding = _stringCodec.ZipInputEncoding(localFlags);

					// Local entry flags dont have reserved bit set on.
					if ((localFlags & (int)(GeneralBitFlags.ReservedPKware4 | GeneralBitFlags.ReservedPkware14 | GeneralBitFlags.ReservedPkware15)) != 0)
					{
						throw new ZipException("Reserved bit flags cannot be set.");
					}

					// Encryption requires extract version >= 20
					if (((localFlags & (int)GeneralBitFlags.Encrypted) != 0) && (extractVersion < 20))
					{
						throw new ZipException(string.Format("Version required to extract this entry is too low for encryption ({0})", extractVersion));
					}

					// Strong encryption requires encryption flag to be set and extract version >= 50.
					if ((localFlags & (int)GeneralBitFlags.StrongEncryption) != 0)
					{
						if ((localFlags & (int)GeneralBitFlags.Encrypted) == 0)
						{
							throw new ZipException("Strong encryption flag set but encryption flag is not set");
						}

						if (extractVersion < 50)
						{
							throw new ZipException(string.Format("Version required to extract this entry is too low for encryption ({0})", extractVersion));
						}
					}

					// Patched entries require extract version >= 27
					if (((localFlags & (int)GeneralBitFlags.Patched) != 0) && (extractVersion < 27))
					{
						throw new ZipException(string.Format("Patched data requires higher version than ({0})", extractVersion));
					}

					// Central header flags match local entry flags.
					if (localFlags != entry.Flags)
					{
						throw new ZipException("Central header/local header flags mismatch");
					}

					// Central header compression method matches local entry
					if (entry.CompressionMethodForHeader != (CompressionMethod)compressionMethod)
					{
						throw new ZipException("Central header/local header compression method mismatch");
					}

					if (entry.Version != extractVersion)
					{
						throw new ZipException("Extract version mismatch");
					}

					// Strong encryption and extract version match
					if ((localFlags & (int)GeneralBitFlags.StrongEncryption) != 0)
					{
						if (extractVersion < 62)
						{
							throw new ZipException("Strong encryption flag set but version not high enough");
						}
					}

					if ((localFlags & (int)GeneralBitFlags.HeaderMasked) != 0)
					{
						if ((fileTime != 0) || (fileDate != 0))
						{
							throw new ZipException("Header masked set but date/time values non-zero");
						}
					}

					if ((localFlags & (int)GeneralBitFlags.Descriptor) == 0)
					{
						if (crcValue != (uint)entry.Crc)
						{
							throw new ZipException("Central header/local header crc mismatch");
						}
					}

					// Crc valid for empty entry.
					// This will also apply to streamed entries where size isnt known and the header cant be patched
					if ((size == 0) && (compressedSize == 0))
					{
						if (crcValue != 0)
						{
							throw new ZipException("Invalid CRC for empty entry");
						}
					}

					// TODO: make test more correct...  can't compare lengths as was done originally as this can fail for MBCS strings
					// Assuming a code page at this point is not valid?  Best is to store the name length in the ZipEntry probably
					if (entry.Name.Length > storedNameLength)
					{
						throw new ZipException("File name length mismatch");
					}

					// Name data has already been read convert it and compare.
					string localName = localEncoding.GetString(nameData);

					// Central directory and local entry name match
					if (localName != entry.Name)
					{
						throw new ZipException("Central header and local header file name mismatch");
					}

					// Directories have zero actual size but can have compressed size
					if (entry.IsDirectory)
					{
						if (size > 0)
						{
							throw new ZipException("Directory cannot have size");
						}

						// There may be other cases where the compressed size can be greater than this?
						// If so until details are known we will be strict.
						if (entry.IsCrypted)
						{
							if (compressedSize > entry.EncryptionOverheadSize + 2)
							{
								throw new ZipException("Directory compressed size invalid");
							}
						}
						else if (compressedSize > 2)
						{
							// When not compressed the directory size can validly be 2 bytes
							// if the true size wasn't known when data was originally being written.
							// NOTE: Versions of the library 0.85.4 and earlier always added 2 bytes
							throw new ZipException("Directory compressed size invalid");
						}
					}

					if (!ZipNameTransform.IsValidName(localName, true))
					{
						throw new ZipException("Name is invalid");
					}
				}

				// Tests that apply to both data and header.

				// Size can be verified only if it is known in the local header.
				// it will always be known in the central header.
				if (((localFlags & (int)GeneralBitFlags.Descriptor) == 0) ||
					((size > 0 || compressedSize > 0) && entry.Size > 0))
				{
					if ((size != 0)
						&& (size != entry.Size))
					{
						throw new ZipException(
							string.Format("Size mismatch between central header({0}) and local header({1})",
								entry.Size, size));
					}

					if ((compressedSize != 0)
						&& (compressedSize != entry.CompressedSize && compressedSize != 0xFFFFFFFF && compressedSize != -1))
					{
						throw new ZipException(
							string.Format("Compressed size mismatch between central header({0}) and local header({1})",
							entry.CompressedSize, compressedSize));
					}
				}

				int extraLength = storedNameLength + extraDataLength;
				return offsetOfFirstEntry + entry.Offset + ZipConstants.LocalHeaderBaseSize + extraLength;
			}
		}

		#endregion Archive Testing

		#region Updating

		private const int DefaultBufferSize = 4096;

		/// <summary>
		/// The kind of update to apply.
		/// </summary>
		private enum UpdateCommand
		{
			Copy,       // Copy original file contents.
			Modify,     // Change encryption, compression, attributes, name, time etc, of an existing file.
			Add,        // Add a new file to the archive.
		}

		#region Properties

		/// <summary>
		/// Get / set the <see cref="INameTransform"/> to apply to names when updating.
		/// </summary>
		public INameTransform NameTransform
		{
			get
			{
				return updateEntryFactory_.NameTransform;
			}

			set
			{
				updateEntryFactory_.NameTransform = value;
			}
		}

		/// <summary>
		/// Get/set the <see cref="IEntryFactory"/> used to generate <see cref="ZipEntry"/> values
		/// during updates.
		/// </summary>
		public IEntryFactory EntryFactory
		{
			get
			{
				return updateEntryFactory_;
			}

			set
			{
				if (value == null)
				{
					updateEntryFactory_ = new ZipEntryFactory();
				}
				else
				{
					updateEntryFactory_ = value;
				}
			}
		}

		/// <summary>
		/// Get /set the buffer size to be used when updating this zip file.
		/// </summary>
		public int BufferSize
		{
			get { return bufferSize_; }
			set
			{
				if (value < 1024)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "cannot be below 1024");
				}

				if (bufferSize_ != value)
				{
					bufferSize_ = value;
					copyBuffer_ = null;
				}
			}
		}

		/// <summary>
		/// Get a value indicating an update has <see cref="BeginUpdate()">been started</see>.
		/// </summary>
		public bool IsUpdating
		{
			get { return updates_ != null; }
		}

		/// <summary>
		/// Get / set a value indicating how Zip64 Extension usage is determined when adding entries.
		/// </summary>
		public UseZip64 UseZip64
		{
			get { return useZip64_; }
			set { useZip64_ = value; }
		}

		#endregion Properties

		#region Immediate updating

		//		TBD: Direct form of updating
		//
		//		public void Update(IEntryMatcher deleteMatcher)
		//		{
		//		}
		//
		//		public void Update(IScanner addScanner)
		//		{
		//		}

		#endregion Immediate updating

		#region Deferred Updating

		/// <summary>
		/// Begin updating this <see cref="ZipFile"/> archive.
		/// </summary>
		/// <param name="archiveStorage">The <see cref="IArchiveStorage">archive storage</see> for use during the update.</param>
		/// <param name="dataSource">The <see cref="IDynamicDataSource">data source</see> to utilise during updating.</param>
		/// <exception cref="ObjectDisposedException">ZipFile has been closed.</exception>
		/// <exception cref="ArgumentNullException">One of the arguments provided is null</exception>
		/// <exception cref="ObjectDisposedException">ZipFile has been closed.</exception>
		public void BeginUpdate(IArchiveStorage archiveStorage, IDynamicDataSource dataSource)
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			if (IsEmbeddedArchive)
			{
				throw new ZipException("Cannot update embedded/SFX archives");
			}

			archiveStorage_ = archiveStorage ?? throw new ArgumentNullException(nameof(archiveStorage));
			updateDataSource_ = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

			// NOTE: the baseStream_ may not currently support writing or seeking.

			updateIndex_ = new Dictionary<string, int>();

			updates_ = new List<ZipUpdate>(entries_.Length);
			foreach (ZipEntry entry in entries_)
			{
				int index = updates_.Count;
				updates_.Add(new ZipUpdate(entry));
				updateIndex_.Add(entry.Name, index);
			}

			// We must sort by offset before using offset's calculated sizes
			updates_.Sort(new UpdateComparer());

			int idx = 0;
			foreach (ZipUpdate update in updates_)
			{
				//If last entry, there is no next entry offset to use
				if (idx == updates_.Count - 1)
					break;

				update.OffsetBasedSize = ((ZipUpdate)updates_[idx + 1]).Entry.Offset - update.Entry.Offset;
				idx++;
			}
			updateCount_ = updates_.Count;

			contentsEdited_ = false;
			commentEdited_ = false;
			newComment_ = null;
		}

		/// <summary>
		/// Begin updating to this <see cref="ZipFile"/> archive.
		/// </summary>
		/// <param name="archiveStorage">The storage to use during the update.</param>
		public void BeginUpdate(IArchiveStorage archiveStorage)
		{
			BeginUpdate(archiveStorage, new DynamicDiskDataSource());
		}

		/// <summary>
		/// Begin updating this <see cref="ZipFile"/> archive.
		/// </summary>
		/// <seealso cref="BeginUpdate(IArchiveStorage)"/>
		/// <seealso cref="CommitUpdate"></seealso>
		/// <seealso cref="AbortUpdate"></seealso>
		public void BeginUpdate()
		{
			if (Name == null)
			{
				BeginUpdate(new MemoryArchiveStorage(), new DynamicDiskDataSource());
			}
			else
			{
				BeginUpdate(new DiskArchiveStorage(this), new DynamicDiskDataSource());
			}
		}

		/// <summary>
		/// Commit current updates, updating this archive.
		/// </summary>
		/// <seealso cref="BeginUpdate()"></seealso>
		/// <seealso cref="AbortUpdate"></seealso>
		/// <exception cref="ObjectDisposedException">ZipFile has been closed.</exception>
		public void CommitUpdate()
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			CheckUpdating();

			try
			{
				updateIndex_.Clear();
				updateIndex_ = null;

				if (contentsEdited_)
				{
					RunUpdates();
				}
				else if (commentEdited_)
				{
					UpdateCommentOnly();
				}
				else
				{
					// Create an empty archive if none existed originally.
					if (entries_.Length != 0) return;
					byte[] theComment = (newComment_ != null)
						? newComment_.RawComment
						: _stringCodec.ZipArchiveCommentEncoding.GetBytes(comment_);
					ZipFormat.WriteEndOfCentralDirectory(baseStream_, 0, 0, 0, theComment);
				}
			}
			finally
			{
				PostUpdateCleanup();
			}
		}

		/// <summary>
		/// Abort updating leaving the archive unchanged.
		/// </summary>
		/// <seealso cref="BeginUpdate()"></seealso>
		/// <seealso cref="CommitUpdate"></seealso>
		public void AbortUpdate()
		{
			PostUpdateCleanup();
		}

		/// <summary>
		/// Set the file comment to be recorded when the current update is <see cref="CommitUpdate">commited</see>.
		/// </summary>
		/// <param name="comment">The comment to record.</param>
		/// <exception cref="ObjectDisposedException">ZipFile has been closed.</exception>
		public void SetComment(string comment)
		{
			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			CheckUpdating();

			newComment_ = new ZipString(comment, _stringCodec.ZipArchiveCommentEncoding);

			if (newComment_.RawLength > 0xffff)
			{
				newComment_ = null;
				throw new ZipException("Comment length exceeds maximum - 65535");
			}

			// We dont take account of the original and current comment appearing to be the same
			// as encoding may be different.
			commentEdited_ = true;
		}

		#endregion Deferred Updating

		#region Adding Entries

		private void AddUpdate(ZipUpdate update)
		{
			contentsEdited_ = true;

			int index = FindExistingUpdate(update.Entry.Name, isEntryName: true);

			if (index >= 0)
			{
				if (updates_[index] == null)
				{
					updateCount_ += 1;
				}

				// Direct replacement is faster than delete and add.
				updates_[index] = update;
			}
			else
			{
				index = updates_.Count;
				updates_.Add(update);
				updateCount_ += 1;
				updateIndex_.Add(update.Entry.Name, index);
			}
		}

		/// <summary>
		/// Add a new entry to the archive.
		/// </summary>
		/// <param name="fileName">The name of the file to add.</param>
		/// <param name="compressionMethod">The compression method to use.</param>
		/// <param name="useUnicodeText">Ensure Unicode text is used for name and comment for this entry.</param>
		/// <exception cref="ArgumentNullException">Argument supplied is null.</exception>
		/// <exception cref="ObjectDisposedException">ZipFile has been closed.</exception>
		/// <exception cref="NotImplementedException">Compression method is not supported for creating entries.</exception>
		public void Add(string fileName, CompressionMethod compressionMethod, bool useUnicodeText)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			if (isDisposed_)
			{
				throw new ObjectDisposedException("ZipFile");
			}

			CheckSupportedCompressionMethod(compressionMethod);
			CheckUpdating();
			contentsEdited_ = true;

			ZipEntry entry = EntryFactory.MakeFileEntry(fileName);
			entry.IsUnicodeText = useUnicodeText;
			entry.CompressionMethod = compressionMethod;

			AddUpdate(new ZipUpdate(fileName, entry));
		}

		/// <summary>
		/// Add a new entry to the archive.
		/// </summary>
		/// <param name="fileName">The name of the file to add.</param>
		/// <param name="compressionMethod">The compression method to use.</param>
		/// <exception cref="ArgumentNullException">ZipFile has been closed.</exception>
		/// <exception cref="NotImplementedException">Compression method is not supported for creating entries.</exception>
		public void Add(string fileName, CompressionMethod compressionMethod)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			CheckSupportedCompressionMethod(compressionMethod);
			CheckUpdating();
			contentsEdited_ = true;

			ZipEntry entry = EntryFactory.MakeFileEntry(fileName);
			entry.CompressionMethod = compressionMethod;
			AddUpdate(new ZipUpdate(fileName, entry));
		}

		/// <summary>
		/// Add a file to the archive.
		/// </summary>
		/// <param name="fileName">The name of the file to add.</param>
		/// <exception cref="ArgumentNullException">Argument supplied is null.</exception>
		public void Add(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			CheckUpdating();
			AddUpdate(new ZipUpdate(fileName, EntryFactory.MakeFileEntry(fileName)));
		}

		/// <summary>
		/// Add a file to the archive.
		/// </summary>
		/// <param name="fileName">The name of the file to add.</param>
		/// <param name="entryName">The name to use for the <see cref="ZipEntry"/> on the Zip file created.</param>
		/// <exception cref="ArgumentNullException">Argument supplied is null.</exception>
		public void Add(string fileName, string entryName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			if (entryName == null)
			{
				throw new ArgumentNullException(nameof(entryName));
			}

			CheckUpdating();
			AddUpdate(new ZipUpdate(fileName, EntryFactory.MakeFileEntry(fileName, entryName, true)));
		}

		/// <summary>
		/// Add a file entry with data.
		/// </summary>
		/// <param name="dataSource">The source of the data for this entry.</param>
		/// <param name="entryName">The name to give to the entry.</param>
		public void Add(IStaticDataSource dataSource, string entryName)
		{
			if (dataSource == null)
			{
				throw new ArgumentNullException(nameof(dataSource));
			}

			if (entryName == null)
			{
				throw new ArgumentNullException(nameof(entryName));
			}

			CheckUpdating();
			AddUpdate(new ZipUpdate(dataSource, EntryFactory.MakeFileEntry(entryName, false)));
		}

		/// <summary>
		/// Add a file entry with data.
		/// </summary>
		/// <param name="dataSource">The source of the data for this entry.</param>
		/// <param name="entryName">The name to give to the entry.</param>
		/// <param name="compressionMethod">The compression method to use.</param>
		/// <exception cref="NotImplementedException">Compression method is not supported for creating entries.</exception>
		public void Add(IStaticDataSource dataSource, string entryName, CompressionMethod compressionMethod)
		{
			if (dataSource == null)
			{
				throw new ArgumentNullException(nameof(dataSource));
			}

			if (entryName == null)
			{
				throw new ArgumentNullException(nameof(entryName));
			}

			CheckSupportedCompressionMethod(compressionMethod);
			CheckUpdating();

			ZipEntry entry = EntryFactory.MakeFileEntry(entryName, false);
			entry.CompressionMethod = compressionMethod;

			AddUpdate(new ZipUpdate(dataSource, entry));
		}

		/// <summary>
		/// Add a file entry with data.
		/// </summary>
		/// <param name="dataSource">The source of the data for this entry.</param>
		/// <param name="entryName">The name to give to the entry.</param>
		/// <param name="compressionMethod">The compression method to use.</param>
		/// <param name="useUnicodeText">Ensure Unicode text is used for name and comments for this entry.</param>
		/// <exception cref="NotImplementedException">Compression method is not supported for creating entries.</exception>
		public void Add(IStaticDataSource dataSource, string entryName, CompressionMethod compressionMethod, bool useUnicodeText)
		{
			if (dataSource == null)
			{
				throw new ArgumentNullException(nameof(dataSource));
			}

			if (entryName == null)
			{
				throw new ArgumentNullException(nameof(entryName));
			}

			CheckSupportedCompressionMethod(compressionMethod);
			CheckUpdating();

			ZipEntry entry = EntryFactory.MakeFileEntry(entryName, false);
			entry.IsUnicodeText = useUnicodeText;
			entry.CompressionMethod = compressionMethod;

			AddUpdate(new ZipUpdate(dataSource, entry));
		}

		/// <summary>
		/// Add a <see cref="ZipEntry"/> that contains no data.
		/// </summary>
		/// <param name="entry">The entry to add.</param>
		/// <remarks>This can be used to add directories, volume labels, or empty file entries.</remarks>
		public void Add(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			CheckUpdating();

			if ((entry.Size != 0) || (entry.CompressedSize != 0))
			{
				throw new ZipException("Entry cannot have any data");
			}

			AddUpdate(new ZipUpdate(UpdateCommand.Add, entry));
		}

		/// <summary>
		/// Add a <see cref="ZipEntry"/> with data.
		/// </summary>
		/// <param name="dataSource">The source of the data for this entry.</param>
		/// <param name="entry">The entry to add.</param>
		/// <remarks>This can be used to add file entries with a custom data source.</remarks>
		/// <exception cref="NotSupportedException">
		/// The encryption method specified in <paramref name="entry"/> is unsupported.
		/// </exception>
		/// <exception cref="NotImplementedException">Compression method is not supported for creating entries.</exception>
		public void Add(IStaticDataSource dataSource, ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			if (dataSource == null)
			{
				throw new ArgumentNullException(nameof(dataSource));
			}

			// We don't currently support adding entries with AES encryption, so throw
			// up front instead of failing or falling back to ZipCrypto later on
			if (entry.AESKeySize > 0)
			{
				throw new NotSupportedException("Creation of AES encrypted entries is not supported");
			}

			CheckSupportedCompressionMethod(entry.CompressionMethod);
			CheckUpdating();

			AddUpdate(new ZipUpdate(dataSource, entry));
		}

		/// <summary>
		/// Add a directory entry to the archive.
		/// </summary>
		/// <param name="directoryName">The directory to add.</param>
		public void AddDirectory(string directoryName)
		{
			if (directoryName == null)
			{
				throw new ArgumentNullException(nameof(directoryName));
			}

			CheckUpdating();

			ZipEntry dirEntry = EntryFactory.MakeDirectoryEntry(directoryName);
			AddUpdate(new ZipUpdate(UpdateCommand.Add, dirEntry));
		}

		/// <summary>
		/// Check if the specified compression method is supported for adding a new entry.
		/// </summary>
		/// <param name="compressionMethod">The compression method for the new entry.</param>
		private static void CheckSupportedCompressionMethod(CompressionMethod compressionMethod)
		{
			if (compressionMethod != CompressionMethod.Deflated && compressionMethod != CompressionMethod.Stored && compressionMethod != CompressionMethod.BZip2)
			{
				throw new NotImplementedException("Compression method not supported");
			}
		}

		#endregion Adding Entries

		#region Modifying Entries

		/* Modify not yet ready for public consumption.
		   Direct modification of an entry should not overwrite original data before its read.
		   Safe mode is trivial in this sense.
				public void Modify(ZipEntry original, ZipEntry updated)
				{
					if ( original == null ) {
						throw new ArgumentNullException("original");
					}
					if ( updated == null ) {
						throw new ArgumentNullException("updated");
					}
					CheckUpdating();
					contentsEdited_ = true;
					updates_.Add(new ZipUpdate(original, updated));
				}
		*/

		#endregion Modifying Entries

		#region Deleting Entries

		/// <summary>
		/// Delete an entry by name
		/// </summary>
		/// <param name="fileName">The filename to delete</param>
		/// <returns>True if the entry was found and deleted; false otherwise.</returns>
		public bool Delete(string fileName)
		{
			if (fileName == null)
			{
				throw new ArgumentNullException(nameof(fileName));
			}

			CheckUpdating();

			bool result = false;
			int index = FindExistingUpdate(fileName);
			if ((index >= 0) && (updates_[index] != null))
			{
				result = true;
				contentsEdited_ = true;
				updates_[index] = null;
				updateCount_ -= 1;
			}
			else
			{
				throw new ZipException("Cannot find entry to delete");
			}
			return result;
		}

		/// <summary>
		/// Delete a <see cref="ZipEntry"/> from the archive.
		/// </summary>
		/// <param name="entry">The entry to delete.</param>
		public void Delete(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			CheckUpdating();

			int index = FindExistingUpdate(entry);
			if (index >= 0)
			{
				contentsEdited_ = true;
				updates_[index] = null;
				updateCount_ -= 1;
			}
			else
			{
				throw new ZipException("Cannot find entry to delete");
			}
		}

		#endregion Deleting Entries

		#region Update Support

		#region Writing Values/Headers

		private void WriteLEShort(int value)
		{
			baseStream_.WriteByte((byte)(value & 0xff));
			baseStream_.WriteByte((byte)((value >> 8) & 0xff));
		}

		/// <summary>
		/// Write an unsigned short in little endian byte order.
		/// </summary>
		private void WriteLEUshort(ushort value)
		{
			baseStream_.WriteByte((byte)(value & 0xff));
			baseStream_.WriteByte((byte)(value >> 8));
		}

		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		private void WriteLEInt(int value)
		{
			WriteLEShort(value & 0xffff);
			WriteLEShort(value >> 16);
		}

		/// <summary>
		/// Write an unsigned int in little endian byte order.
		/// </summary>
		private void WriteLEUint(uint value)
		{
			WriteLEUshort((ushort)(value & 0xffff));
			WriteLEUshort((ushort)(value >> 16));
		}

		/// <summary>
		/// Write a long in little endian byte order.
		/// </summary>
		private void WriteLeLong(long value)
		{
			WriteLEInt((int)(value & 0xffffffff));
			WriteLEInt((int)(value >> 32));
		}

		private void WriteLEUlong(ulong value)
		{
			WriteLEUint((uint)(value & 0xffffffff));
			WriteLEUint((uint)(value >> 32));
		}

		private void WriteLocalEntryHeader(ZipUpdate update)
		{
			ZipEntry entry = update.OutEntry;

			// TODO: Local offset will require adjusting for multi-disk zip files.
			entry.Offset = baseStream_.Position;

			// TODO: Need to clear any entry flags that dont make sense or throw an exception here.
			if (update.Command != UpdateCommand.Copy)
			{
				if (entry.CompressionMethod == CompressionMethod.Deflated)
				{
					if (entry.Size == 0)
					{
						// No need to compress - no data.
						entry.CompressedSize = entry.Size;
						entry.Crc = 0;
						entry.CompressionMethod = CompressionMethod.Stored;
					}
				}
				else if (entry.CompressionMethod == CompressionMethod.Stored)
				{
					entry.Flags &= ~(int)GeneralBitFlags.Descriptor;
				}

				if (HaveKeys)
				{
					entry.IsCrypted = true;
					if (entry.Crc < 0)
					{
						entry.Flags |= (int)GeneralBitFlags.Descriptor;
					}
				}
				else
				{
					entry.IsCrypted = false;
				}

				switch (useZip64_)
				{
					case UseZip64.Dynamic:
						if (entry.Size < 0)
						{
							entry.ForceZip64();
						}
						break;

					case UseZip64.On:
						entry.ForceZip64();
						break;

					case UseZip64.Off:
						// Do nothing.  The entry itself may be using Zip64 independently.
						break;
				}
			}

			// Write the local file header
			WriteLEInt(ZipConstants.LocalHeaderSignature);

			WriteLEShort(entry.Version);
			WriteLEShort(entry.Flags);

			WriteLEShort((byte)entry.CompressionMethodForHeader);
			WriteLEInt((int)entry.DosTime);

			if (!entry.HasCrc)
			{
				// Note patch address for updating CRC later.
				update.CrcPatchOffset = baseStream_.Position;
				WriteLEInt((int)0);
			}
			else
			{
				WriteLEInt(unchecked((int)entry.Crc));
			}

			if (entry.LocalHeaderRequiresZip64)
			{
				WriteLEInt(-1);
				WriteLEInt(-1);
			}
			else
			{
				if ((entry.CompressedSize < 0) || (entry.Size < 0))
				{
					update.SizePatchOffset = baseStream_.Position;
				}

				WriteLEInt((int)entry.CompressedSize);
				WriteLEInt((int)entry.Size);
			}

			var entryEncoding = _stringCodec.ZipInputEncoding(entry.Flags);
			byte[] name = entryEncoding.GetBytes(entry.Name);

			if (name.Length > 0xFFFF)
			{
				throw new ZipException("Entry name too long.");
			}

			var ed = new ZipExtraData(entry.ExtraData);

			if (entry.LocalHeaderRequiresZip64)
			{
				ed.StartNewEntry();

				// Local entry header always includes size and compressed size.
				// NOTE the order of these fields is reversed when compared to the normal headers!
				ed.AddLeLong(entry.Size);
				ed.AddLeLong(entry.CompressedSize);
				ed.AddNewEntry(1);
			}
			else
			{
				ed.Delete(1);
			}

			entry.ExtraData = ed.GetEntryData();

			WriteLEShort(name.Length);
			WriteLEShort(entry.ExtraData.Length);

			if (name.Length > 0)
			{
				baseStream_.Write(name, 0, name.Length);
			}

			if (entry.LocalHeaderRequiresZip64)
			{
				if (!ed.Find(1))
				{
					throw new ZipException("Internal error cannot find extra data");
				}

				update.SizePatchOffset = baseStream_.Position + ed.CurrentReadIndex;
			}

			if (entry.ExtraData.Length > 0)
			{
				baseStream_.Write(entry.ExtraData, 0, entry.ExtraData.Length);
			}
		}

		private int WriteCentralDirectoryHeader(ZipEntry entry)
		{
			if (entry.CompressedSize < 0)
			{
				throw new ZipException("Attempt to write central directory entry with unknown csize");
			}

			if (entry.Size < 0)
			{
				throw new ZipException("Attempt to write central directory entry with unknown size");
			}

			if (entry.Crc < 0)
			{
				throw new ZipException("Attempt to write central directory entry with unknown crc");
			}

			// Write the central file header
			WriteLEInt(ZipConstants.CentralHeaderSignature);

			// Version made by
			WriteLEShort((entry.HostSystem << 8) | entry.VersionMadeBy);

			// Version required to extract
			WriteLEShort(entry.Version);

			WriteLEShort(entry.Flags);

			unchecked
			{
				WriteLEShort((byte)entry.CompressionMethodForHeader);
				WriteLEInt((int)entry.DosTime);
				WriteLEInt((int)entry.Crc);
			}

			bool useExtraCompressedSize = false; //Do we want to store the compressed size in the extra data?
			if ((entry.IsZip64Forced()) || (entry.CompressedSize >= 0xffffffff))
			{
				useExtraCompressedSize = true;
				WriteLEInt(-1);
			}
			else
			{
				WriteLEInt((int)(entry.CompressedSize & 0xffffffff));
			}

			bool useExtraUncompressedSize = false; //Do we want to store the uncompressed size in the extra data?
			if ((entry.IsZip64Forced()) || (entry.Size >= 0xffffffff))
			{
				useExtraUncompressedSize = true;
				WriteLEInt(-1);
			}
			else
			{
				WriteLEInt((int)entry.Size);
			}

			var entryEncoding = _stringCodec.ZipInputEncoding(entry.Flags);
			byte[] name = entryEncoding.GetBytes(entry.Name);

			if (name.Length > 0xFFFF)
			{
				throw new ZipException("Entry name is too long.");
			}

			WriteLEShort(name.Length);

			// Central header extra data is different to local header version so regenerate.
			var ed = new ZipExtraData(entry.ExtraData);

			if (entry.CentralHeaderRequiresZip64)
			{
				ed.StartNewEntry();

				if (useExtraUncompressedSize)
				{
					ed.AddLeLong(entry.Size);
				}

				if (useExtraCompressedSize)
				{
					ed.AddLeLong(entry.CompressedSize);
				}

				if (entry.Offset >= 0xffffffff)
				{
					ed.AddLeLong(entry.Offset);
				}

				// Number of disk on which this file starts isnt supported and is never written here.
				ed.AddNewEntry(1);
			}
			else
			{
				// Should have already be done when local header was added.
				ed.Delete(1);
			}

			byte[] centralExtraData = ed.GetEntryData();

			WriteLEShort(centralExtraData.Length);
			WriteLEShort(entry.Comment != null ? entry.Comment.Length : 0);

			WriteLEShort(0);    // disk number
			WriteLEShort(0);    // internal file attributes

			// External file attributes...
			if (entry.ExternalFileAttributes != -1)
			{
				WriteLEInt(entry.ExternalFileAttributes);
			}
			else
			{
				if (entry.IsDirectory)
				{
					WriteLEUint(16);
				}
				else
				{
					WriteLEUint(0);
				}
			}

			if (entry.Offset >= 0xffffffff)
			{
				WriteLEUint(0xffffffff);
			}
			else
			{
				WriteLEUint((uint)(int)entry.Offset);
			}

			if (name.Length > 0)
			{
				baseStream_.Write(name, 0, name.Length);
			}

			if (centralExtraData.Length > 0)
			{
				baseStream_.Write(centralExtraData, 0, centralExtraData.Length);
			}

			byte[] rawComment = (entry.Comment != null) ? Encoding.ASCII.GetBytes(entry.Comment) : Empty.Array<byte>();

			if (rawComment.Length > 0)
			{
				baseStream_.Write(rawComment, 0, rawComment.Length);
			}

			return ZipConstants.CentralHeaderBaseSize + name.Length + centralExtraData.Length + rawComment.Length;
		}

		#endregion Writing Values/Headers

		private void PostUpdateCleanup()
		{
			updateDataSource_ = null;
			updates_ = null;
			updateIndex_ = null;

			if (archiveStorage_ != null)
			{
				archiveStorage_.Dispose();
				archiveStorage_ = null;
			}
		}

		private string GetTransformedFileName(string name)
		{
			INameTransform transform = NameTransform;
			return (transform != null) ?
				transform.TransformFile(name) :
				name;
		}

		private string GetTransformedDirectoryName(string name)
		{
			INameTransform transform = NameTransform;
			return (transform != null) ?
				transform.TransformDirectory(name) :
				name;
		}

		/// <summary>
		/// Get a raw memory buffer.
		/// </summary>
		/// <returns>Returns a raw memory buffer.</returns>
		private byte[] GetBuffer()
		{
			if (copyBuffer_ == null)
			{
				copyBuffer_ = new byte[bufferSize_];
			}
			return copyBuffer_;
		}

		private void CopyDescriptorBytes(ZipUpdate update, Stream dest, Stream source)
		{
			// Don't include the signature size to allow copy without seeking
			var bytesToCopy = GetDescriptorSize(update, false);

			// Don't touch the source stream if no descriptor is present
			if (bytesToCopy == 0) return;

			var buffer = GetBuffer();

			// Copy the first 4 bytes of the descriptor
			source.Read(buffer, 0, sizeof(int));
			dest.Write(buffer, 0, sizeof(int));

			if (BitConverter.ToUInt32(buffer, 0) != ZipConstants.DataDescriptorSignature)
			{
				// The initial bytes wasn't the descriptor, reduce the pending byte count
				bytesToCopy -= buffer.Length;
			}

			while (bytesToCopy > 0)
			{
				int readSize = Math.Min(buffer.Length, bytesToCopy);

				int bytesRead = source.Read(buffer, 0, readSize);
				if (bytesRead > 0)
				{
					dest.Write(buffer, 0, bytesRead);
					bytesToCopy -= bytesRead;
				}
				else
				{
					throw new ZipException("Unxpected end of stream");
				}
			}
		}

		private void CopyBytes(ZipUpdate update, Stream destination, Stream source,
			long bytesToCopy, bool updateCrc)
		{
			if (destination == source)
			{
				throw new InvalidOperationException("Destination and source are the same");
			}

			// NOTE: Compressed size is updated elsewhere.
			var crc = new Crc32();
			byte[] buffer = GetBuffer();

			long targetBytes = bytesToCopy;
			long totalBytesRead = 0;

			int bytesRead;
			do
			{
				int readSize = buffer.Length;

				if (bytesToCopy < readSize)
				{
					readSize = (int)bytesToCopy;
				}

				bytesRead = source.Read(buffer, 0, readSize);
				if (bytesRead > 0)
				{
					if (updateCrc)
					{
						crc.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
					}
					destination.Write(buffer, 0, bytesRead);
					bytesToCopy -= bytesRead;
					totalBytesRead += bytesRead;
				}
			}
			while ((bytesRead > 0) && (bytesToCopy > 0));

			if (totalBytesRead != targetBytes)
			{
				throw new ZipException(string.Format("Failed to copy bytes expected {0} read {1}", targetBytes, totalBytesRead));
			}

			if (updateCrc)
			{
				update.OutEntry.Crc = crc.Value;
			}
		}

		/// <summary>
		/// Get the size of the source descriptor for a <see cref="ZipUpdate"/>.
		/// </summary>
		/// <param name="update">The update to get the size for.</param>
		/// <param name="includingSignature">Whether to include the signature size</param>
		/// <returns>The descriptor size, zero if there isn't one.</returns>
		private static int GetDescriptorSize(ZipUpdate update, bool includingSignature)
		{
			if (!((GeneralBitFlags)update.Entry.Flags).HasFlag(GeneralBitFlags.Descriptor))
				return 0;

			var descriptorWithSignature = update.Entry.LocalHeaderRequiresZip64
				? ZipConstants.Zip64DataDescriptorSize
				: ZipConstants.DataDescriptorSize;

			return includingSignature
				? descriptorWithSignature
				: descriptorWithSignature - sizeof(int);
		}

		private void CopyDescriptorBytesDirect(ZipUpdate update, Stream stream, ref long destinationPosition, long sourcePosition)
		{
			var buffer = GetBuffer(); ;

			stream.Position = sourcePosition;
			stream.Read(buffer, 0, sizeof(int));
			var sourceHasSignature = BitConverter.ToUInt32(buffer, 0) == ZipConstants.DataDescriptorSignature;

			var bytesToCopy = GetDescriptorSize(update, sourceHasSignature);

			while (bytesToCopy > 0)
			{
				stream.Position = sourcePosition;

				var bytesRead = stream.Read(buffer, 0, bytesToCopy);
				if (bytesRead > 0)
				{
					stream.Position = destinationPosition;
					stream.Write(buffer, 0, bytesRead);
					bytesToCopy -= bytesRead;
					destinationPosition += bytesRead;
					sourcePosition += bytesRead;
				}
				else
				{
					throw new ZipException("Unexpected end of stream");
				}
			}
		}

		private void CopyEntryDataDirect(ZipUpdate update, Stream stream, bool updateCrc, ref long destinationPosition, ref long sourcePosition)
		{
			long bytesToCopy = update.Entry.CompressedSize;

			// NOTE: Compressed size is updated elsewhere.
			var crc = new Crc32();
			byte[] buffer = GetBuffer();

			long targetBytes = bytesToCopy;
			long totalBytesRead = 0;

			int bytesRead;
			do
			{
				int readSize = buffer.Length;

				if (bytesToCopy < readSize)
				{
					readSize = (int)bytesToCopy;
				}

				stream.Position = sourcePosition;
				bytesRead = stream.Read(buffer, 0, readSize);
				if (bytesRead > 0)
				{
					if (updateCrc)
					{
						crc.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
					}
					stream.Position = destinationPosition;
					stream.Write(buffer, 0, bytesRead);

					destinationPosition += bytesRead;
					sourcePosition += bytesRead;
					bytesToCopy -= bytesRead;
					totalBytesRead += bytesRead;
				}
			}
			while ((bytesRead > 0) && (bytesToCopy > 0));

			if (totalBytesRead != targetBytes)
			{
				throw new ZipException(string.Format("Failed to copy bytes expected {0} read {1}", targetBytes, totalBytesRead));
			}

			if (updateCrc)
			{
				update.OutEntry.Crc = crc.Value;
			}
		}

		private int FindExistingUpdate(ZipEntry entry)
		{
			int result = -1;
			if (updateIndex_.ContainsKey(entry.Name))
			{
				result = (int)updateIndex_[entry.Name];
			}
			/*
						// This is slow like the coming of the next ice age but takes less storage and may be useful
						// for CF?
						for (int index = 0; index < updates_.Count; ++index)
						{
							ZipUpdate zu = ( ZipUpdate )updates_[index];
							if ( (zu.Entry.ZipFileIndex == entry.ZipFileIndex) &&
								(string.Compare(convertedName, zu.Entry.Name, true, CultureInfo.InvariantCulture) == 0) ) {
								result = index;
								break;
							}
						}
			 */
			return result;
		}

		private int FindExistingUpdate(string fileName, bool isEntryName = false)
		{
			int result = -1;

			string convertedName = !isEntryName ? GetTransformedFileName(fileName) : fileName;

			if (updateIndex_.ContainsKey(convertedName))
			{
				result = (int)updateIndex_[convertedName];
			}

			/*
						// This is slow like the coming of the next ice age but takes less storage and may be useful
						// for CF?
						for ( int index = 0; index < updates_.Count; ++index ) {
							if ( string.Compare(convertedName, (( ZipUpdate )updates_[index]).Entry.Name,
								true, CultureInfo.InvariantCulture) == 0 ) {
								result = index;
								break;
							}
						}
			 */

			return result;
		}

		/// <summary>
		/// Get an output stream for the specified <see cref="ZipEntry"/>
		/// </summary>
		/// <param name="entry">The entry to get an output stream for.</param>
		/// <returns>The output stream obtained for the entry.</returns>
		private Stream GetOutputStream(ZipEntry entry)
		{
			Stream result = baseStream_;

			if (entry.IsCrypted == true)
			{
				result = CreateAndInitEncryptionStream(result, entry);
			}

			switch (entry.CompressionMethod)
			{
				case CompressionMethod.Stored:
					if (!entry.IsCrypted)
					{
						// If there is an encryption stream in use, that can be returned directly
						// otherwise, wrap the base stream in an UncompressedStream instead of returning it directly
						result = new UncompressedStream(result);
					}
					break;

				case CompressionMethod.Deflated:
					var dos = new DeflaterOutputStream(result, new Deflater(9, true))
					{
						// If there is an encryption stream in use, then we want that to be disposed when the deflator stream is disposed
						// If not, then we don't want it to dispose the base stream
						IsStreamOwner = entry.IsCrypted
					};
					result = dos;
					break;

				case CompressionMethod.BZip2:
					var bzos = new BZip2.BZip2OutputStream(result)
					{
						// If there is an encryption stream in use, then we want that to be disposed when the BZip2OutputStream stream is disposed
						// If not, then we don't want it to dispose the base stream
						IsStreamOwner = entry.IsCrypted
					};
					result = bzos;
					break;

				default:
					throw new ZipException("Unknown compression method " + entry.CompressionMethod);
			}
			return result;
		}

		private void AddEntry(ZipFile workFile, ZipUpdate update)
		{
			Stream source = null;

			if (update.Entry.IsFile)
			{
				source = update.GetSource();

				if (source == null)
				{
					source = updateDataSource_.GetSource(update.Entry, update.Filename);
				}
			}

			var useCrc = update.Entry.AESKeySize == 0;

			if (source != null)
			{
				using (source)
				{
					long sourceStreamLength = source.Length;
					if (update.OutEntry.Size < 0)
					{
						update.OutEntry.Size = sourceStreamLength;
					}
					else
					{
						// Check for errant entries.
						if (update.OutEntry.Size != sourceStreamLength)
						{
							throw new ZipException("Entry size/stream size mismatch");
						}
					}

					workFile.WriteLocalEntryHeader(update);

					long dataStart = workFile.baseStream_.Position;

					using (Stream output = workFile.GetOutputStream(update.OutEntry))
					{
						CopyBytes(update, output, source, sourceStreamLength, useCrc);
					}

					long dataEnd = workFile.baseStream_.Position;
					update.OutEntry.CompressedSize = dataEnd - dataStart;

					if ((update.OutEntry.Flags & (int)GeneralBitFlags.Descriptor) == (int)GeneralBitFlags.Descriptor)
					{
						ZipFormat.WriteDataDescriptor(workFile.baseStream_, update.OutEntry);
					}
				}
			}
			else
			{
				workFile.WriteLocalEntryHeader(update);
				update.OutEntry.CompressedSize = 0;
			}
		}

		private void ModifyEntry(ZipFile workFile, ZipUpdate update)
		{
			workFile.WriteLocalEntryHeader(update);
			long dataStart = workFile.baseStream_.Position;

			// TODO: This is slow if the changes don't effect the data!!
			if (update.Entry.IsFile && (update.Filename != null))
			{
				using (Stream output = workFile.GetOutputStream(update.OutEntry))
				{
					using (Stream source = this.GetInputStream(update.Entry))
					{
						CopyBytes(update, output, source, source.Length, true);
					}
				}
			}

			long dataEnd = workFile.baseStream_.Position;
			update.Entry.CompressedSize = dataEnd - dataStart;
		}

		private void CopyEntryDirect(ZipFile workFile, ZipUpdate update, ref long destinationPosition)
		{
			bool skipOver = false || update.Entry.Offset == destinationPosition;

			if (!skipOver)
			{
				baseStream_.Position = destinationPosition;
				workFile.WriteLocalEntryHeader(update);
				destinationPosition = baseStream_.Position;
			}

			long sourcePosition = 0;

			const int NameLengthOffset = 26;

			// TODO: Add base for SFX friendly handling
			long entryDataOffset = update.Entry.Offset + NameLengthOffset;

			baseStream_.Seek(entryDataOffset, SeekOrigin.Begin);

			// Clumsy way of handling retrieving the original name and extra data length for now.
			// TODO: Stop re-reading name and data length in CopyEntryDirect.

			uint nameLength = ReadLEUshort();
			uint extraLength = ReadLEUshort();

			sourcePosition = baseStream_.Position + nameLength + extraLength;

			if (skipOver)
			{
				if (update.OffsetBasedSize != -1)
				{
					destinationPosition += update.OffsetBasedSize;
				}
				else
				{
					// Skip entry header
					destinationPosition += (sourcePosition - entryDataOffset) + NameLengthOffset;

					// Skip entry compressed data
					destinationPosition += update.Entry.CompressedSize;

					// Seek to end of entry to check for descriptor signature
					baseStream_.Seek(destinationPosition, SeekOrigin.Begin);

					var descriptorHasSignature = ReadLEUint() == ZipConstants.DataDescriptorSignature;

					// Skip descriptor and it's signature (if present)
					destinationPosition += GetDescriptorSize(update, descriptorHasSignature);
				}
			}
			else
			{
				if (update.Entry.CompressedSize > 0)
				{
					CopyEntryDataDirect(update, baseStream_, false, ref destinationPosition, ref sourcePosition);
				}
				CopyDescriptorBytesDirect(update, baseStream_, ref destinationPosition, sourcePosition);
			}
		}

		private void CopyEntry(ZipFile workFile, ZipUpdate update)
		{
			workFile.WriteLocalEntryHeader(update);

			if (update.Entry.CompressedSize > 0)
			{
				const int NameLengthOffset = 26;

				long entryDataOffset = update.Entry.Offset + NameLengthOffset;

				// TODO: This wont work for SFX files!
				baseStream_.Seek(entryDataOffset, SeekOrigin.Begin);

				uint nameLength = ReadLEUshort();
				uint extraLength = ReadLEUshort();

				baseStream_.Seek(nameLength + extraLength, SeekOrigin.Current);

				CopyBytes(update, workFile.baseStream_, baseStream_, update.Entry.CompressedSize, false);
			}
			CopyDescriptorBytes(update, workFile.baseStream_, baseStream_);
		}

		private void Reopen(Stream source)
		{
			isNewArchive_ = false;
			baseStream_ = source ?? throw new ZipException("Failed to reopen archive - no source");
			ReadEntries();
		}

		private void Reopen()
		{
			if (Name == null)
			{
				throw new InvalidOperationException("Name is not known cannot Reopen");
			}

			Reopen(File.Open(Name, FileMode.Open, FileAccess.Read, FileShare.Read));
		}

		private void UpdateCommentOnly()
		{
			long baseLength = baseStream_.Length;

			Stream updateFile;

			if (archiveStorage_.UpdateMode == FileUpdateMode.Safe)
			{
				updateFile = archiveStorage_.MakeTemporaryCopy(baseStream_);

				baseStream_.Dispose();
				baseStream_ = null;
			}
			else
			{
				if (archiveStorage_.UpdateMode == FileUpdateMode.Direct)
				{
					// TODO: archiveStorage wasnt originally intended for this use.
					// Need to revisit this to tidy up handling as archive storage currently doesnt
					// handle the original stream well.
					// The problem is when using an existing zip archive with an in memory archive storage.
					// The open stream wont support writing but the memory storage should open the same file not an in memory one.

					// Need to tidy up the archive storage interface and contract basically.
					baseStream_ = archiveStorage_.OpenForDirectUpdate(baseStream_);
					updateFile = baseStream_;
				}
				else
				{
					baseStream_.Dispose();
					baseStream_ = null;
					updateFile = new FileStream(Name, FileMode.Open, FileAccess.ReadWrite);
				}
			}

			try
			{
				long locatedCentralDirOffset =
					ZipFormat.LocateBlockWithSignature(updateFile, ZipConstants.EndOfCentralDirectorySignature,
						baseLength, ZipConstants.EndOfCentralRecordBaseSize, 0xffff);
				if (locatedCentralDirOffset < 0)
				{
					throw new ZipException("Cannot find central directory");
				}

				const int CentralHeaderCommentSizeOffset = 16;
				updateFile.Position += CentralHeaderCommentSizeOffset;

				byte[] rawComment = newComment_.RawComment;

				updateFile.WriteLEShort(rawComment.Length);
				updateFile.Write(rawComment, 0, rawComment.Length);
				updateFile.SetLength(updateFile.Position);
			}
			finally
			{
				if (updateFile != baseStream_)
					updateFile.Dispose();
			}

			if (archiveStorage_.UpdateMode == FileUpdateMode.Safe)
			{
				Reopen(archiveStorage_.ConvertTemporaryToFinal());
			}
			else
			{
				ReadEntries();
			}
		}

		/// <summary>
		/// Class used to sort updates.
		/// </summary>
		private class UpdateComparer : IComparer<ZipUpdate>
		{
			/// <summary>
			/// Compares two objects and returns a value indicating whether one is
			/// less than, equal to or greater than the other.
			/// </summary>
			/// <param name="x">First object to compare</param>
			/// <param name="y">Second object to compare.</param>
			/// <returns>Compare result.</returns>
			public int Compare(ZipUpdate x, ZipUpdate y)
			{
				int result;

				if (x == null)
				{
					if (y == null)
					{
						result = 0;
					}
					else
					{
						result = -1;
					}
				}
				else if (y == null)
				{
					result = 1;
				}
				else
				{
					int xCmdValue = ((x.Command == UpdateCommand.Copy) || (x.Command == UpdateCommand.Modify)) ? 0 : 1;
					int yCmdValue = ((y.Command == UpdateCommand.Copy) || (y.Command == UpdateCommand.Modify)) ? 0 : 1;

					result = xCmdValue - yCmdValue;
					if (result == 0)
					{
						long offsetDiff = x.Entry.Offset - y.Entry.Offset;
						if (offsetDiff < 0)
						{
							result = -1;
						}
						else if (offsetDiff == 0)
						{
							result = 0;
						}
						else
						{
							result = 1;
						}
					}
				}
				return result;
			}
		}

		private void RunUpdates()
		{
			long sizeEntries = 0;
			long endOfStream = 0;
			bool directUpdate = false;
			long destinationPosition = 0; // NOT SFX friendly

			ZipFile workFile;

			if (IsNewArchive)
			{
				workFile = this;
				workFile.baseStream_.Position = 0;
				directUpdate = true;
			}
			else if (archiveStorage_.UpdateMode == FileUpdateMode.Direct)
			{
				workFile = this;
				workFile.baseStream_.Position = 0;
				directUpdate = true;

				// Sort the updates by offset within copies/modifies, then adds.
				// This ensures that data required by copies will not be overwritten.
				updates_.Sort(new UpdateComparer());
			}
			else
			{
				workFile = ZipFile.Create(archiveStorage_.GetTemporaryOutput());
				workFile.UseZip64 = UseZip64;

				if (key != null)
				{
					workFile.key = (byte[])key.Clone();
				}
			}

			try
			{
				foreach (ZipUpdate update in updates_)
				{
					if (update != null)
					{
						switch (update.Command)
						{
							case UpdateCommand.Copy:
								if (directUpdate)
								{
									CopyEntryDirect(workFile, update, ref destinationPosition);
								}
								else
								{
									CopyEntry(workFile, update);
								}
								break;

							case UpdateCommand.Modify:
								// TODO: Direct modifying of an entry will take some legwork.
								ModifyEntry(workFile, update);
								break;

							case UpdateCommand.Add:
								if (!IsNewArchive && directUpdate)
								{
									workFile.baseStream_.Position = destinationPosition;
								}

								AddEntry(workFile, update);

								if (directUpdate)
								{
									destinationPosition = workFile.baseStream_.Position;
								}
								break;
						}
					}
				}

				if (!IsNewArchive && directUpdate)
				{
					workFile.baseStream_.Position = destinationPosition;
				}

				long centralDirOffset = workFile.baseStream_.Position;

				foreach (ZipUpdate update in updates_)
				{
					if (update != null)
					{
						sizeEntries += workFile.WriteCentralDirectoryHeader(update.OutEntry);
					}
				}

				byte[] theComment = newComment_?.RawComment ?? _stringCodec.ZipArchiveCommentEncoding.GetBytes(comment_);
				ZipFormat.WriteEndOfCentralDirectory(workFile.baseStream_, updateCount_,
					sizeEntries, centralDirOffset, theComment);

				endOfStream = workFile.baseStream_.Position;

				// And now patch entries...
				foreach (ZipUpdate update in updates_)
				{
					if (update != null)
					{
						// If the size of the entry is zero leave the crc as 0 as well.
						// The calculated crc will be all bits on...
						if ((update.CrcPatchOffset > 0) && (update.OutEntry.CompressedSize > 0))
						{
							workFile.baseStream_.Position = update.CrcPatchOffset;
							workFile.WriteLEInt((int)update.OutEntry.Crc);
						}

						if (update.SizePatchOffset > 0)
						{
							workFile.baseStream_.Position = update.SizePatchOffset;
							if (update.OutEntry.LocalHeaderRequiresZip64)
							{
								workFile.WriteLeLong(update.OutEntry.Size);
								workFile.WriteLeLong(update.OutEntry.CompressedSize);
							}
							else
							{
								workFile.WriteLEInt((int)update.OutEntry.CompressedSize);
								workFile.WriteLEInt((int)update.OutEntry.Size);
							}
						}
					}
				}
			}
			catch
			{
				workFile.Close();
				if (!directUpdate && (workFile.Name != null))
				{
					File.Delete(workFile.Name);
				}
				throw;
			}

			if (directUpdate)
			{
				workFile.baseStream_.SetLength(endOfStream);
				workFile.baseStream_.Flush();
				isNewArchive_ = false;
				ReadEntries();
			}
			else
			{
				baseStream_.Dispose();
				Reopen(archiveStorage_.ConvertTemporaryToFinal());
			}
		}

		private void CheckUpdating()
		{
			if (updates_ == null)
			{
				throw new InvalidOperationException("BeginUpdate has not been called");
			}
		}

		#endregion Update Support

		#region ZipUpdate class

		/// <summary>
		/// Represents a pending update to a Zip file.
		/// </summary>
		private class ZipUpdate
		{
			#region Constructors

			public ZipUpdate(string fileName, ZipEntry entry)
			{
				command_ = UpdateCommand.Add;
				entry_ = entry;
				filename_ = fileName;
			}

			[Obsolete]
			public ZipUpdate(string fileName, string entryName, CompressionMethod compressionMethod)
			{
				command_ = UpdateCommand.Add;
				entry_ = new ZipEntry(entryName)
				{
					CompressionMethod = compressionMethod
				};
				filename_ = fileName;
			}

			[Obsolete]
			public ZipUpdate(string fileName, string entryName)
				: this(fileName, entryName, CompressionMethod.Deflated)
			{
				// Do nothing.
			}

			[Obsolete]
			public ZipUpdate(IStaticDataSource dataSource, string entryName, CompressionMethod compressionMethod)
			{
				command_ = UpdateCommand.Add;
				entry_ = new ZipEntry(entryName)
				{
					CompressionMethod = compressionMethod
				};
				dataSource_ = dataSource;
			}

			public ZipUpdate(IStaticDataSource dataSource, ZipEntry entry)
			{
				command_ = UpdateCommand.Add;
				entry_ = entry;
				dataSource_ = dataSource;
			}

			public ZipUpdate(ZipEntry original, ZipEntry updated)
			{
				throw new ZipException("Modify not currently supported");
				/*
					command_ = UpdateCommand.Modify;
					entry_ = ( ZipEntry )original.Clone();
					outEntry_ = ( ZipEntry )updated.Clone();
				*/
			}

			public ZipUpdate(UpdateCommand command, ZipEntry entry)
			{
				command_ = command;
				entry_ = (ZipEntry)entry.Clone();
			}

			/// <summary>
			/// Copy an existing entry.
			/// </summary>
			/// <param name="entry">The existing entry to copy.</param>
			public ZipUpdate(ZipEntry entry)
				: this(UpdateCommand.Copy, entry)
			{
				// Do nothing.
			}

			#endregion Constructors

			/// <summary>
			/// Get the <see cref="ZipEntry"/> for this update.
			/// </summary>
			/// <remarks>This is the source or original entry.</remarks>
			public ZipEntry Entry
			{
				get { return entry_; }
			}

			/// <summary>
			/// Get the <see cref="ZipEntry"/> that will be written to the updated/new file.
			/// </summary>
			public ZipEntry OutEntry
			{
				get
				{
					if (outEntry_ == null)
					{
						outEntry_ = (ZipEntry)entry_.Clone();
					}

					return outEntry_;
				}
			}

			/// <summary>
			/// Get the command for this update.
			/// </summary>
			public UpdateCommand Command
			{
				get { return command_; }
			}

			/// <summary>
			/// Get the filename if any for this update.  Null if none exists.
			/// </summary>
			public string Filename
			{
				get { return filename_; }
			}

			/// <summary>
			/// Get/set the location of the size patch for this update.
			/// </summary>
			public long SizePatchOffset
			{
				get { return sizePatchOffset_; }
				set { sizePatchOffset_ = value; }
			}

			/// <summary>
			/// Get /set the location of the crc patch for this update.
			/// </summary>
			public long CrcPatchOffset
			{
				get { return crcPatchOffset_; }
				set { crcPatchOffset_ = value; }
			}

			/// <summary>
			/// Get/set the size calculated by offset.
			/// Specifically, the difference between this and next entry's starting offset.
			/// </summary>
			public long OffsetBasedSize
			{
				get { return _offsetBasedSize; }
				set { _offsetBasedSize = value; }
			}

			public Stream GetSource()
			{
				Stream result = null;
				if (dataSource_ != null)
				{
					result = dataSource_.GetSource();
				}

				return result;
			}

			#region Instance Fields

			private ZipEntry entry_;
			private ZipEntry outEntry_;
			private readonly UpdateCommand command_;
			private IStaticDataSource dataSource_;
			private readonly string filename_;
			private long sizePatchOffset_ = -1;
			private long crcPatchOffset_ = -1;
			private long _offsetBasedSize = -1;

			#endregion Instance Fields
		}

		#endregion ZipUpdate class

		#endregion Updating

		#region Disposing

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			Close();
		}

		#endregion IDisposable Members

		private void DisposeInternal(bool disposing)
		{
			if (!isDisposed_)
			{
				isDisposed_ = true;
				entries_ = Empty.Array<ZipEntry>();

				if (IsStreamOwner && (baseStream_ != null))
				{
					lock (baseStream_)
					{
						baseStream_.Dispose();
					}
				}

				PostUpdateCleanup();
			}
		}

		/// <summary>
		/// Releases the unmanaged resources used by the this instance and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources;
		/// false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			DisposeInternal(disposing);
		}

		#endregion Disposing

		#region Internal routines

		#region Reading

		/// <summary>
		/// Read an unsigned short in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="EndOfStreamException">
		/// The stream ends prematurely
		/// </exception>
		private ushort ReadLEUshort()
		{
			int data1 = baseStream_.ReadByte();

			if (data1 < 0)
			{
				throw new EndOfStreamException("End of stream");
			}

			int data2 = baseStream_.ReadByte();

			if (data2 < 0)
			{
				throw new EndOfStreamException("End of stream");
			}

			return unchecked((ushort)((ushort)data1 | (ushort)(data2 << 8)));
		}

		/// <summary>
		/// Read a uint in little endian byte order.
		/// </summary>
		/// <returns>Returns the value read.</returns>
		/// <exception cref="IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The file ends prematurely
		/// </exception>
		private uint ReadLEUint()
		{
			return (uint)(ReadLEUshort() | (ReadLEUshort() << 16));
		}

		private ulong ReadLEUlong()
		{
			return ReadLEUint() | ((ulong)ReadLEUint() << 32);
		}

		#endregion Reading

		// NOTE this returns the offset of the first byte after the signature.
		private long LocateBlockWithSignature(int signature, long endLocation, int minimumBlockSize, int maximumVariableData)
			=> ZipFormat.LocateBlockWithSignature(baseStream_, signature, endLocation, minimumBlockSize, maximumVariableData);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public long GetCentralDirOffset()
		{
			// Search for the End Of Central Directory.  When a zip comment is
			// present the directory will start earlier
			//
			// The search is limited to 64K which is the maximum size of a trailing comment field to aid speed.
			// This should be compatible with both SFX and ZIP files but has only been tested for Zip files
			// If a SFX file has the Zip data attached as a resource and there are other resources occurring later then
			// this could be invalid.
			// Could also speed this up by reading memory in larger blocks.

			if (baseStream_.CanSeek == false)
			{
				throw new ZipException("ZipFile stream must be seekable");
			}

			long locatedEndOfCentralDir = LocateBlockWithSignature(ZipConstants.EndOfCentralDirectorySignature,
				baseStream_.Length, ZipConstants.EndOfCentralRecordBaseSize, 0xffff);

			if (locatedEndOfCentralDir < 0)
			{
				throw new ZipException("Cannot find central directory");
			}

			// Read end of central directory record
			ushort thisDiskNumber = ReadLEUshort();
			ushort startCentralDirDisk = ReadLEUshort();
			ulong entriesForThisDisk = ReadLEUshort();
			ulong entriesForWholeCentralDir = ReadLEUshort();
			ulong centralDirSize = ReadLEUint();
			long offsetOfCentralDir = ReadLEUint();
			uint commentSize = ReadLEUshort();

			if (commentSize > 0)
			{
				byte[] comment = new byte[commentSize];

				StreamUtils.ReadFully(baseStream_, comment);
				comment_ = _stringCodec.ZipArchiveCommentEncoding.GetString(comment);
			}
			else
			{
				comment_ = string.Empty;
			}

			bool isZip64 = false;
			bool requireZip64 = false;

			// Check if zip64 header information is required.
			if ((thisDiskNumber == 0xffff) ||
				(startCentralDirDisk == 0xffff) ||
				(entriesForThisDisk == 0xffff) ||
				(entriesForWholeCentralDir == 0xffff) ||
				(centralDirSize == 0xffffffff) ||
				(offsetOfCentralDir == 0xffffffff))
			{
				requireZip64 = true;
			}

			// #357 - always check for the existance of the Zip64 central directory.
			// #403 - Take account of the fixed size of the locator when searching.
			//    Subtract from locatedEndOfCentralDir so that the endLocation is the location of EndOfCentralDirectorySignature,
			//    rather than the data following the signature.
			long locatedZip64EndOfCentralDirLocator = LocateBlockWithSignature(
				ZipConstants.Zip64CentralDirLocatorSignature,
				locatedEndOfCentralDir - 4,
				ZipConstants.Zip64EndOfCentralDirectoryLocatorSize,
				0);

			if (locatedZip64EndOfCentralDirLocator < 0)
			{
				if (requireZip64)
				{
					// This is only an error in cases where the Zip64 directory is required.
					throw new ZipException("Cannot find Zip64 locator");
				}
			}
			else
			{
				isZip64 = true;

				// number of the disk with the start of the zip64 end of central directory 4 bytes
				// relative offset of the zip64 end of central directory record 8 bytes
				// total number of disks 4 bytes
				ReadLEUint(); // startDisk64 is not currently used
				ulong offset64 = ReadLEUlong();
				uint totalDisks = ReadLEUint();

				baseStream_.Position = (long)offset64;
				long sig64 = ReadLEUint();

				if (sig64 != ZipConstants.Zip64CentralFileHeaderSignature)
				{
					throw new ZipException(string.Format("Invalid Zip64 Central directory signature at {0:X}", offset64));
				}

				// NOTE: Record size = SizeOfFixedFields + SizeOfVariableData - 12.
				ulong recordSize = ReadLEUlong();
				int versionMadeBy = ReadLEUshort();
				int versionToExtract = ReadLEUshort();
				uint thisDisk = ReadLEUint();
				uint centralDirDisk = ReadLEUint();
				entriesForThisDisk = ReadLEUlong();
				entriesForWholeCentralDir = ReadLEUlong();
				centralDirSize = ReadLEUlong();
				offsetOfCentralDir = (long)ReadLEUlong();

				// NOTE: zip64 extensible data sector (variable size) is ignored.
			}

			if (!isZip64 && (offsetOfCentralDir < locatedEndOfCentralDir - (4 + (long)centralDirSize)))
			{
				offsetOfFirstEntry = locatedEndOfCentralDir - (4 + (long)centralDirSize + offsetOfCentralDir);
				if (offsetOfFirstEntry <= 0)
				{
					throw new ZipException("Invalid embedded zip archive");
				}
			}

			return offsetOfFirstEntry + offsetOfCentralDir;
		}

		/// <summary>
		/// Search for and read the central directory of a zip file filling the entries array.
		/// </summary>
		/// <exception cref="System.IO.IOException">
		/// An i/o error occurs.
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The central directory is malformed or cannot be found
		/// </exception>
		private void ReadEntries()
		{
			// Search for the End Of Central Directory.  When a zip comment is
			// present the directory will start earlier
			//
			// The search is limited to 64K which is the maximum size of a trailing comment field to aid speed.
			// This should be compatible with both SFX and ZIP files but has only been tested for Zip files
			// If a SFX file has the Zip data attached as a resource and there are other resources occurring later then
			// this could be invalid.
			// Could also speed this up by reading memory in larger blocks.

			if (baseStream_.CanSeek == false)
			{
				throw new ZipException("ZipFile stream must be seekable");
			}

			long locatedEndOfCentralDir = LocateBlockWithSignature(ZipConstants.EndOfCentralDirectorySignature,
				baseStream_.Length, ZipConstants.EndOfCentralRecordBaseSize, 0xffff);

			if (locatedEndOfCentralDir < 0)
			{
				throw new ZipException("Cannot find central directory");
			}

			// Read end of central directory record
			ushort thisDiskNumber = ReadLEUshort();
			ushort startCentralDirDisk = ReadLEUshort();
			ulong entriesForThisDisk = ReadLEUshort();
			ulong entriesForWholeCentralDir = ReadLEUshort();
			ulong centralDirSize = ReadLEUint();
			long offsetOfCentralDir = ReadLEUint();
			uint commentSize = ReadLEUshort();

			if (commentSize > 0)
			{
				byte[] comment = new byte[commentSize];

				StreamUtils.ReadFully(baseStream_, comment);
				comment_ = _stringCodec.ZipArchiveCommentEncoding.GetString(comment);
			}
			else
			{
				comment_ = string.Empty;
			}

			bool isZip64 = false;
			bool requireZip64 = false;

			// Check if zip64 header information is required.
			if ((thisDiskNumber == 0xffff) ||
				(startCentralDirDisk == 0xffff) ||
				(entriesForThisDisk == 0xffff) ||
				(entriesForWholeCentralDir == 0xffff) ||
				(centralDirSize == 0xffffffff) ||
				(offsetOfCentralDir == 0xffffffff))
			{
				requireZip64 = true;
			}

			// #357 - always check for the existance of the Zip64 central directory.
			// #403 - Take account of the fixed size of the locator when searching.
			//    Subtract from locatedEndOfCentralDir so that the endLocation is the location of EndOfCentralDirectorySignature,
			//    rather than the data following the signature.
			long locatedZip64EndOfCentralDirLocator = LocateBlockWithSignature(
				ZipConstants.Zip64CentralDirLocatorSignature,
				locatedEndOfCentralDir - 4,
				ZipConstants.Zip64EndOfCentralDirectoryLocatorSize,
				0);

			if (locatedZip64EndOfCentralDirLocator < 0)
			{
				if (requireZip64)
				{
					// This is only an error in cases where the Zip64 directory is required.
					throw new ZipException("Cannot find Zip64 locator");
				}
			}
			else
			{
				isZip64 = true;

				// number of the disk with the start of the zip64 end of central directory 4 bytes
				// relative offset of the zip64 end of central directory record 8 bytes
				// total number of disks 4 bytes
				ReadLEUint(); // startDisk64 is not currently used
				ulong offset64 = ReadLEUlong();
				uint totalDisks = ReadLEUint();

				baseStream_.Position = (long)offset64;
				long sig64 = ReadLEUint();

				if (sig64 != ZipConstants.Zip64CentralFileHeaderSignature)
				{
					throw new ZipException(string.Format("Invalid Zip64 Central directory signature at {0:X}", offset64));
				}

				// NOTE: Record size = SizeOfFixedFields + SizeOfVariableData - 12.
				ulong recordSize = ReadLEUlong();
				int versionMadeBy = ReadLEUshort();
				int versionToExtract = ReadLEUshort();
				uint thisDisk = ReadLEUint();
				uint centralDirDisk = ReadLEUint();
				entriesForThisDisk = ReadLEUlong();
				entriesForWholeCentralDir = ReadLEUlong();
				centralDirSize = ReadLEUlong();
				offsetOfCentralDir = (long)ReadLEUlong();

				// NOTE: zip64 extensible data sector (variable size) is ignored.
			}

			entries_ = new ZipEntry[entriesForThisDisk];

			// SFX/embedded support, find the offset of the first entry vis the start of the stream
			// This applies to Zip files that are appended to the end of an SFX stub.
			// Or are appended as a resource to an executable.
			// Zip files created by some archivers have the offsets altered to reflect the true offsets
			// and so dont require any adjustment here...
			// TODO: Difficulty with Zip64 and SFX offset handling needs resolution - maths?
			if (!isZip64 && (offsetOfCentralDir < locatedEndOfCentralDir - (4 + (long)centralDirSize)))
			{
				offsetOfFirstEntry = locatedEndOfCentralDir - (4 + (long)centralDirSize + offsetOfCentralDir);
				if (offsetOfFirstEntry <= 0)
				{
					throw new ZipException("Invalid embedded zip archive");
				}
			}

			baseStream_.Seek(offsetOfFirstEntry + offsetOfCentralDir, SeekOrigin.Begin);

			for (ulong i = 0; i < entriesForThisDisk; i++)
			{
				if (ReadLEUint() != ZipConstants.CentralHeaderSignature)
				{
					throw new ZipException("Wrong Central Directory signature");
				}

				int versionMadeBy = ReadLEUshort();
				int versionToExtract = ReadLEUshort();
				int bitFlags = ReadLEUshort();
				int method = ReadLEUshort();
				uint dostime = ReadLEUint();
				uint crc = ReadLEUint();
				var csize = (long)ReadLEUint();
				var size = (long)ReadLEUint();
				int nameLen = ReadLEUshort();
				int extraLen = ReadLEUshort();
				int commentLen = ReadLEUshort();

				int diskStartNo = ReadLEUshort();  // Not currently used
				int internalAttributes = ReadLEUshort();  // Not currently used

				uint externalAttributes = ReadLEUint();
				long offset = ReadLEUint();

				byte[] buffer = new byte[Math.Max(nameLen, commentLen)];
				var entryEncoding = _stringCodec.ZipInputEncoding(bitFlags);

				StreamUtils.ReadFully(baseStream_, buffer, 0, nameLen);
				string name = entryEncoding.GetString(buffer, 0, nameLen);
				var unicode = entryEncoding.IsZipUnicode();

				var entry = new ZipEntry(name, versionToExtract, versionMadeBy, (CompressionMethod)method, unicode)
				{
					Crc = crc & 0xffffffffL,
					Size = size & 0xffffffffL,
					CompressedSize = csize & 0xffffffffL,
					Flags = bitFlags,
					DosTime = dostime,
					ZipFileIndex = (long)i,
					Offset = offset,
					ExternalFileAttributes = (int)externalAttributes
				};

				if ((bitFlags & 8) == 0)
				{
					entry.CryptoCheckValue = (byte)(crc >> 24);
				}
				else
				{
					entry.CryptoCheckValue = (byte)((dostime >> 8) & 0xff);
				}

				if (extraLen > 0)
				{
					byte[] extra = new byte[extraLen];
					StreamUtils.ReadFully(baseStream_, extra);
					entry.ExtraData = extra;
				}

				entry.ProcessExtraData(false);

				if (commentLen > 0)
				{
					StreamUtils.ReadFully(baseStream_, buffer, 0, commentLen);
					entry.Comment = entryEncoding.GetString(buffer, 0, commentLen);
				}

				entries_[i] = entry;
			}
		}

		/// <summary>
		/// Locate the data for a given entry.
		/// </summary>
		/// <returns>
		/// The start offset of the data.
		/// </returns>
		/// <exception cref="System.IO.EndOfStreamException">
		/// The stream ends prematurely
		/// </exception>
		/// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
		/// The local header signature is invalid, the entry and central header file name lengths are different
		/// or the local and entry compression methods dont match
		/// </exception>
		private long LocateEntry(ZipEntry entry)
		{
			return TestLocalHeader(entry, HeaderTest.Extract);
		}

		private Stream CreateAndInitDecryptionStream(Stream baseStream, ZipEntry entry)
		{
			CryptoStream result = null;

			if (entry.CompressionMethodForHeader == CompressionMethod.WinZipAES)
			{
				if (entry.Version >= ZipConstants.VERSION_AES)
				{
					// Issue #471 - accept an empty string as a password, but reject null.
					OnKeysRequired(entry.Name);
					if (rawPassword_ == null)
					{
						throw new ZipException("No password available for AES encrypted stream");
					}
					int saltLen = entry.AESSaltLen;
					byte[] saltBytes = new byte[saltLen];
					int saltIn = StreamUtils.ReadRequestedBytes(baseStream, saltBytes, 0, saltLen);
					if (saltIn != saltLen)
						throw new ZipException("AES Salt expected " + saltLen + " got " + saltIn);
					//
					byte[] pwdVerifyRead = new byte[2];
					StreamUtils.ReadFully(baseStream, pwdVerifyRead);
					int blockSize = entry.AESKeySize / 8;   // bits to bytes

					var decryptor = new ZipAESTransform(rawPassword_, saltBytes, blockSize, false);
					byte[] pwdVerifyCalc = decryptor.PwdVerifier;
					if (pwdVerifyCalc[0] != pwdVerifyRead[0] || pwdVerifyCalc[1] != pwdVerifyRead[1])
						throw new ZipException("Invalid password for AES");
					result = new ZipAESStream(baseStream, decryptor, CryptoStreamMode.Read);
				}
				else
				{
					throw new ZipException("Decryption method not supported");
				}
			}
			else
			{
				if ((entry.Version < ZipConstants.VersionStrongEncryption)
					|| (entry.Flags & (int)GeneralBitFlags.StrongEncryption) == 0)
				{
					var classicManaged = new PkzipClassicManaged();

					OnKeysRequired(entry.Name);
					if (HaveKeys == false)
					{
						throw new ZipException("No password available for encrypted stream");
					}

					result = new CryptoStream(baseStream, classicManaged.CreateDecryptor(key, null), CryptoStreamMode.Read);
					CheckClassicPassword(result, entry);
				}
				else
				{
					// We don't support PKWare strong encryption
					throw new ZipException("Decryption method not supported");
				}
			}

			return result;
		}

		private Stream CreateAndInitEncryptionStream(Stream baseStream, ZipEntry entry)
		{
			CryptoStream result = null;
			if ((entry.Version < ZipConstants.VersionStrongEncryption)
				|| (entry.Flags & (int)GeneralBitFlags.StrongEncryption) == 0)
			{
				var classicManaged = new PkzipClassicManaged();

				OnKeysRequired(entry.Name);
				if (HaveKeys == false)
				{
					throw new ZipException("No password available for encrypted stream");
				}

				// Closing a CryptoStream will close the base stream as well so wrap it in an UncompressedStream
				// which doesnt do this.
				result = new CryptoStream(new UncompressedStream(baseStream),
					classicManaged.CreateEncryptor(key, null), CryptoStreamMode.Write);

				if ((entry.Crc < 0) || (entry.Flags & 8) != 0)
				{
					WriteEncryptionHeader(result, entry.DosTime << 16);
				}
				else
				{
					WriteEncryptionHeader(result, entry.Crc);
				}
			}
			return result;
		}

		private static void CheckClassicPassword(CryptoStream classicCryptoStream, ZipEntry entry)
		{
			byte[] cryptbuffer = new byte[ZipConstants.CryptoHeaderSize];
			StreamUtils.ReadFully(classicCryptoStream, cryptbuffer);
			if (cryptbuffer[ZipConstants.CryptoHeaderSize - 1] != entry.CryptoCheckValue)
			{
				throw new ZipException("Invalid password");
			}
		}

		private static void WriteEncryptionHeader(Stream stream, long crcValue)
		{
			byte[] cryptBuffer = new byte[ZipConstants.CryptoHeaderSize];
			using (var rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(cryptBuffer);
			}
			cryptBuffer[11] = (byte)(crcValue >> 24);
			stream.Write(cryptBuffer, 0, cryptBuffer.Length);
		}

		#endregion Internal routines

		#region Instance Fields

		private bool isDisposed_;
		private string name_;
		private string comment_ = string.Empty;
		private string rawPassword_;
		private Stream baseStream_;
		private bool isStreamOwner;
		private long offsetOfFirstEntry;
		private ZipEntry[] entries_;
		private byte[] key;
		private bool isNewArchive_;
		private StringCodec _stringCodec = ZipStrings.GetStringCodec();

		// Default is dynamic which is not backwards compatible and can cause problems
		// with XP's built in compression which cant read Zip64 archives.
		// However it does avoid the situation were a large file is added and cannot be completed correctly.
		// Hint: Set always ZipEntry size before they are added to an archive and this setting isnt needed.
		private UseZip64 useZip64_ = UseZip64.Dynamic;

		#region Zip Update Instance Fields

		private List<ZipUpdate> updates_;
		private long updateCount_; // Count is managed manually as updates_ can contain nulls!
		private Dictionary<string, int> updateIndex_;
		private IArchiveStorage archiveStorage_;
		private IDynamicDataSource updateDataSource_;
		private bool contentsEdited_;
		private int bufferSize_ = DefaultBufferSize;
		private byte[] copyBuffer_;
		private ZipString newComment_;
		private bool commentEdited_;
		private IEntryFactory updateEntryFactory_ = new ZipEntryFactory();

		#endregion Zip Update Instance Fields

		#endregion Instance Fields

		#region Support Classes

		/// <summary>
		/// Represents a string from a <see cref="ZipFile"/> which is stored as an array of bytes.
		/// </summary>
		private class ZipString
		{
			#region Constructors

			/// <summary>
			/// Initialise a <see cref="ZipString"/> with a string.
			/// </summary>
			/// <param name="comment">The textual string form.</param>
			/// <param name="encoding"></param>
			public ZipString(string comment, Encoding encoding)
			{
				comment_ = comment;
				isSourceString_ = true;
				_encoding = encoding;
			}

			/// <summary>
			/// Initialise a <see cref="ZipString"/> using a string in its binary 'raw' form.
			/// </summary>
			/// <param name="rawString"></param>
			/// <param name="encoding"></param>
			public ZipString(byte[] rawString, Encoding encoding)
			{
				rawComment_ = rawString;
				_encoding = encoding;
			}

			#endregion Constructors

			/// <summary>
			/// Get a value indicating the original source of data for this instance.
			/// True if the source was a string; false if the source was binary data.
			/// </summary>
			public bool IsSourceString => isSourceString_;

			/// <summary>
			/// Get the length of the comment when represented as raw bytes.
			/// </summary>
			public int RawLength
			{
				get
				{
					MakeBytesAvailable();
					return rawComment_.Length;
				}
			}

			/// <summary>
			/// Get the comment in its 'raw' form as plain bytes.
			/// </summary>
			public byte[] RawComment
			{
				get
				{
					MakeBytesAvailable();
					return (byte[])rawComment_.Clone();
				}
			}

			/// <summary>
			/// Reset the comment to its initial state.
			/// </summary>
			public void Reset()
			{
				if (isSourceString_)
				{
					rawComment_ = null;
				}
				else
				{
					comment_ = null;
				}
			}

			private void MakeTextAvailable()
			{
				if (comment_ == null)
				{
					comment_ = _encoding.GetString(rawComment_);
				}
			}

			private void MakeBytesAvailable()
			{
				if (rawComment_ == null)
				{
					rawComment_ = _encoding.GetBytes(comment_);
				}
			}

			/// <summary>
			/// Implicit conversion of comment to a string.
			/// </summary>
			/// <param name="zipString">The <see cref="ZipString"/> to convert to a string.</param>
			/// <returns>The textual equivalent for the input value.</returns>
			public static implicit operator string(ZipString zipString)
			{
				zipString.MakeTextAvailable();
				return zipString.comment_;
			}

			#region Instance Fields

			private string comment_;
			private byte[] rawComment_;
			private readonly bool isSourceString_;
			private readonly Encoding _encoding;

			#endregion Instance Fields
		}

		/// <summary>
		/// An <see cref="IEnumerator">enumerator</see> for <see cref="ZipEntry">Zip entries</see>
		/// </summary>
		private class ZipEntryEnumerator : IEnumerator
		{
			#region Constructors

			public ZipEntryEnumerator(ZipEntry[] entries)
			{
				array = entries;
			}

			#endregion Constructors

			#region IEnumerator Members

			public object Current
			{
				get
				{
					return array[index];
				}
			}

			public void Reset()
			{
				index = -1;
			}

			public bool MoveNext()
			{
				return (++index < array.Length);
			}

			#endregion IEnumerator Members

			#region Instance Fields

			private ZipEntry[] array;
			private int index = -1;

			#endregion Instance Fields
		}

		/// <summary>
		/// An <see cref="UncompressedStream"/> is a stream that you can write uncompressed data
		/// to and flush, but cannot read, seek or do anything else to.
		/// </summary>
		private class UncompressedStream : Stream
		{
			#region Constructors

			public UncompressedStream(Stream baseStream)
			{
				baseStream_ = baseStream;
			}

			#endregion Constructors

			/// <summary>
			/// Gets a value indicating whether the current stream supports reading.
			/// </summary>
			public override bool CanRead
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Write any buffered data to underlying storage.
			/// </summary>
			public override void Flush()
			{
				baseStream_.Flush();
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports writing.
			/// </summary>
			public override bool CanWrite
			{
				get
				{
					return baseStream_.CanWrite;
				}
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports seeking.
			/// </summary>
			public override bool CanSeek
			{
				get
				{
					return false;
				}
			}

			/// <summary>
			/// Get the length in bytes of the stream.
			/// </summary>
			public override long Length
			{
				get
				{
					return 0;
				}
			}

			/// <summary>
			/// Gets or sets the position within the current stream.
			/// </summary>
			public override long Position
			{
				get
				{
					return baseStream_.Position;
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			/// <summary>
			/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
			/// </summary>
			/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
			/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
			/// <returns>
			/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
			/// </returns>
			/// <exception cref="System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading. </exception>
			/// <exception cref="System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override int Read(byte[] buffer, int offset, int count)
			{
				return 0;
			}

			/// <summary>
			/// Sets the position within the current stream.
			/// </summary>
			/// <param name="offset">A byte offset relative to the origin parameter.</param>
			/// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
			/// <returns>
			/// The new position within the current stream.
			/// </returns>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Seek(long offset, SeekOrigin origin)
			{
				return 0;
			}

			/// <summary>
			/// Sets the length of the current stream.
			/// </summary>
			/// <param name="value">The desired length of the current stream in bytes.</param>
			/// <exception cref="System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override void SetLength(long value)
			{
			}

			/// <summary>
			/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
			/// </summary>
			/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
			/// <param name="count">The number of bytes to be written to the current stream.</param>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support writing. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
			/// <exception cref="System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override void Write(byte[] buffer, int offset, int count)
			{
				baseStream_.Write(buffer, offset, count);
			}

			private readonly

			#region Instance Fields

			Stream baseStream_;

			#endregion Instance Fields
		}

		/// <summary>
		/// A <see cref="PartialInputStream"/> is an <see cref="InflaterInputStream"/>
		/// whose data is only a part or subsection of a file.
		/// </summary>
		private class PartialInputStream : Stream
		{
			#region Constructors

			/// <summary>
			/// Initialise a new instance of the <see cref="PartialInputStream"/> class.
			/// </summary>
			/// <param name="zipFile">The <see cref="ZipFile"/> containing the underlying stream to use for IO.</param>
			/// <param name="start">The start of the partial data.</param>
			/// <param name="length">The length of the partial data.</param>
			public PartialInputStream(ZipFile zipFile, long start, long length)
			{
				start_ = start;
				length_ = length;

				// Although this is the only time the zipfile is used
				// keeping a reference here prevents premature closure of
				// this zip file and thus the baseStream_.

				// Code like this will cause apparently random failures depending
				// on the size of the files and when garbage is collected.
				//
				// ZipFile z = new ZipFile (stream);
				// Stream reader = z.GetInputStream(0);
				// uses reader here....
				zipFile_ = zipFile;
				baseStream_ = zipFile_.baseStream_;
				readPos_ = start;
				end_ = start + length;
			}

			#endregion Constructors

			/// <summary>
			/// Read a byte from this stream.
			/// </summary>
			/// <returns>Returns the byte read or -1 on end of stream.</returns>
			public override int ReadByte()
			{
				if (readPos_ >= end_)
				{
					// -1 is the correct value at end of stream.
					return -1;
				}

				lock (baseStream_)
				{
					baseStream_.Seek(readPos_++, SeekOrigin.Begin);
					return baseStream_.ReadByte();
				}
			}

			/// <summary>
			/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
			/// </summary>
			/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
			/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
			/// <returns>
			/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
			/// </returns>
			/// <exception cref="System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support reading. </exception>
			/// <exception cref="System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override int Read(byte[] buffer, int offset, int count)
			{
				lock (baseStream_)
				{
					if (count > end_ - readPos_)
					{
						count = (int)(end_ - readPos_);
						if (count == 0)
						{
							return 0;
						}
					}
					// Protect against Stream implementations that throw away their buffer on every Seek
					// (for example, Mono FileStream)
					if (baseStream_.Position != readPos_)
					{
						baseStream_.Seek(readPos_, SeekOrigin.Begin);
					}
					int readCount = baseStream_.Read(buffer, offset, count);
					if (readCount > 0)
					{
						readPos_ += readCount;
					}
					return readCount;
				}
			}

			/// <summary>
			/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
			/// </summary>
			/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
			/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
			/// <param name="count">The number of bytes to be written to the current stream.</param>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support writing. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			/// <exception cref="System.ArgumentNullException">buffer is null. </exception>
			/// <exception cref="System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
			/// <exception cref="System.ArgumentOutOfRangeException">offset or count is negative. </exception>
			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// When overridden in a derived class, sets the length of the current stream.
			/// </summary>
			/// <param name="value">The desired length of the current stream in bytes.</param>
			/// <exception cref="System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// When overridden in a derived class, sets the position within the current stream.
			/// </summary>
			/// <param name="offset">A byte offset relative to the origin parameter.</param>
			/// <param name="origin">A value of type <see cref="System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
			/// <returns>
			/// The new position within the current stream.
			/// </returns>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Seek(long offset, SeekOrigin origin)
			{
				long newPos = readPos_;

				switch (origin)
				{
					case SeekOrigin.Begin:
						newPos = start_ + offset;
						break;

					case SeekOrigin.Current:
						newPos = readPos_ + offset;
						break;

					case SeekOrigin.End:
						newPos = end_ + offset;
						break;
				}

				if (newPos < start_)
				{
					throw new ArgumentException("Negative position is invalid");
				}

				if (newPos > end_)
				{
					throw new IOException("Cannot seek past end");
				}
				readPos_ = newPos;
				return readPos_;
			}

			/// <summary>
			/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
			/// </summary>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			public override void Flush()
			{
				// Nothing to do.
			}

			/// <summary>
			/// Gets or sets the position within the current stream.
			/// </summary>
			/// <value></value>
			/// <returns>The current position within the stream.</returns>
			/// <exception cref="System.IO.IOException">An I/O error occurs. </exception>
			/// <exception cref="System.NotSupportedException">The stream does not support seeking. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Position
			{
				get { return readPos_ - start_; }
				set
				{
					long newPos = start_ + value;

					if (newPos < start_)
					{
						throw new ArgumentException("Negative position is invalid");
					}

					if (newPos > end_)
					{
						throw new InvalidOperationException("Cannot seek past end");
					}
					readPos_ = newPos;
				}
			}

			/// <summary>
			/// Gets the length in bytes of the stream.
			/// </summary>
			/// <value></value>
			/// <returns>A long value representing the length of the stream in bytes.</returns>
			/// <exception cref="System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
			/// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
			public override long Length
			{
				get { return length_; }
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports writing.
			/// </summary>
			/// <value>false</value>
			/// <returns>true if the stream supports writing; otherwise, false.</returns>
			public override bool CanWrite
			{
				get { return false; }
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports seeking.
			/// </summary>
			/// <value>true</value>
			/// <returns>true if the stream supports seeking; otherwise, false.</returns>
			public override bool CanSeek
			{
				get { return true; }
			}

			/// <summary>
			/// Gets a value indicating whether the current stream supports reading.
			/// </summary>
			/// <value>true.</value>
			/// <returns>true if the stream supports reading; otherwise, false.</returns>
			public override bool CanRead
			{
				get { return true; }
			}

			/// <summary>
			/// Gets a value that determines whether the current stream can time out.
			/// </summary>
			/// <value></value>
			/// <returns>A value that determines whether the current stream can time out.</returns>
			public override bool CanTimeout
			{
				get { return baseStream_.CanTimeout; }
			}

			#region Instance Fields

			private ZipFile zipFile_;
			private Stream baseStream_;
			private readonly long start_;
			private readonly long length_;
			private long readPos_;
			private readonly long end_;

			#endregion Instance Fields
		}

		#endregion Support Classes
	}

	#endregion ZipFile Class

	#region DataSources

	/// <summary>
	/// Provides a static way to obtain a source of data for an entry.
	/// </summary>
	public interface IStaticDataSource
	{
		/// <summary>
		/// Get a source of data by creating a new stream.
		/// </summary>
		/// <returns>Returns a <see cref="Stream"/> to use for compression input.</returns>
		/// <remarks>Ideally a new stream is created and opened to achieve this, to avoid locking problems.</remarks>
		Stream GetSource();
	}

	/// <summary>
	/// Represents a source of data that can dynamically provide
	/// multiple <see cref="Stream">data sources</see> based on the parameters passed.
	/// </summary>
	public interface IDynamicDataSource
	{
		/// <summary>
		/// Get a data source.
		/// </summary>
		/// <param name="entry">The <see cref="ZipEntry"/> to get a source for.</param>
		/// <param name="name">The name for data if known.</param>
		/// <returns>Returns a <see cref="Stream"/> to use for compression input.</returns>
		/// <remarks>Ideally a new stream is created and opened to achieve this, to avoid locking problems.</remarks>
		Stream GetSource(ZipEntry entry, string name);
	}

	/// <summary>
	/// Default implementation of a <see cref="IStaticDataSource"/> for use with files stored on disk.
	/// </summary>
	public class StaticDiskDataSource : IStaticDataSource
	{
		/// <summary>
		/// Initialise a new instance of <see cref="StaticDiskDataSource"/>
		/// </summary>
		/// <param name="fileName">The name of the file to obtain data from.</param>
		public StaticDiskDataSource(string fileName)
		{
			fileName_ = fileName;
		}

		#region IDataSource Members

		/// <summary>
		/// Get a <see cref="Stream"/> providing data.
		/// </summary>
		/// <returns>Returns a <see cref="Stream"/> providing data.</returns>
		public Stream GetSource()
		{
			return File.Open(fileName_, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		private readonly

		#endregion IDataSource Members

		#region Instance Fields

		string fileName_;

		#endregion Instance Fields
	}

	/// <summary>
	/// Default implementation of <see cref="IDynamicDataSource"/> for files stored on disk.
	/// </summary>
	public class DynamicDiskDataSource : IDynamicDataSource
	{
		#region IDataSource Members

		/// <summary>
		/// Get a <see cref="Stream"/> providing data for an entry.
		/// </summary>
		/// <param name="entry">The entry to provide data for.</param>
		/// <param name="name">The file name for data if known.</param>
		/// <returns>Returns a stream providing data; or null if not available</returns>
		public Stream GetSource(ZipEntry entry, string name)
		{
			Stream result = null;

			if (name != null)
			{
				result = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			return result;
		}

		#endregion IDataSource Members
	}

	#endregion DataSources

	#region Archive Storage

	/// <summary>
	/// Defines facilities for data storage when updating Zip Archives.
	/// </summary>
	public interface IArchiveStorage
	{
		/// <summary>
		/// Get the <see cref="FileUpdateMode"/> to apply during updates.
		/// </summary>
		FileUpdateMode UpdateMode { get; }

		/// <summary>
		/// Get an empty <see cref="Stream"/> that can be used for temporary output.
		/// </summary>
		/// <returns>Returns a temporary output <see cref="Stream"/></returns>
		/// <seealso cref="ConvertTemporaryToFinal"></seealso>
		Stream GetTemporaryOutput();

		/// <summary>
		/// Convert a temporary output stream to a final stream.
		/// </summary>
		/// <returns>The resulting final <see cref="Stream"/></returns>
		/// <seealso cref="GetTemporaryOutput"/>
		Stream ConvertTemporaryToFinal();

		/// <summary>
		/// Make a temporary copy of the original stream.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to copy.</param>
		/// <returns>Returns a temporary output <see cref="Stream"/> that is a copy of the input.</returns>
		Stream MakeTemporaryCopy(Stream stream);

		/// <summary>
		/// Return a stream suitable for performing direct updates on the original source.
		/// </summary>
		/// <param name="stream">The current stream.</param>
		/// <returns>Returns a stream suitable for direct updating.</returns>
		/// <remarks>This may be the current stream passed.</remarks>
		Stream OpenForDirectUpdate(Stream stream);

		/// <summary>
		/// Dispose of this instance.
		/// </summary>
		void Dispose();
	}

	/// <summary>
	/// An abstract <see cref="IArchiveStorage"/> suitable for extension by inheritance.
	/// </summary>
	abstract public class BaseArchiveStorage : IArchiveStorage
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseArchiveStorage"/> class.
		/// </summary>
		/// <param name="updateMode">The update mode.</param>
		protected BaseArchiveStorage(FileUpdateMode updateMode)
		{
			updateMode_ = updateMode;
		}

		#endregion Constructors

		#region IArchiveStorage Members

		/// <summary>
		/// Gets a temporary output <see cref="Stream"/>
		/// </summary>
		/// <returns>Returns the temporary output stream.</returns>
		/// <seealso cref="ConvertTemporaryToFinal"></seealso>
		public abstract Stream GetTemporaryOutput();

		/// <summary>
		/// Converts the temporary <see cref="Stream"/> to its final form.
		/// </summary>
		/// <returns>Returns a <see cref="Stream"/> that can be used to read
		/// the final storage for the archive.</returns>
		/// <seealso cref="GetTemporaryOutput"/>
		public abstract Stream ConvertTemporaryToFinal();

		/// <summary>
		/// Make a temporary copy of a <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to make a copy of.</param>
		/// <returns>Returns a temporary output <see cref="Stream"/> that is a copy of the input.</returns>
		public abstract Stream MakeTemporaryCopy(Stream stream);

		/// <summary>
		/// Return a stream suitable for performing direct updates on the original source.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to open for direct update.</param>
		/// <returns>Returns a stream suitable for direct updating.</returns>
		public abstract Stream OpenForDirectUpdate(Stream stream);

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public abstract void Dispose();

		/// <summary>
		/// Gets the update mode applicable.
		/// </summary>
		/// <value>The update mode.</value>
		public FileUpdateMode UpdateMode
		{
			get
			{
				return updateMode_;
			}
		}

		#endregion IArchiveStorage Members

		#region Instance Fields

		private readonly FileUpdateMode updateMode_;

		#endregion Instance Fields
	}

	/// <summary>
	/// An <see cref="IArchiveStorage"/> implementation suitable for hard disks.
	/// </summary>
	public class DiskArchiveStorage : BaseArchiveStorage
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DiskArchiveStorage"/> class.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <param name="updateMode">The update mode.</param>
		public DiskArchiveStorage(ZipFile file, FileUpdateMode updateMode)
			: base(updateMode)
		{
			if (file.Name == null)
			{
				throw new ZipException("Cant handle non file archives");
			}

			fileName_ = file.Name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DiskArchiveStorage"/> class.
		/// </summary>
		/// <param name="file">The file.</param>
		public DiskArchiveStorage(ZipFile file)
			: this(file, FileUpdateMode.Safe)
		{
		}

		#endregion Constructors

		#region IArchiveStorage Members

		/// <summary>
		/// Gets a temporary output <see cref="Stream"/> for performing updates on.
		/// </summary>
		/// <returns>Returns the temporary output stream.</returns>
		public override Stream GetTemporaryOutput()
		{
			temporaryName_ = PathUtils.GetTempFileName(temporaryName_);
			temporaryStream_ = File.Open(temporaryName_, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

			return temporaryStream_;
		}

		/// <summary>
		/// Converts a temporary <see cref="Stream"/> to its final form.
		/// </summary>
		/// <returns>Returns a <see cref="Stream"/> that can be used to read
		/// the final storage for the archive.</returns>
		public override Stream ConvertTemporaryToFinal()
		{
			if (temporaryStream_ == null)
			{
				throw new ZipException("No temporary stream has been created");
			}

			Stream result = null;

			string moveTempName = PathUtils.GetTempFileName(fileName_);
			bool newFileCreated = false;

			try
			{
				temporaryStream_.Dispose();
				File.Move(fileName_, moveTempName);
				File.Move(temporaryName_, fileName_);
				newFileCreated = true;
				File.Delete(moveTempName);

				result = File.Open(fileName_, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch (Exception)
			{
				result = null;

				// Try to roll back changes...
				if (!newFileCreated)
				{
					File.Move(moveTempName, fileName_);
					File.Delete(temporaryName_);
				}

				throw;
			}

			return result;
		}

		/// <summary>
		/// Make a temporary copy of a stream.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to copy.</param>
		/// <returns>Returns a temporary output <see cref="Stream"/> that is a copy of the input.</returns>
		public override Stream MakeTemporaryCopy(Stream stream)
		{
			stream.Dispose();

			temporaryName_ = PathUtils.GetTempFileName(fileName_);
			File.Copy(fileName_, temporaryName_, true);

			temporaryStream_ = new FileStream(temporaryName_,
				FileMode.Open,
				FileAccess.ReadWrite);
			return temporaryStream_;
		}

		/// <summary>
		/// Return a stream suitable for performing direct updates on the original source.
		/// </summary>
		/// <param name="stream">The current stream.</param>
		/// <returns>Returns a stream suitable for direct updating.</returns>
		/// <remarks>If the <paramref name="stream"/> is not null this is used as is.</remarks>
		public override Stream OpenForDirectUpdate(Stream stream)
		{
			Stream result;
			if ((stream == null) || !stream.CanWrite)
			{
				if (stream != null)
				{
					stream.Dispose();
				}

				result = new FileStream(fileName_,
						FileMode.Open,
						FileAccess.ReadWrite);
			}
			else
			{
				result = stream;
			}

			return result;
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public override void Dispose()
		{
			if (temporaryStream_ != null)
			{
				temporaryStream_.Dispose();
			}
		}

		#endregion IArchiveStorage Members

		#region Instance Fields

		private Stream temporaryStream_;
		private readonly string fileName_;
		private string temporaryName_;

		#endregion Instance Fields
	}

	/// <summary>
	/// An <see cref="IArchiveStorage"/> implementation suitable for in memory streams.
	/// </summary>
	public class MemoryArchiveStorage : BaseArchiveStorage
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryArchiveStorage"/> class.
		/// </summary>
		public MemoryArchiveStorage()
			: base(FileUpdateMode.Direct)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MemoryArchiveStorage"/> class.
		/// </summary>
		/// <param name="updateMode">The <see cref="FileUpdateMode"/> to use</param>
		/// <remarks>This constructor is for testing as memory streams dont really require safe mode.</remarks>
		public MemoryArchiveStorage(FileUpdateMode updateMode)
			: base(updateMode)
		{
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Get the stream returned by <see cref="ConvertTemporaryToFinal"/> if this was in fact called.
		/// </summary>
		public MemoryStream FinalStream
		{
			get { return finalStream_; }
		}

		#endregion Properties

		#region IArchiveStorage Members

		/// <summary>
		/// Gets the temporary output <see cref="Stream"/>
		/// </summary>
		/// <returns>Returns the temporary output stream.</returns>
		public override Stream GetTemporaryOutput()
		{
			temporaryStream_ = new MemoryStream();
			return temporaryStream_;
		}

		/// <summary>
		/// Converts the temporary <see cref="Stream"/> to its final form.
		/// </summary>
		/// <returns>Returns a <see cref="Stream"/> that can be used to read
		/// the final storage for the archive.</returns>
		public override Stream ConvertTemporaryToFinal()
		{
			if (temporaryStream_ == null)
			{
				throw new ZipException("No temporary stream has been created");
			}

			finalStream_ = new MemoryStream(temporaryStream_.ToArray());
			return finalStream_;
		}

		/// <summary>
		/// Make a temporary copy of the original stream.
		/// </summary>
		/// <param name="stream">The <see cref="Stream"/> to copy.</param>
		/// <returns>Returns a temporary output <see cref="Stream"/> that is a copy of the input.</returns>
		public override Stream MakeTemporaryCopy(Stream stream)
		{
			temporaryStream_ = new MemoryStream();
			stream.Position = 0;
			StreamUtils.Copy(stream, temporaryStream_, new byte[4096]);
			return temporaryStream_;
		}

		/// <summary>
		/// Return a stream suitable for performing direct updates on the original source.
		/// </summary>
		/// <param name="stream">The original source stream</param>
		/// <returns>Returns a stream suitable for direct updating.</returns>
		/// <remarks>If the <paramref name="stream"/> passed is not null this is used;
		/// otherwise a new <see cref="MemoryStream"/> is returned.</remarks>
		public override Stream OpenForDirectUpdate(Stream stream)
		{
			Stream result;
			if ((stream == null) || !stream.CanWrite)
			{
				result = new MemoryStream();

				if (stream != null)
				{
					stream.Position = 0;
					StreamUtils.Copy(stream, result, new byte[4096]);

					stream.Dispose();
				}
			}
			else
			{
				result = stream;
			}

			return result;
		}

		/// <summary>
		/// Disposes this instance.
		/// </summary>
		public override void Dispose()
		{
			if (temporaryStream_ != null)
			{
				temporaryStream_.Dispose();
			}
		}

		#endregion IArchiveStorage Members

		#region Instance Fields

		private MemoryStream temporaryStream_;
		private MemoryStream finalStream_;

		#endregion Instance Fields
	}

	#endregion Archive Storage
}
