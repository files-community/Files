using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Encryption;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// This is a DeflaterOutputStream that writes the files into a zip
	/// archive one after another.  It has a special method to start a new
	/// zip entry.  The zip entries contains information about the file name
	/// size, compressed size, CRC, etc.
	///
	/// It includes support for Stored and Deflated entries.
	/// This class is not thread safe.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	/// <example> This sample shows how to create a zip file
	/// <code>
	/// using System;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.Core;
	/// using ICSharpCode.SharpZipLib.Zip;
	///
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	/// 		string[] filenames = Directory.GetFiles(args[0]);
	/// 		byte[] buffer = new byte[4096];
	///
	/// 		using ( ZipOutputStream s = new ZipOutputStream(File.Create(args[1])) ) {
	///
	/// 			s.SetLevel(9); // 0 - store only to 9 - means best compression
	///
	/// 			foreach (string file in filenames) {
	/// 				ZipEntry entry = new ZipEntry(file);
	/// 				s.PutNextEntry(entry);
	///
	/// 				using (FileStream fs = File.OpenRead(file)) {
	///						StreamUtils.Copy(fs, s, buffer);
	/// 				}
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class ZipOutputStream : DeflaterOutputStream
	{
		#region Constructors

		/// <summary>
		/// Creates a new Zip output stream, writing a zip archive.
		/// </summary>
		/// <param name="baseOutputStream">
		/// The output stream to which the archive contents are written.
		/// </param>
		public ZipOutputStream(Stream baseOutputStream)
			: base(baseOutputStream, new Deflater(Deflater.DEFAULT_COMPRESSION, true))
		{
		}

		/// <summary>
		/// Creates a new Zip output stream, writing a zip archive.
		/// </summary>
		/// <param name="baseOutputStream">The output stream to which the archive contents are written.</param>
		/// <param name="bufferSize">Size of the buffer to use.</param>
		public ZipOutputStream(Stream baseOutputStream, int bufferSize)
			: base(baseOutputStream, new Deflater(Deflater.DEFAULT_COMPRESSION, true), bufferSize)
		{
		}

		internal ZipOutputStream(Stream baseOutputStream, StringCodec stringCodec) : this(baseOutputStream)
		{
			_stringCodec = stringCodec;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseOutputStream"></param>
		/// <param name="existing"></param>
		public ZipOutputStream(Stream baseOutputStream, bool existing)
			: base(baseOutputStream, new Deflater(Deflater.DEFAULT_COMPRESSION, true))
		{
			if (existing)
			{
				using (var zipFile = new ZipFile(baseOutputStream))
				{
					zipFile.IsStreamOwner = false;
					entries = zipFile.OfType<ZipEntry>().ToList();
					offset = zipFile.GetCentralDirOffset();
					baseOutputStream_.Seek(offset, SeekOrigin.Begin);
				}
			}
		}

		#endregion Constructors

		/// <summary>
		/// Gets a flag value of true if the central header has been added for this archive; false if it has not been added.
		/// </summary>
		/// <remarks>No further entries can be added once this has been done.</remarks>
		public bool IsFinished
		{
			get
			{
				return entries == null;
			}
		}

		/// <summary>
		/// Set the zip file comment.
		/// </summary>
		/// <param name="comment">
		/// The comment text for the entire archive.
		/// </param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The converted comment is longer than 0xffff bytes.
		/// </exception>
		public void SetComment(string comment)
		{
			byte[] commentBytes = _stringCodec.ZipArchiveCommentEncoding.GetBytes(comment);
			if (commentBytes.Length > 0xffff)
			{
				throw new ArgumentOutOfRangeException(nameof(comment));
			}
			zipComment = commentBytes;
		}

		/// <summary>
		/// Sets the compression level.  The new level will be activated
		/// immediately.
		/// </summary>
		/// <param name="level">The new compression level (1 to 9).</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Level specified is not supported.
		/// </exception>
		/// <see cref="ICSharpCode.SharpZipLib.Zip.Compression.Deflater"/>
		public void SetLevel(int level)
		{
			deflater_.SetLevel(level);
			defaultCompressionLevel = level;
		}

		/// <summary>
		/// Get the current deflater compression level
		/// </summary>
		/// <returns>The current compression level</returns>
		public int GetLevel()
		{
			return deflater_.GetLevel();
		}

		/// <summary>
		/// Get / set a value indicating how Zip64 Extension usage is determined when adding entries.
		/// </summary>
		/// <remarks>Older archivers may not understand Zip64 extensions.
		/// If backwards compatability is an issue be careful when adding <see cref="ZipEntry.Size">entries</see> to an archive.
		/// Setting this property to off is workable but less desirable as in those circumstances adding a file
		/// larger then 4GB will fail.</remarks>
		public UseZip64 UseZip64
		{
			get { return useZip64_; }
			set { useZip64_ = value; }
		}

		/// <summary>
		/// Used for transforming the names of entries added by <see cref="PutNextEntry(ZipEntry)"/>.
		/// Defaults to <see cref="PathTransformer"/>, set to null to disable transforms and use names as supplied.
		/// </summary>
		public INameTransform NameTransform { get; set; } = new PathTransformer();

		/// <summary>
		/// Get/set the password used for encryption.
		/// </summary>
		/// <remarks>When set to null or if the password is empty no encryption is performed</remarks>
		public string Password
		{
			get
			{
				return password;
			}
			set
			{
				if ((value != null) && (value.Length == 0))
				{
					password = null;
				}
				else
				{
					password = value;
				}
			}
		}

		/// <summary>
		/// Write an unsigned short in little endian byte order.
		/// </summary>
		private void WriteLeShort(int value)
		{
			unchecked
			{
				baseOutputStream_.WriteByte((byte)(value & 0xff));
				baseOutputStream_.WriteByte((byte)((value >> 8) & 0xff));
			}
		}

		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		private void WriteLeInt(int value)
		{
			unchecked
			{
				WriteLeShort(value);
				WriteLeShort(value >> 16);
			}
		}

		/// <summary>
		/// Write an int in little endian byte order.
		/// </summary>
		private void WriteLeLong(long value)
		{
			unchecked
			{
				WriteLeInt((int)value);
				WriteLeInt((int)(value >> 32));
			}
		}

		// Apply any configured transforms/cleaning to the name of the supplied entry.
		private void TransformEntryName(ZipEntry entry)
		{
			if (NameTransform == null) return;
			entry.Name = entry.IsDirectory
				? NameTransform.TransformDirectory(entry.Name)
				: NameTransform.TransformFile(entry.Name);
		}

		/// <summary>
		/// Starts a new Zip entry. It automatically closes the previous
		/// entry if present.
		/// All entry elements bar name are optional, but must be correct if present.
		/// If the compression method is stored and the output is not patchable
		/// the compression for that entry is automatically changed to deflate level 0
		/// </summary>
		/// <param name="entry">
		/// the entry.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		/// if entry passed is null.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// if an I/O error occurred.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// if stream was finished
		/// </exception>
		/// <exception cref="ZipException">
		/// Too many entries in the Zip file<br/>
		/// Entry name is too long<br/>
		/// Finish has already been called<br/>
		/// </exception>
		/// <exception cref="System.NotImplementedException">
		/// The Compression method specified for the entry is unsupported.
		/// </exception>
		public void PutNextEntry(ZipEntry entry)
		{
			if (curEntry != null)
			{
				CloseEntry();
			}

			PutNextEntry(baseOutputStream_, entry);

			if (entry.IsCrypted)
			{
				WriteOutput(GetEntryEncryptionHeader(entry));
			}
		}

		/// <summary>
		/// Starts a new passthrough Zip entry. It automatically closes the previous
		/// entry if present.
		/// Passthrough entry is an entry that is created from compressed data. 
		/// It is useful to avoid recompression to save CPU resources if compressed data is already disposable.
		/// All entry elements bar name, crc, size and compressed size are optional, but must be correct if present.
		/// Compression should be set to Deflated.
		/// </summary>
		/// <param name="entry">
		/// the entry.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		/// if entry passed is null.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// if an I/O error occurred.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// if stream was finished.
		/// </exception>
		/// <exception cref="ZipException">
		/// Crc is not set<br/>
		/// Size is not set<br/>
		/// CompressedSize is not set<br/>
		/// CompressionMethod is not Deflate<br/>
		/// Too many entries in the Zip file<br/>
		/// Entry name is too long<br/>
		/// Finish has already been called<br/>
		/// </exception>
		/// <exception cref="System.NotImplementedException">
		/// The Compression method specified for the entry is unsupported<br/>
		/// Entry is encrypted<br/>
		/// </exception>
		public void PutNextPassthroughEntry(ZipEntry entry)
		{
			if (curEntry != null)
			{
				CloseEntry();
			}

			if (entry.Crc < 0)
			{
				throw new ZipException("Crc must be set for passthrough entry");
			}

			if (entry.Size < 0)
			{
				throw new ZipException("Size must be set for passthrough entry");
			}

			if (entry.CompressedSize < 0)
			{
				throw new ZipException("CompressedSize must be set for passthrough entry");
			}

			if (entry.CompressionMethod != CompressionMethod.Deflated)
			{
				throw new NotImplementedException("Only Deflated entries are supported for passthrough");
			}

			if (!string.IsNullOrEmpty(Password))
			{
				throw new NotImplementedException("Encrypted passthrough entries are not supported");
			}

			PutNextEntry(baseOutputStream_, entry, 0, true);
		}


		private void WriteOutput(byte[] bytes)
			=> baseOutputStream_.Write(bytes, 0, bytes.Length);

		private Task WriteOutputAsync(byte[] bytes)
			=> baseOutputStream_.WriteAsync(bytes, 0, bytes.Length);

		private byte[] GetEntryEncryptionHeader(ZipEntry entry) =>
			entry.AESKeySize > 0
				? InitializeAESPassword(entry, Password)
				: CreateZipCryptoHeader(entry.Crc < 0 ? entry.DosTime << 16 : entry.Crc);

		internal void PutNextEntry(Stream stream, ZipEntry entry, long streamOffset = 0, bool passthroughEntry = false)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			if (entries == null)
			{
				throw new InvalidOperationException("ZipOutputStream was finished");
			}

			if (entries.Count == int.MaxValue)
			{
				throw new ZipException("Too many entries for Zip file");
			}

			CompressionMethod method = entry.CompressionMethod;

			// Check that the compression is one that we support
			if (method != CompressionMethod.Deflated && method != CompressionMethod.Stored)
			{
				throw new NotImplementedException("Compression method not supported");
			}

			// A password must have been set in order to add AES encrypted entries
			if (entry.AESKeySize > 0 && string.IsNullOrEmpty(this.Password))
			{
				throw new InvalidOperationException("The Password property must be set before AES encrypted entries can be added");
			}

			entryIsPassthrough = passthroughEntry;

			int compressionLevel = defaultCompressionLevel;

			// Clear flags that the library manages internally
			entry.Flags &= (int)GeneralBitFlags.UnicodeText;
			patchEntryHeader = false;

			bool headerInfoAvailable;

			// No need to compress - definitely no data.
			if (entry.Size == 0 && !entryIsPassthrough)
			{
				entry.CompressedSize = entry.Size;
				entry.Crc = 0;
				method = CompressionMethod.Stored;
				headerInfoAvailable = true;
			}
			else
			{
				headerInfoAvailable = (entry.Size >= 0) && entry.HasCrc && entry.CompressedSize >= 0;

				// Switch to deflation if storing isnt possible.
				if (method == CompressionMethod.Stored)
				{
					if (!headerInfoAvailable)
					{
						if (!CanPatchEntries)
						{
							// Can't patch entries so storing is not possible.
							method = CompressionMethod.Deflated;
							compressionLevel = 0;
						}
					}
					else // entry.size must be > 0
					{
						entry.CompressedSize = entry.Size;
						headerInfoAvailable = entry.HasCrc;
					}
				}
			}

			if (headerInfoAvailable == false)
			{
				if (CanPatchEntries == false)
				{
					// Only way to record size and compressed size is to append a data descriptor
					// after compressed data.

					// Stored entries of this form have already been converted to deflating.
					entry.Flags |= 8;
				}
				else
				{
					patchEntryHeader = true;
				}
			}

			if (Password != null)
			{
				entry.IsCrypted = true;
				if (entry.Crc < 0)
				{
					// Need to append a data descriptor as the crc isnt available for use
					// with encryption, the date is used instead.  Setting the flag
					// indicates this to the decompressor.
					entry.Flags |= 8;
				}
			}

			entry.Offset = offset;
			entry.CompressionMethod = (CompressionMethod)method;

			curMethod = method;

			if ((useZip64_ == UseZip64.On) || ((entry.Size < 0) && (useZip64_ == UseZip64.Dynamic)))
			{
				entry.ForceZip64();
			}

			// Apply any required transforms to the entry name
			TransformEntryName(entry);

			// Write the local file header
			offset += ZipFormat.WriteLocalHeader(stream, entry, out var entryPatchData,
				headerInfoAvailable, patchEntryHeader, streamOffset, _stringCodec);

			patchData = entryPatchData;

			// Fix offsetOfCentraldir for AES
			if (entry.AESKeySize > 0)
				offset += entry.AESOverheadSize;

			// Activate the entry.
			curEntry = entry;
			size = 0;

			if (entryIsPassthrough)
				return;

			crc.Reset();
			if (method == CompressionMethod.Deflated)
			{
				deflater_.Reset();
				deflater_.SetLevel(compressionLevel);
			}
		}

		/// <summary>
		/// Starts a new Zip entry. It automatically closes the previous
		/// entry if present.
		/// All entry elements bar name are optional, but must be correct if present.
		/// If the compression method is stored and the output is not patchable
		/// the compression for that entry is automatically changed to deflate level 0
		/// </summary>
		/// <param name="entry">
		/// the entry.
		/// </param>
		/// <param name="ct">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
		/// <exception cref="System.ArgumentNullException">
		/// if entry passed is null.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// if an I/O error occured.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// if stream was finished
		/// </exception>
		/// <exception cref="ZipException">
		/// Too many entries in the Zip file<br/>
		/// Entry name is too long<br/>
		/// Finish has already been called<br/>
		/// </exception>
		/// <exception cref="System.NotImplementedException">
		/// The Compression method specified for the entry is unsupported.
		/// </exception>
		public async Task PutNextEntryAsync(ZipEntry entry, CancellationToken ct = default)
		{
			if (curEntry != null) await CloseEntryAsync(ct);
			await baseOutputStream_.WriteProcToStreamAsync(s =>
			{
				PutNextEntry(s, entry, baseOutputStream_.Position);
			}, ct);

			if (!entry.IsCrypted) return;
			await WriteOutputAsync(GetEntryEncryptionHeader(entry));
		}

		/// <summary>
		/// Closes the current entry, updating header and footer information as required
		/// </summary>
		/// <exception cref="ZipException">
		/// Invalid entry field values.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurs.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// No entry is active.
		/// </exception>
		public void CloseEntry()
		{
			WriteEntryFooter(baseOutputStream_);

			// Patch the header if possible
			if (patchEntryHeader)
			{
				patchEntryHeader = false;
				ZipFormat.PatchLocalHeaderSync(baseOutputStream_, curEntry, patchData);
			}

			entries.Add(curEntry);
			curEntry = null;
		}

		/// <inheritdoc cref="CloseEntry"/>
		public async Task CloseEntryAsync(CancellationToken ct)
		{
			await baseOutputStream_.WriteProcToStreamAsync(WriteEntryFooter, ct);

			// Patch the header if possible
			if (patchEntryHeader)
			{
				patchEntryHeader = false;
				await ZipFormat.PatchLocalHeaderAsync(baseOutputStream_, curEntry, patchData, ct);
			}

			entries.Add(curEntry);
			curEntry = null;
		}

		internal void WriteEntryFooter(Stream stream)
		{
			if (curEntry == null)
			{
				throw new InvalidOperationException("No open entry");
			}

			if (entryIsPassthrough)
			{
				if (curEntry.CompressedSize != size)
				{
					throw new ZipException($"compressed size was {size}, but {curEntry.CompressedSize} expected");
				}

				offset += size;
				return;
			}

			long csize = size;

			// First finish the deflater, if appropriate
			if (curMethod == CompressionMethod.Deflated)
			{
				if (size >= 0)
				{
					base.Finish();
					csize = deflater_.TotalOut;
				}
				else
				{
					deflater_.Reset();
				}
			}
			else if (curMethod == CompressionMethod.Stored)
			{
				// This is done by Finish() for Deflated entries, but we need to do it
				// ourselves for Stored ones
				base.GetAuthCodeIfAES();
			}

			// Write the AES Authentication Code (a hash of the compressed and encrypted data)
			if (curEntry.AESKeySize > 0)
			{
				stream.Write(AESAuthCode, 0, 10);
				// Always use 0 as CRC for AE-2 format
				curEntry.Crc = 0;
			}
			else
			{
				if (curEntry.Crc < 0)
				{
					curEntry.Crc = crc.Value;
				}
				else if (curEntry.Crc != crc.Value)
				{
					throw new ZipException($"crc was {crc.Value}, but {curEntry.Crc} was expected");
				}
			}

			if (curEntry.Size < 0)
			{
				curEntry.Size = size;
			}
			else if (curEntry.Size != size)
			{
				throw new ZipException($"size was {size}, but {curEntry.Size} was expected");
			}

			if (curEntry.CompressedSize < 0)
			{
				curEntry.CompressedSize = csize;
			}
			else if (curEntry.CompressedSize != csize)
			{
				throw new ZipException($"compressed size was {csize}, but {curEntry.CompressedSize} expected");
			}

			offset += csize;

			if (curEntry.IsCrypted)
			{
				curEntry.CompressedSize += curEntry.EncryptionOverheadSize;
			}

			// Add data descriptor if flagged as required
			if ((curEntry.Flags & 8) != 0)
			{
				stream.WriteLEInt(ZipConstants.DataDescriptorSignature);
				stream.WriteLEInt(unchecked((int)curEntry.Crc));

				if (curEntry.LocalHeaderRequiresZip64)
				{
					stream.WriteLELong(curEntry.CompressedSize);
					stream.WriteLELong(curEntry.Size);
					offset += ZipConstants.Zip64DataDescriptorSize;
				}
				else
				{
					stream.WriteLEInt((int)curEntry.CompressedSize);
					stream.WriteLEInt((int)curEntry.Size);
					offset += ZipConstants.DataDescriptorSize;
				}
			}
		}



		// File format for AES:
		// Size (bytes)   Content
		// ------------   -------
		// Variable       Salt value
		// 2              Password verification value
		// Variable       Encrypted file data
		// 10             Authentication code
		//
		// Value in the "compressed size" fields of the local file header and the central directory entry
		// is the total size of all the items listed above. In other words, it is the total size of the
		// salt value, password verification value, encrypted data, and authentication code.

		/// <summary>
		/// Initializes encryption keys based on given password.
		/// </summary>
		protected byte[] InitializeAESPassword(ZipEntry entry, string rawPassword)
		{
			var salt = new byte[entry.AESSaltLen];
			// Salt needs to be cryptographically random, and unique per file
			if (_aesRnd == null)
				_aesRnd = RandomNumberGenerator.Create();
			_aesRnd.GetBytes(salt);
			int blockSize = entry.AESKeySize / 8;   // bits to bytes

			cryptoTransform_ = new ZipAESTransform(rawPassword, salt, blockSize, true);

			var headBytes = new byte[salt.Length + 2];

			Array.Copy(salt, headBytes, salt.Length);
			Array.Copy(((ZipAESTransform)cryptoTransform_).PwdVerifier, 0,
				headBytes, headBytes.Length - 2, 2);

			return headBytes;
		}

		private byte[] CreateZipCryptoHeader(long crcValue)
		{
			offset += ZipConstants.CryptoHeaderSize;

			InitializeZipCryptoPassword(Password);

			byte[] cryptBuffer = new byte[ZipConstants.CryptoHeaderSize];
			using (var rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(cryptBuffer);
			}

			cryptBuffer[11] = (byte)(crcValue >> 24);

			EncryptBlock(cryptBuffer, 0, cryptBuffer.Length);

			return cryptBuffer;
		}

		/// <summary>
		/// Initializes encryption keys based on given <paramref name="password"/>.
		/// </summary>
		/// <param name="password">The password.</param>
		private void InitializeZipCryptoPassword(string password)
		{
			var pkManaged = new PkzipClassicManaged();
			byte[] key = PkzipClassic.GenerateKeys(ZipCryptoEncoding.GetBytes(password));
			cryptoTransform_ = pkManaged.CreateEncryptor(key, null);
		}

		/// <summary>
		/// Writes the given buffer to the current entry.
		/// </summary>
		/// <param name="buffer">The buffer containing data to write.</param>
		/// <param name="offset">The offset of the first byte to write.</param>
		/// <param name="count">The number of bytes to write.</param>
		/// <exception cref="ZipException">Archive size is invalid</exception>
		/// <exception cref="System.InvalidOperationException">No entry is active.</exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (curEntry == null)
			{
				throw new InvalidOperationException("No open entry.");
			}

			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "Cannot be negative");
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), "Cannot be negative");
			}

			if ((buffer.Length - offset) < count)
			{
				throw new ArgumentException("Invalid offset/count combination");
			}

			if (curEntry.AESKeySize == 0 && !entryIsPassthrough)
			{
				// Only update CRC if AES is not enabled and entry is not a passthrough one
				crc.Update(new ArraySegment<byte>(buffer, offset, count));
			}

			size += count;

			if (curMethod == CompressionMethod.Stored || entryIsPassthrough)
			{
				if (Password != null)
				{
					CopyAndEncrypt(buffer, offset, count);
				}
				else
				{
					baseOutputStream_.Write(buffer, offset, count);
				}
			}
			else
			{
				base.Write(buffer, offset, count);
			}
		}

		private void CopyAndEncrypt(byte[] buffer, int offset, int count)
		{
			const int CopyBufferSize = 4096;
			byte[] localBuffer = new byte[CopyBufferSize];
			while (count > 0)
			{
				int bufferCount = (count < CopyBufferSize) ? count : CopyBufferSize;

				Array.Copy(buffer, offset, localBuffer, 0, bufferCount);
				EncryptBlock(localBuffer, 0, bufferCount);
				baseOutputStream_.Write(localBuffer, 0, bufferCount);
				count -= bufferCount;
				offset += bufferCount;
			}
		}

		/// <summary>
		/// Finishes the stream.  This will write the central directory at the
		/// end of the zip file and flush the stream.
		/// </summary>
		/// <remarks>
		/// This is automatically called when the stream is closed.
		/// </remarks>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurs.
		/// </exception>
		/// <exception cref="ZipException">
		/// Comment exceeds the maximum length<br/>
		/// Entry name exceeds the maximum length
		/// </exception>
		public override void Finish()
		{
			if (entries == null)
			{
				return;
			}

			if (curEntry != null)
			{
				CloseEntry();
			}

			long numEntries = entries.Count;
			long sizeEntries = 0;

			foreach (var entry in entries)
			{
				sizeEntries += ZipFormat.WriteEndEntry(baseOutputStream_, entry, _stringCodec);
			}

			ZipFormat.WriteEndOfCentralDirectory(baseOutputStream_, numEntries, sizeEntries, offset, zipComment);

			entries = null;
		}

		/// <inheritdoc cref="Finish"/>>
		public override async Task FinishAsync(CancellationToken ct)
		{
			using (var ms = new MemoryStream())
			{
				if (entries == null)
				{
					return;
				}

				if (curEntry != null)
				{
					await CloseEntryAsync(ct);
				}

				long numEntries = entries.Count;
				long sizeEntries = 0;

				foreach (var entry in entries)
				{
					await baseOutputStream_.WriteProcToStreamAsync(ms, s =>
					{
						sizeEntries += ZipFormat.WriteEndEntry(s, entry, _stringCodec);
					}, ct);
				}

				await baseOutputStream_.WriteProcToStreamAsync(ms, s
						=> ZipFormat.WriteEndOfCentralDirectory(s, numEntries, sizeEntries, offset, zipComment),
					ct);

				entries = null;
			}
		}

		/// <summary>
		/// Flushes the stream by calling <see cref="DeflaterOutputStream.Flush">Flush</see> on the deflater stream unless
		/// the current compression method is <see cref="CompressionMethod.Stored"/>. Then it flushes the underlying output stream.
		/// </summary>
		public override void Flush()
		{
			if (curMethod == CompressionMethod.Stored)
			{
				baseOutputStream_.Flush();
			}
			else
			{
				base.Flush();
			}
		}

		#region Instance Fields

		/// <summary>
		/// The entries for the archive.
		/// </summary>
		private List<ZipEntry> entries = new List<ZipEntry>();

		/// <summary>
		/// Used to track the crc of data added to entries.
		/// </summary>
		private Crc32 crc = new Crc32();

		/// <summary>
		/// The current entry being added.
		/// </summary>
		private ZipEntry curEntry;

		private bool entryIsPassthrough;

		private int defaultCompressionLevel = Deflater.DEFAULT_COMPRESSION;

		private CompressionMethod curMethod = CompressionMethod.Deflated;

		/// <summary>
		/// Used to track the size of data for an entry during writing.
		/// </summary>
		private long size;

		/// <summary>
		/// Offset to be recorded for each entry in the central header.
		/// </summary>
		private long offset;

		/// <summary>
		/// Comment for the entire archive recorded in central header.
		/// </summary>
		private byte[] zipComment = Empty.Array<byte>();

		/// <summary>
		/// Flag indicating that header patching is required for the current entry.
		/// </summary>
		private bool patchEntryHeader;

		/// <summary>
		/// The values to patch in the entry local header
		/// </summary>
		private EntryPatchData patchData;

		// Default is dynamic which is not backwards compatible and can cause problems
		// with XP's built in compression which cant read Zip64 archives.
		// However it does avoid the situation were a large file is added and cannot be completed correctly.
		// NOTE: Setting the size for entries before they are added is the best solution!
		private UseZip64 useZip64_ = UseZip64.Dynamic;

		/// <summary>
		/// The password to use when encrypting archive entries.
		/// </summary>
		private string password;

		private readonly StringCodec _stringCodec = ZipStrings.GetStringCodec();

		#endregion Instance Fields

		#region Static Fields

		// Static to help ensure that multiple files within a zip will get different random salt
		private static RandomNumberGenerator _aesRnd = RandomNumberGenerator.Create();

		#endregion Static Fields
	}
}
