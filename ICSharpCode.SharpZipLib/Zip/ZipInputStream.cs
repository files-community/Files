using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Encryption;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// This is an InflaterInputStream that reads the files baseInputStream an zip archive
	/// one after another.  It has a special method to get the zip entry of
	/// the next file.  The zip entry contains information about the file name
	/// size, compressed size, Crc, etc.
	/// It includes support for Stored and Deflated entries.
	/// <br/>
	/// <br/>Author of the original java version : Jochen Hoenicke
	/// </summary>
	///
	/// <example> This sample shows how to read a zip file
	/// <code lang="C#">
	/// using System;
	/// using System.Text;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.Zip;
	///
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	/// 		using ( ZipInputStream s = new ZipInputStream(File.OpenRead(args[0]))) {
	///
	/// 			ZipEntry theEntry;
	/// 			const int size = 2048;
	/// 			byte[] data = new byte[2048];
	///
	/// 			while ((theEntry = s.GetNextEntry()) != null) {
	///                 if ( entry.IsFile ) {
	/// 				    Console.Write("Show contents (y/n) ?");
	/// 				    if (Console.ReadLine() == "y") {
	/// 				    	while (true) {
	/// 				    		size = s.Read(data, 0, data.Length);
	/// 				    		if (size > 0) {
	/// 				    			Console.Write(new ASCIIEncoding().GetString(data, 0, size));
	/// 				    		} else {
	/// 				    			break;
	/// 				    		}
	/// 				    	}
	/// 				    }
	/// 				}
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class ZipInputStream : InflaterInputStream
	{
		#region Instance Fields

		/// <summary>
		/// Delegate for reading bytes from a stream.
		/// </summary>
		private delegate int ReadDataHandler(byte[] b, int offset, int length);

		/// <summary>
		/// The current reader this instance.
		/// </summary>
		private ReadDataHandler internalReader;

		private Crc32 crc = new Crc32();
		private ZipEntry entry;

		private long size;
		private CompressionMethod method;
		private int flags;
		private string password;
		private readonly StringCodec _stringCodec = ZipStrings.GetStringCodec();

		#endregion Instance Fields

		#region Constructors

		/// <summary>
		/// Creates a new Zip input stream, for reading a zip archive.
		/// </summary>
		/// <param name="baseInputStream">The underlying <see cref="Stream"/> providing data.</param>
		public ZipInputStream(Stream baseInputStream)
			: base(baseInputStream, new Inflater(true))
		{
			internalReader = new ReadDataHandler(ReadingNotAvailable);
		}

		/// <summary>
		/// Creates a new Zip input stream, for reading a zip archive.
		/// </summary>
		/// <param name="baseInputStream">The underlying <see cref="Stream"/> providing data.</param>
		/// <param name="bufferSize">Size of the buffer.</param>
		public ZipInputStream(Stream baseInputStream, int bufferSize)
			: base(baseInputStream, new Inflater(true), bufferSize)
		{
			internalReader = new ReadDataHandler(ReadingNotAvailable);
		}

		#endregion Constructors

		/// <summary>
		/// Optional password used for encryption when non-null
		/// </summary>
		/// <value>A password for all encrypted <see cref="ZipEntry">entries </see> in this <see cref="ZipInputStream"/></value>
		public string Password
		{
			get
			{
				return password;
			}
			set
			{
				password = value;
			}
		}

		/// <summary>
		/// Gets a value indicating if there is a current entry and it can be decompressed
		/// </summary>
		/// <remarks>
		/// The entry can only be decompressed if the library supports the zip features required to extract it.
		/// See the <see cref="ZipEntry.Version">ZipEntry Version</see> property for more details.
		///
		/// Since <see cref="ZipInputStream"/> uses the local headers for extraction, entries with no compression combined with the
		/// <see cref="GeneralBitFlags.Descriptor"/> flag set, cannot be extracted as the end of the entry data cannot be deduced.
		/// </remarks>
		public bool CanDecompressEntry 
			=> entry != null
			&& IsEntryCompressionMethodSupported(entry)
			&& entry.CanDecompress
			&& (!entry.HasFlag(GeneralBitFlags.Descriptor) || entry.CompressionMethod != CompressionMethod.Stored || entry.IsCrypted);

		/// <summary>
		/// Is the compression method for the specified entry supported?
		/// </summary>
		/// <remarks>
		/// Uses entry.CompressionMethodForHeader so that entries of type WinZipAES will be rejected. 
		/// </remarks>
		/// <param name="entry">the entry to check.</param>
		/// <returns>true if the compression method is supported, false if not.</returns>
		private static bool IsEntryCompressionMethodSupported(ZipEntry entry)
		{
			var entryCompressionMethod = entry.CompressionMethodForHeader;

			return entryCompressionMethod == CompressionMethod.Deflated ||
				   entryCompressionMethod == CompressionMethod.Stored;
		}

		/// <summary>
		/// Advances to the next entry in the archive
		/// </summary>
		/// <returns>
		/// The next <see cref="ZipEntry">entry</see> in the archive or null if there are no more entries.
		/// </returns>
		/// <remarks>
		/// If the previous entry is still open <see cref="CloseEntry">CloseEntry</see> is called.
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// Input stream is closed
		/// </exception>
		/// <exception cref="ZipException">
		/// Password is not set, password is invalid, compression method is invalid,
		/// version required to extract is not supported
		/// </exception>
		public ZipEntry GetNextEntry()
		{
			if (crc == null)
			{
				throw new InvalidOperationException("Closed.");
			}

			if (entry != null)
			{
				CloseEntry();
			}

			int header = inputBuffer.ReadLeInt();

			if (header == ZipConstants.CentralHeaderSignature ||
				header == ZipConstants.EndOfCentralDirectorySignature ||
				header == ZipConstants.CentralHeaderDigitalSignature ||
				header == ZipConstants.ArchiveExtraDataSignature ||
				header == ZipConstants.Zip64CentralFileHeaderSignature)
			{
				// No more individual entries exist
				Dispose();
				return null;
			}

			// -jr- 07-Dec-2003 Ignore spanning temporary signatures if found
			// Spanning signature is same as descriptor signature and is untested as yet.
			if ((header == ZipConstants.SpanningTempSignature) || (header == ZipConstants.SpanningSignature))
			{
				header = inputBuffer.ReadLeInt();
			}

			if (header != ZipConstants.LocalHeaderSignature)
			{
				throw new ZipException("Wrong Local header signature: 0x" + String.Format("{0:X}", header));
			}

			var versionRequiredToExtract = (short)inputBuffer.ReadLeShort();

			flags = inputBuffer.ReadLeShort();
			method = (CompressionMethod)inputBuffer.ReadLeShort();
			var dostime = (uint)inputBuffer.ReadLeInt();
			int crc2 = inputBuffer.ReadLeInt();
			csize = inputBuffer.ReadLeInt();
			size = inputBuffer.ReadLeInt();
			int nameLen = inputBuffer.ReadLeShort();
			int extraLen = inputBuffer.ReadLeShort();

			bool isCrypted = (flags & 1) == 1;

			byte[] buffer = new byte[nameLen];
			inputBuffer.ReadRawBuffer(buffer);

			var entryEncoding = _stringCodec.ZipInputEncoding(flags);
			string name = entryEncoding.GetString(buffer);
			var unicode = entryEncoding.IsZipUnicode();

			entry = new ZipEntry(name, versionRequiredToExtract, ZipConstants.VersionMadeBy, method, unicode)
			{
				Flags = flags,
			};

			if ((flags & 8) == 0)
			{
				entry.Crc = crc2 & 0xFFFFFFFFL;
				entry.Size = size & 0xFFFFFFFFL;
				entry.CompressedSize = csize & 0xFFFFFFFFL;

				entry.CryptoCheckValue = (byte)((crc2 >> 24) & 0xff);
			}
			else
			{
				// This allows for GNU, WinZip and possibly other archives, the PKZIP spec
				// says these values are zero under these circumstances.
				if (crc2 != 0)
				{
					entry.Crc = crc2 & 0xFFFFFFFFL;
				}

				if (size != 0)
				{
					entry.Size = size & 0xFFFFFFFFL;
				}

				if (csize != 0)
				{
					entry.CompressedSize = csize & 0xFFFFFFFFL;
				}

				entry.CryptoCheckValue = (byte)((dostime >> 8) & 0xff);
			}

			entry.DosTime = dostime;

			// If local header requires Zip64 is true then the extended header should contain
			// both values.

			// Handle extra data if present.  This can set/alter some fields of the entry.
			if (extraLen > 0)
			{
				byte[] extra = new byte[extraLen];
				inputBuffer.ReadRawBuffer(extra);
				entry.ExtraData = extra;
			}

			entry.ProcessExtraData(true);
			if (entry.CompressedSize >= 0)
			{
				csize = entry.CompressedSize;
			}

			if (entry.Size >= 0)
			{
				size = entry.Size;
			}

			if (method == CompressionMethod.Stored && (!isCrypted && csize != size || (isCrypted && csize - ZipConstants.CryptoHeaderSize != size)))
			{
				throw new ZipException("Stored, but compressed != uncompressed");
			}

			// Determine how to handle reading of data if this is attempted.
			if (IsEntryCompressionMethodSupported(entry))
			{
				internalReader = new ReadDataHandler(InitialRead);
			}
			else
			{
				internalReader = new ReadDataHandler(ReadingNotSupported);
			}

			return entry;
		}

		/// <summary>
		/// Read data descriptor at the end of compressed data.
		/// </summary>
		private void ReadDataDescriptor()
		{
			if (inputBuffer.ReadLeInt() != ZipConstants.DataDescriptorSignature)
			{
				throw new ZipException("Data descriptor signature not found");
			}

			entry.Crc = inputBuffer.ReadLeInt() & 0xFFFFFFFFL;

			if (entry.LocalHeaderRequiresZip64)
			{
				csize = inputBuffer.ReadLeLong();
				size = inputBuffer.ReadLeLong();
			}
			else
			{
				csize = inputBuffer.ReadLeInt();
				size = inputBuffer.ReadLeInt();
			}
			entry.CompressedSize = csize;
			entry.Size = size;
		}

		/// <summary>
		/// Complete cleanup as the final part of closing.
		/// </summary>
		/// <param name="testCrc">True if the crc value should be tested</param>
		private void CompleteCloseEntry(bool testCrc)
		{
			StopDecrypting();

			if ((flags & 8) != 0)
			{
				ReadDataDescriptor();
			}

			size = 0;

			if (testCrc &&
				((crc.Value & 0xFFFFFFFFL) != entry.Crc) && (entry.Crc != -1))
			{
				throw new ZipException("CRC mismatch");
			}

			crc.Reset();

			if (method == CompressionMethod.Deflated)
			{
				inf.Reset();
			}
			entry = null;
		}

		/// <summary>
		/// Closes the current zip entry and moves to the next one.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The stream is closed
		/// </exception>
		/// <exception cref="ZipException">
		/// The Zip stream ends early
		/// </exception>
		public void CloseEntry()
		{
			if (crc == null)
			{
				throw new InvalidOperationException("Closed");
			}

			if (entry == null)
			{
				return;
			}

			if (method == CompressionMethod.Deflated)
			{
				if ((flags & 8) != 0)
				{
					// We don't know how much we must skip, read until end.
					byte[] tmp = new byte[4096];

					// Read will close this entry
					while (Read(tmp, 0, tmp.Length) > 0)
					{
					}
					return;
				}

				csize -= inf.TotalIn;
				inputBuffer.Available += inf.RemainingInput;
			}

			if ((inputBuffer.Available > csize) && (csize >= 0))
			{
				inputBuffer.Available = (int)((long)inputBuffer.Available - csize);
			}
			else
			{
				csize -= inputBuffer.Available;
				inputBuffer.Available = 0;
				while (csize != 0)
				{
					long skipped = Skip(csize);

					if (skipped <= 0)
					{
						throw new ZipException("Zip archive ends early.");
					}

					csize -= skipped;
				}
			}

			CompleteCloseEntry(false);
		}

		/// <summary>
		/// Returns 1 if there is an entry available
		/// Otherwise returns 0.
		/// </summary>
		public override int Available
		{
			get
			{
				return entry != null ? 1 : 0;
			}
		}

		/// <summary>
		/// Returns the current size that can be read from the current entry if available
		/// </summary>
		/// <exception cref="ZipException">Thrown if the entry size is not known.</exception>
		/// <exception cref="InvalidOperationException">Thrown if no entry is currently available.</exception>
		public override long Length
		{
			get
			{
				if (entry != null)
				{
					if (entry.Size >= 0)
					{
						return entry.Size;
					}
					else
					{
						throw new ZipException("Length not available for the current entry");
					}
				}
				else
				{
					throw new InvalidOperationException("No current entry");
				}
			}
		}

		/// <summary>
		/// Reads a byte from the current zip entry.
		/// </summary>
		/// <returns>
		/// The byte or -1 if end of stream is reached.
		/// </returns>
		public override int ReadByte()
		{
			byte[] b = new byte[1];
			if (Read(b, 0, 1) <= 0)
			{
				return -1;
			}
			return b[0] & 0xff;
		}

		/// <summary>
		/// Handle attempts to read by throwing an <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <param name="destination">The destination array to store data in.</param>
		/// <param name="offset">The offset at which data read should be stored.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>Returns the number of bytes actually read.</returns>
		private int ReadingNotAvailable(byte[] destination, int offset, int count)
		{
			throw new InvalidOperationException("Unable to read from this stream");
		}

		/// <summary>
		/// Handle attempts to read from this entry by throwing an exception
		/// </summary>
		private int ReadingNotSupported(byte[] destination, int offset, int count)
		{
			throw new ZipException("The compression method for this entry is not supported");
		}

		/// <summary>
		/// Handle attempts to read from this entry by throwing an exception
		/// </summary>
		private int StoredDescriptorEntry(byte[] destination, int offset, int count) =>
			throw new StreamUnsupportedException(
				"The combination of Stored compression method and Descriptor flag is not possible to read using ZipInputStream");
		

		/// <summary>
		/// Perform the initial read on an entry which may include
		/// reading encryption headers and setting up inflation.
		/// </summary>
		/// <param name="destination">The destination to fill with data read.</param>
		/// <param name="offset">The offset to start reading at.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The actual number of bytes read.</returns>
		private int InitialRead(byte[] destination, int offset, int count)
		{
			var usesDescriptor = (entry.Flags & (int)GeneralBitFlags.Descriptor) != 0;

			// Handle encryption if required.
			if (entry.IsCrypted)
			{
				if (password == null)
				{
					throw new ZipException("No password set.");
				}

				// Generate and set crypto transform...
				var managed = new PkzipClassicManaged();
				byte[] key = PkzipClassic.GenerateKeys(_stringCodec.ZipCryptoEncoding.GetBytes(password));

				inputBuffer.CryptoTransform = managed.CreateDecryptor(key, null);

				byte[] cryptbuffer = new byte[ZipConstants.CryptoHeaderSize];
				inputBuffer.ReadClearTextBuffer(cryptbuffer, 0, ZipConstants.CryptoHeaderSize);

				if (cryptbuffer[ZipConstants.CryptoHeaderSize - 1] != entry.CryptoCheckValue)
				{
					throw new ZipException("Invalid password");
				}

				if (csize >= ZipConstants.CryptoHeaderSize)
				{
					csize -= ZipConstants.CryptoHeaderSize;
				}
				else if (!usesDescriptor)
				{
					throw new ZipException($"Entry compressed size {csize} too small for encryption");
				}
			}
			else
			{
				inputBuffer.CryptoTransform = null;
			}

			if (csize > 0 || usesDescriptor)
			{
				if (method == CompressionMethod.Deflated && inputBuffer.Available > 0)
				{
					inputBuffer.SetInflaterInput(inf);
				}

				// It's not possible to know how many bytes to read when using "Stored" compression (unless using encryption)
				if (!entry.IsCrypted && method == CompressionMethod.Stored && usesDescriptor)
				{
					internalReader = StoredDescriptorEntry;
					return StoredDescriptorEntry(destination, offset, count);
				}

				if (!CanDecompressEntry)
				{
					internalReader = ReadingNotSupported;
					return ReadingNotSupported(destination, offset, count);
				}

				internalReader = BodyRead;
				return BodyRead(destination, offset, count);
			}
			

			internalReader = ReadingNotAvailable;
			return 0;
		}

		/// <summary>
		/// Read a block of bytes from the stream.
		/// </summary>
		/// <param name="buffer">The destination for the bytes.</param>
		/// <param name="offset">The index to start storing data.</param>
		/// <param name="count">The number of bytes to attempt to read.</param>
		/// <returns>Returns the number of bytes read.</returns>
		/// <remarks>Zero bytes read means end of stream.</remarks>
		public override int Read(byte[] buffer, int offset, int count)
		{
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

			return internalReader(buffer, offset, count);
		}

		/// <summary>
		/// Reads a block of bytes from the current zip entry.
		/// </summary>
		/// <returns>
		/// The number of bytes read (this may be less than the length requested, even before the end of stream), or 0 on end of stream.
		/// </returns>
		/// <exception cref="IOException">
		/// An i/o error occurred.
		/// </exception>
		/// <exception cref="ZipException">
		/// The deflated stream is corrupted.
		/// </exception>
		/// <exception cref="InvalidOperationException">
		/// The stream is not open.
		/// </exception>
		private int BodyRead(byte[] buffer, int offset, int count)
		{
			if (crc == null)
			{
				throw new InvalidOperationException("Closed");
			}

			if ((entry == null) || (count <= 0))
			{
				return 0;
			}

			if (offset + count > buffer.Length)
			{
				throw new ArgumentException("Offset + count exceeds buffer size");
			}

			bool finished = false;

			switch (method)
			{
				case CompressionMethod.Deflated:
					count = base.Read(buffer, offset, count);
					if (count <= 0)
					{
						if (!inf.IsFinished)
						{
							throw new ZipException("Inflater not finished!");
						}
						inputBuffer.Available = inf.RemainingInput;

						// A csize of -1 is from an unpatched local header
						if ((flags & 8) == 0 &&
							(inf.TotalIn != csize && csize != 0xFFFFFFFF && csize != -1 || inf.TotalOut != size))
						{
							throw new ZipException("Size mismatch: " + csize + ";" + size + " <-> " + inf.TotalIn + ";" + inf.TotalOut);
						}
						inf.Reset();
						finished = true;
					}
					break;

				case CompressionMethod.Stored:
					if ((count > csize) && (csize >= 0))
					{
						count = (int)csize;
					}

					if (count > 0)
					{
						count = inputBuffer.ReadClearTextBuffer(buffer, offset, count);
						if (count > 0)
						{
							csize -= count;
							size -= count;
						}
					}

					if (csize == 0)
					{
						finished = true;
					}
					else
					{
						if (count < 0)
						{
							throw new ZipException("EOF in stored block");
						}
					}
					break;
			}

			if (count > 0)
			{
				crc.Update(new ArraySegment<byte>(buffer, offset, count));
			}

			if (finished)
			{
				CompleteCloseEntry(true);
			}

			return count;
		}

		/// <summary>
		/// Closes the zip input stream
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			internalReader = new ReadDataHandler(ReadingNotAvailable);
			crc = null;
			entry = null;

			base.Dispose(disposing);
		}
	}
}
