using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.IO;
using static ICSharpCode.SharpZipLib.Zip.Compression.Deflater;
using static ICSharpCode.SharpZipLib.Zip.ZipEntryFactory;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// FastZipEvents supports all events applicable to <see cref="FastZip">FastZip</see> operations.
	/// </summary>
	public class FastZipEvents
	{
		/// <summary>
		/// Delegate to invoke when processing directories.
		/// </summary>
		public event EventHandler<DirectoryEventArgs> ProcessDirectory;

		/// <summary>
		/// Delegate to invoke when processing files.
		/// </summary>
		public ProcessFileHandler ProcessFile;

		/// <summary>
		/// Delegate to invoke during processing of files.
		/// </summary>
		public ProgressHandler Progress;

		/// <summary>
		/// Delegate to invoke when processing for a file has been completed.
		/// </summary>
		public CompletedFileHandler CompletedFile;

		/// <summary>
		/// Delegate to invoke when processing directory failures.
		/// </summary>
		public DirectoryFailureHandler DirectoryFailure;

		/// <summary>
		/// Delegate to invoke when processing file failures.
		/// </summary>
		public FileFailureHandler FileFailure;

		/// <summary>
		/// Raise the <see cref="DirectoryFailure">directory failure</see> event.
		/// </summary>
		/// <param name="directory">The directory causing the failure.</param>
		/// <param name="e">The exception for this event.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnDirectoryFailure(string directory, Exception e)
		{
			bool result = false;
			DirectoryFailureHandler handler = DirectoryFailure;

			if (handler != null)
			{
				var args = new ScanFailureEventArgs(directory, e);
				handler(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}

		/// <summary>
		/// Fires the <see cref="FileFailure"> file failure handler delegate</see>.
		/// </summary>
		/// <param name="file">The file causing the failure.</param>
		/// <param name="e">The exception for this failure.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnFileFailure(string file, Exception e)
		{
			FileFailureHandler handler = FileFailure;
			bool result = (handler != null);

			if (result)
			{
				var args = new ScanFailureEventArgs(file, e);
				handler(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}

		/// <summary>
		/// Fires the <see cref="ProcessFile">ProcessFile delegate</see>.
		/// </summary>
		/// <param name="file">The file being processed.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnProcessFile(string file)
		{
			bool result = true;
			ProcessFileHandler handler = ProcessFile;

			if (handler != null)
			{
				var args = new ScanEventArgs(file);
				handler(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}

		/// <summary>
		/// Fires the <see cref="CompletedFile"/> delegate
		/// </summary>
		/// <param name="file">The file whose processing has been completed.</param>
		/// <returns>A boolean indicating if execution should continue or not.</returns>
		public bool OnCompletedFile(string file)
		{
			bool result = true;
			CompletedFileHandler handler = CompletedFile;
			if (handler != null)
			{
				var args = new ScanEventArgs(file);
				handler(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}

		/// <summary>
		/// Fires the <see cref="ProcessDirectory">process directory</see> delegate.
		/// </summary>
		/// <param name="directory">The directory being processed.</param>
		/// <param name="hasMatchingFiles">Flag indicating if the directory has matching files as determined by the current filter.</param>
		/// <returns>A <see cref="bool"/> of true if the operation should continue; false otherwise.</returns>
		public bool OnProcessDirectory(string directory, bool hasMatchingFiles)
		{
			bool result = true;
			EventHandler<DirectoryEventArgs> handler = ProcessDirectory;
			if (handler != null)
			{
				var args = new DirectoryEventArgs(directory, hasMatchingFiles);
				handler(this, args);
				result = args.ContinueRunning;
			}
			return result;
		}

		/// <summary>
		/// The minimum timespan between <see cref="Progress"/> events.
		/// </summary>
		/// <value>The minimum period of time between <see cref="Progress"/> events.</value>
		/// <seealso cref="Progress"/>
		/// <remarks>The default interval is three seconds.</remarks>
		public TimeSpan ProgressInterval
		{
			get { return progressInterval_; }
			set { progressInterval_ = value; }
		}

		#region Instance Fields

		private TimeSpan progressInterval_ = TimeSpan.FromSeconds(3);

		#endregion Instance Fields
	}

	/// <summary>
	/// FastZip provides facilities for creating and extracting zip files.
	/// </summary>
	public class FastZip
	{
		#region Enumerations

		/// <summary>
		/// Defines the desired handling when overwriting files during extraction.
		/// </summary>
		public enum Overwrite
		{
			/// <summary>
			/// Prompt the user to confirm overwriting
			/// </summary>
			Prompt,

			/// <summary>
			/// Never overwrite files.
			/// </summary>
			Never,

			/// <summary>
			/// Always overwrite files.
			/// </summary>
			Always
		}

		#endregion Enumerations

		#region Constructors

		/// <summary>
		/// Initialise a default instance of <see cref="FastZip"/>.
		/// </summary>
		public FastZip()
		{
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FastZip"/> using the specified <see cref="TimeSetting"/>
		/// </summary>
		/// <param name="timeSetting">The <see cref="TimeSetting">time setting</see> to use when creating or extracting <see cref="ZipEntry">Zip entries</see>.</param>
		/// <remarks>Using <see cref="TimeSetting.LastAccessTime">TimeSetting.LastAccessTime</see><see cref="TimeSetting.LastAccessTimeUtc">[Utc]</see> when
		/// creating an archive will set the file time to the moment of reading.
		/// </remarks>
		public FastZip(TimeSetting timeSetting)
		{
			entryFactory_ = new ZipEntryFactory(timeSetting);
			restoreDateTimeOnExtract_ = true;
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FastZip"/> using the specified <see cref="DateTime"/>
		/// </summary>
		/// <param name="time">The time to set all <see cref="ZipEntry.DateTime"/> values for created or extracted <see cref="ZipEntry">Zip Entries</see>.</param>
		public FastZip(DateTime time)
		{
			entryFactory_ = new ZipEntryFactory(time);
			restoreDateTimeOnExtract_ = true;
		}

		/// <summary>
		/// Initialise a new instance of <see cref="FastZip"/>
		/// </summary>
		/// <param name="events">The <see cref="FastZipEvents">events</see> to use during operations.</param>
		public FastZip(FastZipEvents events)
		{
			events_ = events;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Get/set a value indicating whether empty directories should be created.
		/// </summary>
		public bool CreateEmptyDirectories
		{
			get { return createEmptyDirectories_; }
			set { createEmptyDirectories_ = value; }
		}

		/// <summary>
		/// Get / set the password value.
		/// </summary>
		public string Password
		{
			get { return password_; }
			set { password_ = value; }
		}

		/// <summary>
		/// Get / set the method of encrypting entries.
		/// </summary>
		/// <remarks>
		/// Only applies when <see cref="Password"/> is set.
		/// Defaults to ZipCrypto for backwards compatibility purposes.
		/// </remarks>
		public ZipEncryptionMethod EntryEncryptionMethod { get; set; } = ZipEncryptionMethod.ZipCrypto;

		/// <summary>
		/// Get or set the <see cref="INameTransform"></see> active when creating Zip files.
		/// </summary>
		/// <seealso cref="EntryFactory"></seealso>
		public INameTransform NameTransform
		{
			get { return entryFactory_.NameTransform; }
			set
			{
				entryFactory_.NameTransform = value;
			}
		}

		/// <summary>
		/// Get or set the <see cref="IEntryFactory"></see> active when creating Zip files.
		/// </summary>
		public IEntryFactory EntryFactory
		{
			get { return entryFactory_; }
			set
			{
				if (value == null)
				{
					entryFactory_ = new ZipEntryFactory();
				}
				else
				{
					entryFactory_ = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the setting for <see cref="UseZip64">Zip64 handling when writing.</see>
		/// </summary>
		/// <remarks>
		/// The default value is dynamic which is not backwards compatible with old
		/// programs and can cause problems with XP's built in compression which cant
		/// read Zip64 archives. However it does avoid the situation were a large file
		/// is added and cannot be completed correctly.
		/// NOTE: Setting the size for entries before they are added is the best solution!
		/// By default the EntryFactory used by FastZip will set the file size.
		/// </remarks>
		public UseZip64 UseZip64
		{
			get { return useZip64_; }
			set { useZip64_ = value; }
		}

		/// <summary>
		/// Get/set a value indicating whether file dates and times should
		/// be restored when extracting files from an archive.
		/// </summary>
		/// <remarks>The default value is false.</remarks>
		public bool RestoreDateTimeOnExtract
		{
			get
			{
				return restoreDateTimeOnExtract_;
			}
			set
			{
				restoreDateTimeOnExtract_ = value;
			}
		}

		/// <summary>
		/// Get/set a value indicating whether file attributes should
		/// be restored during extract operations
		/// </summary>
		public bool RestoreAttributesOnExtract
		{
			get { return restoreAttributesOnExtract_; }
			set { restoreAttributesOnExtract_ = value; }
		}

		/// <summary>
		/// Get/set the Compression Level that will be used
		/// when creating the zip
		/// </summary>
		public Deflater.CompressionLevel CompressionLevel
		{
			get { return compressionLevel_; }
			set { compressionLevel_ = value; }
		}

		/// <summary>
		/// Reflects the opposite of the internal <see cref="StringCodec.ForceZipLegacyEncoding"/>, setting it to <c>false</c> overrides the encoding used for reading and writing zip entries
		/// </summary>
		public bool UseUnicode
		{
			get => !_stringCodec.ForceZipLegacyEncoding;
			set => _stringCodec.ForceZipLegacyEncoding = !value;
		}

		/// <summary> Gets or sets the code page used for reading/writing zip file entries when unicode is disabled </summary>
		public int LegacyCodePage
		{
			get => _stringCodec.CodePage;
			set => _stringCodec.CodePage = value;
		}
		
		/// <inheritdoc cref="StringCodec"/>
		public StringCodec StringCodec
		{
			get => _stringCodec;
			set => _stringCodec = value;
		}

		#endregion Properties

		#region Delegates

		/// <summary>
		/// Delegate called when confirming overwriting of files.
		/// </summary>
		public delegate bool ConfirmOverwriteDelegate(string fileName);

		#endregion Delegates

		#region CreateZip

		/// <summary>
		/// Create a zip file.
		/// </summary>
		/// <param name="zipFileName">The name of the zip file to create.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
		public void CreateZip(string zipFileName, string sourceDirectory,
			bool recurse, string fileFilter, string directoryFilter)
		{
			CreateZip(File.Create(zipFileName), sourceDirectory, recurse, fileFilter, directoryFilter);
		}

		/// <summary>
		/// Create a zip file/archive.
		/// </summary>
		/// <param name="zipFileName">The name of the zip file to create.</param>
		/// <param name="sourceDirectory">The directory to obtain files and directories from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The file filter to apply.</param>
		public void CreateZip(string zipFileName, string sourceDirectory, bool recurse, string fileFilter)
		{
			CreateZip(File.Create(zipFileName), sourceDirectory, recurse, fileFilter, null);
		}

		/// <summary>
		/// Create a zip archive sending output to the <paramref name="outputStream"/> passed.
		/// </summary>
		/// <param name="outputStream">The stream to write archive data to.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
		/// <remarks>The <paramref name="outputStream"/> is closed after creation.</remarks>
		public void CreateZip(Stream outputStream, string sourceDirectory, bool recurse, string fileFilter, string directoryFilter)
		{
			CreateZip(outputStream, sourceDirectory, recurse, fileFilter, directoryFilter, false);
		}

		/// <summary>
		/// Create a zip archive sending output to the <paramref name="outputStream"/> passed.
		/// </summary>
		/// <param name="outputStream">The stream to write archive data to.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="PathFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="PathFilter">directory filter</see> to apply.</param>
		/// <param name="leaveOpen">true to leave <paramref name="outputStream"/> open after the zip has been created, false to dispose it.</param>
		public void CreateZip(Stream outputStream, string sourceDirectory, bool recurse, string fileFilter, string directoryFilter, bool leaveOpen)
		{
			var scanner = new FileSystemScanner(fileFilter, directoryFilter);
			CreateZip(outputStream, sourceDirectory, recurse, scanner, leaveOpen);
		}

		/// <summary>
		/// Create a zip file.
		/// </summary>
		/// <param name="zipFileName">The name of the zip file to create.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="IScanFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="IScanFilter">directory filter</see> to apply.</param>
		public void CreateZip(string zipFileName, string sourceDirectory,
			bool recurse, IScanFilter fileFilter, IScanFilter directoryFilter)
		{
			CreateZip(File.Create(zipFileName), sourceDirectory, recurse, fileFilter, directoryFilter, false);
		}

		/// <summary>
		/// Create a zip archive sending output to the <paramref name="outputStream"/> passed.
		/// </summary>
		/// <param name="outputStream">The stream to write archive data to.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="fileFilter">The <see cref="IScanFilter">file filter</see> to apply.</param>
		/// <param name="directoryFilter">The <see cref="IScanFilter">directory filter</see> to apply.</param>
		/// <param name="leaveOpen">true to leave <paramref name="outputStream"/> open after the zip has been created, false to dispose it.</param>
		public void CreateZip(Stream outputStream, string sourceDirectory, bool recurse, IScanFilter fileFilter, IScanFilter directoryFilter, bool leaveOpen = false)
		{
			var scanner = new FileSystemScanner(fileFilter, directoryFilter);
			CreateZip(outputStream, sourceDirectory, recurse, scanner, leaveOpen);
		}

		/// <summary>
		/// Create a zip archive sending output to the <paramref name="outputStream"/> passed.
		/// </summary>
		/// <param name="outputStream">The stream to write archive data to.</param>
		/// <param name="sourceDirectory">The directory to source files from.</param>
		/// <param name="recurse">True to recurse directories, false for no recursion.</param>
		/// <param name="scanner">For performing the actual file system scan</param>
		/// <param name="leaveOpen">true to leave <paramref name="outputStream"/> open after the zip has been created, false to dispose it.</param>
		/// <remarks>The <paramref name="outputStream"/> is closed after creation.</remarks>
		private void CreateZip(Stream outputStream, string sourceDirectory, bool recurse, FileSystemScanner scanner, bool leaveOpen)
		{
			NameTransform = new ZipNameTransform(sourceDirectory);
			sourceDirectory_ = sourceDirectory;

			using (outputStream_ = new ZipOutputStream(outputStream, _stringCodec))
			{
				outputStream_.SetLevel((int)CompressionLevel);
				outputStream_.IsStreamOwner = !leaveOpen;
				outputStream_.NameTransform = null; // all required transforms handled by us

				if (false == string.IsNullOrEmpty(password_) && EntryEncryptionMethod != ZipEncryptionMethod.None)
				{
					outputStream_.Password = password_;
				}

				outputStream_.UseZip64 = UseZip64;
				scanner.ProcessFile += ProcessFile;
				if (this.CreateEmptyDirectories)
				{
					scanner.ProcessDirectory += ProcessDirectory;
				}

				if (events_ != null)
				{
					if (events_.FileFailure != null)
					{
						scanner.FileFailure += events_.FileFailure;
					}

					if (events_.DirectoryFailure != null)
					{
						scanner.DirectoryFailure += events_.DirectoryFailure;
					}
				}

				scanner.Scan(sourceDirectory, recurse);
			}
		}

		#endregion CreateZip

		#region ExtractZip

		/// <summary>
		/// Extract the contents of a zip file.
		/// </summary>
		/// <param name="zipFileName">The zip file to extract from.</param>
		/// <param name="targetDirectory">The directory to save extracted information in.</param>
		/// <param name="fileFilter">A filter to apply to files.</param>
		public void ExtractZip(string zipFileName, string targetDirectory, string fileFilter)
		{
			ExtractZip(zipFileName, targetDirectory, Overwrite.Always, null, fileFilter, null, restoreDateTimeOnExtract_);
		}

		/// <summary>
		/// Extract the contents of a zip file.
		/// </summary>
		/// <param name="zipFileName">The zip file to extract from.</param>
		/// <param name="targetDirectory">The directory to save extracted information in.</param>
		/// <param name="overwrite">The style of <see cref="Overwrite">overwriting</see> to apply.</param>
		/// <param name="confirmDelegate">A delegate to invoke when confirming overwriting.</param>
		/// <param name="fileFilter">A filter to apply to files.</param>
		/// <param name="directoryFilter">A filter to apply to directories.</param>
		/// <param name="restoreDateTime">Flag indicating whether to restore the date and time for extracted files.</param>
		/// <param name="allowParentTraversal">Allow parent directory traversal in file paths (e.g. ../file)</param>
		public void ExtractZip(string zipFileName, string targetDirectory,
							   Overwrite overwrite, ConfirmOverwriteDelegate confirmDelegate,
							   string fileFilter, string directoryFilter, bool restoreDateTime, bool allowParentTraversal = false)
		{
			Stream inputStream = File.Open(zipFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			ExtractZip(inputStream, targetDirectory, overwrite, confirmDelegate, fileFilter, directoryFilter, restoreDateTime, true, allowParentTraversal);
		}

		/// <summary>
		/// Extract the contents of a zip file held in a stream.
		/// </summary>
		/// <param name="inputStream">The seekable input stream containing the zip to extract from.</param>
		/// <param name="targetDirectory">The directory to save extracted information in.</param>
		/// <param name="overwrite">The style of <see cref="Overwrite">overwriting</see> to apply.</param>
		/// <param name="confirmDelegate">A delegate to invoke when confirming overwriting.</param>
		/// <param name="fileFilter">A filter to apply to files.</param>
		/// <param name="directoryFilter">A filter to apply to directories.</param>
		/// <param name="restoreDateTime">Flag indicating whether to restore the date and time for extracted files.</param>
		/// <param name="isStreamOwner">Flag indicating whether the inputStream will be closed by this method.</param>
		/// <param name="allowParentTraversal">Allow parent directory traversal in file paths (e.g. ../file)</param>
		public void ExtractZip(Stream inputStream, string targetDirectory,
					   Overwrite overwrite, ConfirmOverwriteDelegate confirmDelegate,
					   string fileFilter, string directoryFilter, bool restoreDateTime,
					   bool isStreamOwner, bool allowParentTraversal = false)
		{
			if ((overwrite == Overwrite.Prompt) && (confirmDelegate == null))
			{
				throw new ArgumentNullException(nameof(confirmDelegate));
			}

			continueRunning_ = true;
			overwrite_ = overwrite;
			confirmDelegate_ = confirmDelegate;
			extractNameTransform_ = new WindowsNameTransform(targetDirectory, allowParentTraversal);

			fileFilter_ = new NameFilter(fileFilter);
			directoryFilter_ = new NameFilter(directoryFilter);
			restoreDateTimeOnExtract_ = restoreDateTime;

			using (zipFile_ = new ZipFile(inputStream, !isStreamOwner))
			{
				if (password_ != null)
				{
					zipFile_.Password = password_;
				}

				System.Collections.IEnumerator enumerator = zipFile_.GetEnumerator();
				while (continueRunning_ && enumerator.MoveNext())
				{
					var entry = (ZipEntry)enumerator.Current;
					if (entry.IsFile)
					{
						// TODO Path.GetDirectory can fail here on invalid characters.
						if (directoryFilter_.IsMatch(Path.GetDirectoryName(entry.Name)) && fileFilter_.IsMatch(entry.Name))
						{
							ExtractEntry(entry);
						}
					}
					else if (entry.IsDirectory)
					{
						if (directoryFilter_.IsMatch(entry.Name) && CreateEmptyDirectories)
						{
							ExtractEntry(entry);
						}
					}
					else
					{
						// Do nothing for volume labels etc...
					}
				}
			}
		}

		#endregion ExtractZip

		#region Internal Processing

		private void ProcessDirectory(object sender, DirectoryEventArgs e)
		{
			if (!e.HasMatchingFiles && CreateEmptyDirectories)
			{
				if (events_ != null)
				{
					events_.OnProcessDirectory(e.Name, e.HasMatchingFiles);
				}

				if (e.ContinueRunning)
				{
					if (e.Name != sourceDirectory_)
					{
						ZipEntry entry = entryFactory_.MakeDirectoryEntry(e.Name);
						outputStream_.PutNextEntry(entry);
					}
				}
			}
		}

		private void ProcessFile(object sender, ScanEventArgs e)
		{
			if ((events_ != null) && (events_.ProcessFile != null))
			{
				events_.ProcessFile(sender, e);
			}

			if (e.ContinueRunning)
			{
				try
				{
					// The open below is equivalent to OpenRead which guarantees that if opened the
					// file will not be changed by subsequent openers, but precludes opening in some cases
					// were it could succeed. ie the open may fail as its already open for writing and the share mode should reflect that.
					using (FileStream stream = File.Open(e.Name, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						ZipEntry entry = entryFactory_.MakeFileEntry(e.Name);
						if (_stringCodec.ForceZipLegacyEncoding)
						{
							entry.IsUnicodeText = false;
						}

						// Set up AES encryption for the entry if required.
						ConfigureEntryEncryption(entry);

						outputStream_.PutNextEntry(entry);
						AddFileContents(e.Name, stream);
					}
				}
				catch (Exception ex)
				{
					if (events_ != null)
					{
						continueRunning_ = events_.OnFileFailure(e.Name, ex);
					}
					else
					{
						continueRunning_ = false;
						throw;
					}
				}
			}
		}

		// Set up the encryption method to use for the specific entry.
		private void ConfigureEntryEncryption(ZipEntry entry)
		{
			// Only alter the entries options if AES isn't already enabled for it
			// (it might have been set up by the entry factory, and if so we let that take precedence)
			if (!string.IsNullOrEmpty(Password) && entry.AESEncryptionStrength == 0)
			{
				switch (EntryEncryptionMethod)
				{
					case ZipEncryptionMethod.AES128:
						entry.AESKeySize = 128;
						break;

					case ZipEncryptionMethod.AES256:
						entry.AESKeySize = 256;
						break;
				}
			}
		}

		private void AddFileContents(string name, Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (buffer_ == null)
			{
				buffer_ = new byte[4096];
			}

			if ((events_ != null) && (events_.Progress != null))
			{
				StreamUtils.Copy(stream, outputStream_, buffer_,
					events_.Progress, events_.ProgressInterval, this, name);
			}
			else
			{
				StreamUtils.Copy(stream, outputStream_, buffer_);
			}

			if (events_ != null)
			{
				continueRunning_ = events_.OnCompletedFile(name);
			}
		}

		private void ExtractFileEntry(ZipEntry entry, string targetName)
		{
			bool proceed = true;
			if (overwrite_ != Overwrite.Always)
			{
				if (File.Exists(targetName))
				{
					if ((overwrite_ == Overwrite.Prompt) && (confirmDelegate_ != null))
					{
						proceed = confirmDelegate_(targetName);
					}
					else
					{
						proceed = false;
					}
				}
			}

			if (proceed)
			{
				if (events_ != null)
				{
					continueRunning_ = events_.OnProcessFile(entry.Name);
				}

				if (continueRunning_)
				{
					try
					{
						using (FileStream outputStream = File.Create(targetName))
						{
							if (buffer_ == null)
							{
								buffer_ = new byte[4096];
							}

							using (var inputStream = zipFile_.GetInputStream(entry))
							{
								if ((events_ != null) && (events_.Progress != null))
								{
									StreamUtils.Copy(inputStream, outputStream, buffer_,
										events_.Progress, events_.ProgressInterval, this, entry.Name, entry.Size);
								}
								else
								{
									StreamUtils.Copy(inputStream, outputStream, buffer_);
								}
							}

							if (events_ != null)
							{
								continueRunning_ = events_.OnCompletedFile(entry.Name);
							}
						}

						if (restoreDateTimeOnExtract_)
						{
							switch (entryFactory_.Setting)
							{
								case TimeSetting.CreateTime:
									File.SetCreationTime(targetName, entry.DateTime);
									break;

								case TimeSetting.CreateTimeUtc:
									File.SetCreationTimeUtc(targetName, entry.DateTime);
									break;

								case TimeSetting.LastAccessTime:
									File.SetLastAccessTime(targetName, entry.DateTime);
									break;

								case TimeSetting.LastAccessTimeUtc:
									File.SetLastAccessTimeUtc(targetName, entry.DateTime);
									break;

								case TimeSetting.LastWriteTime:
									File.SetLastWriteTime(targetName, entry.DateTime);
									break;

								case TimeSetting.LastWriteTimeUtc:
									File.SetLastWriteTimeUtc(targetName, entry.DateTime);
									break;

								case TimeSetting.Fixed:
									File.SetLastWriteTime(targetName, entryFactory_.FixedDateTime);
									break;

								default:
									throw new ZipException("Unhandled time setting in ExtractFileEntry");
							}
						}

						if (RestoreAttributesOnExtract && entry.IsDOSEntry && (entry.ExternalFileAttributes != -1))
						{
							var fileAttributes = (FileAttributes)entry.ExternalFileAttributes;
							// TODO: FastZip - Setting of other file attributes on extraction is a little trickier.
							fileAttributes &= (FileAttributes.Archive | FileAttributes.Normal | FileAttributes.ReadOnly | FileAttributes.Hidden);
							File.SetAttributes(targetName, fileAttributes);
						}
					}
					catch (Exception ex)
					{
						if (events_ != null)
						{
							continueRunning_ = events_.OnFileFailure(targetName, ex);
						}
						else
						{
							continueRunning_ = false;
							throw;
						}
					}
				}
			}
		}

		private void ExtractEntry(ZipEntry entry)
		{
			bool doExtraction = entry.IsCompressionMethodSupported();
			string targetName = entry.Name;

			if (doExtraction)
			{
				if (entry.IsFile)
				{
					targetName = extractNameTransform_.TransformFile(targetName);
				}
				else if (entry.IsDirectory)
				{
					targetName = extractNameTransform_.TransformDirectory(targetName);
				}

				doExtraction = !(string.IsNullOrEmpty(targetName));
			}

			// TODO: Fire delegate/throw exception were compression method not supported, or name is invalid?

			string dirName = string.Empty;

			if (doExtraction)
			{
				if (entry.IsDirectory)
				{
					dirName = targetName;
				}
				else
				{
					dirName = Path.GetDirectoryName(Path.GetFullPath(targetName));
				}
			}

			if (doExtraction && !Directory.Exists(dirName))
			{
				if (!entry.IsDirectory || CreateEmptyDirectories)
				{
					try
					{
						continueRunning_ = events_?.OnProcessDirectory(dirName, true) ?? true;
						if (continueRunning_)
						{
							Directory.CreateDirectory(dirName);
							if (entry.IsDirectory && restoreDateTimeOnExtract_)
							{
								switch (entryFactory_.Setting)
								{
									case TimeSetting.CreateTime:
										Directory.SetCreationTime(dirName, entry.DateTime);
										break;

									case TimeSetting.CreateTimeUtc:
										Directory.SetCreationTimeUtc(dirName, entry.DateTime);
										break;

									case TimeSetting.LastAccessTime:
										Directory.SetLastAccessTime(dirName, entry.DateTime);
										break;

									case TimeSetting.LastAccessTimeUtc:
										Directory.SetLastAccessTimeUtc(dirName, entry.DateTime);
										break;

									case TimeSetting.LastWriteTime:
										Directory.SetLastWriteTime(dirName, entry.DateTime);
										break;

									case TimeSetting.LastWriteTimeUtc:
										Directory.SetLastWriteTimeUtc(dirName, entry.DateTime);
										break;

									case TimeSetting.Fixed:
										Directory.SetLastWriteTime(dirName, entryFactory_.FixedDateTime);
										break;

									default:
										throw new ZipException("Unhandled time setting in ExtractEntry");
								}
							}
						}
						else
						{
							doExtraction = false;
						}
					}
					catch (Exception ex)
					{
						doExtraction = false;
						if (events_ != null)
						{
							if (entry.IsDirectory)
							{
								continueRunning_ = events_.OnDirectoryFailure(targetName, ex);
							}
							else
							{
								continueRunning_ = events_.OnFileFailure(targetName, ex);
							}
						}
						else
						{
							continueRunning_ = false;
							throw;
						}
					}
				}
			}

			if (doExtraction && entry.IsFile)
			{
				ExtractFileEntry(entry, targetName);
			}
		}

		private static int MakeExternalAttributes(FileInfo info)
		{
			return (int)info.Attributes;
		}

		private static bool NameIsValid(string name)
		{
			return !string.IsNullOrEmpty(name) &&
				(name.IndexOfAny(Path.GetInvalidPathChars()) < 0);
		}

		#endregion Internal Processing

		#region Instance Fields

		private bool continueRunning_;
		private byte[] buffer_;
		private ZipOutputStream outputStream_;
		private ZipFile zipFile_;
		private string sourceDirectory_;
		private NameFilter fileFilter_;
		private NameFilter directoryFilter_;
		private Overwrite overwrite_;
		private ConfirmOverwriteDelegate confirmDelegate_;

		private bool restoreDateTimeOnExtract_;
		private bool restoreAttributesOnExtract_;
		private bool createEmptyDirectories_;
		private FastZipEvents events_;
		private IEntryFactory entryFactory_ = new ZipEntryFactory();
		private INameTransform extractNameTransform_;
		private UseZip64 useZip64_ = UseZip64.Dynamic;
		private CompressionLevel compressionLevel_ = CompressionLevel.DEFAULT_COMPRESSION;
		private StringCodec _stringCodec = new StringCodec();

		private string password_;

		#endregion Instance Fields
	}
}
