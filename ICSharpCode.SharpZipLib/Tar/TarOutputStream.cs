using System;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.Tar
{
	/// <summary>
	/// The TarOutputStream writes a UNIX tar archive as an OutputStream.
	/// Methods are provided to put entries, and then write their contents
	/// by writing to this stream using write().
	/// </summary>
	/// public
	public class TarOutputStream : Stream
	{
		#region Constructors

		/// <summary>
		/// Construct TarOutputStream using default block factor
		/// </summary>
		/// <param name="outputStream">stream to write to</param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		public TarOutputStream(Stream outputStream)
			: this(outputStream, TarBuffer.DefaultBlockFactor)
		{
		}

		/// <summary>
		/// Construct TarOutputStream using default block factor
		/// </summary>
		/// <param name="outputStream">stream to write to</param>
		/// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
		public TarOutputStream(Stream outputStream, Encoding nameEncoding)
			: this(outputStream, TarBuffer.DefaultBlockFactor, nameEncoding)
		{
		}

		/// <summary>
		/// Construct TarOutputStream with user specified block factor
		/// </summary>
		/// <param name="outputStream">stream to write to</param>
		/// <param name="blockFactor">blocking factor</param>
		[Obsolete("No Encoding for Name field is specified, any non-ASCII bytes will be discarded")]
		public TarOutputStream(Stream outputStream, int blockFactor)
		{
			if (outputStream == null)
			{
				throw new ArgumentNullException(nameof(outputStream));
			}

			this.outputStream = outputStream;
			buffer = TarBuffer.CreateOutputTarBuffer(outputStream, blockFactor);

			assemblyBuffer = new byte[TarBuffer.BlockSize];
			blockBuffer = new byte[TarBuffer.BlockSize];
		}

		/// <summary>
		/// Construct TarOutputStream with user specified block factor
		/// </summary>
		/// <param name="outputStream">stream to write to</param>
		/// <param name="blockFactor">blocking factor</param>
		/// <param name="nameEncoding">The <see cref="Encoding"/> used for the Name fields, or null for ASCII only</param>
		public TarOutputStream(Stream outputStream, int blockFactor, Encoding nameEncoding)
		{
			if (outputStream == null)
			{
				throw new ArgumentNullException(nameof(outputStream));
			}

			this.outputStream = outputStream;
			buffer = TarBuffer.CreateOutputTarBuffer(outputStream, blockFactor);

			assemblyBuffer = new byte[TarBuffer.BlockSize];
			blockBuffer = new byte[TarBuffer.BlockSize];

			this.nameEncoding = nameEncoding;
		}

		#endregion Constructors

		/// <summary>
		/// Gets or sets a flag indicating ownership of underlying stream.
		/// When the flag is true <see cref="Stream.Dispose()" /> will close the underlying stream also.
		/// </summary>
		/// <remarks>The default value is true.</remarks>
		public bool IsStreamOwner
		{
			get { return buffer.IsStreamOwner; }
			set { buffer.IsStreamOwner = value; }
		}

		/// <summary>
		/// true if the stream supports reading; otherwise, false.
		/// </summary>
		public override bool CanRead
		{
			get
			{
				return outputStream.CanRead;
			}
		}

		/// <summary>
		/// true if the stream supports seeking; otherwise, false.
		/// </summary>
		public override bool CanSeek
		{
			get
			{
				return outputStream.CanSeek;
			}
		}

		/// <summary>
		/// true if stream supports writing; otherwise, false.
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return outputStream.CanWrite;
			}
		}

		/// <summary>
		/// length of stream in bytes
		/// </summary>
		public override long Length
		{
			get
			{
				return outputStream.Length;
			}
		}

		/// <summary>
		/// gets or sets the position within the current stream.
		/// </summary>
		public override long Position
		{
			get
			{
				return outputStream.Position;
			}
			set
			{
				outputStream.Position = value;
			}
		}

		/// <summary>
		/// set the position within the current stream
		/// </summary>
		/// <param name="offset">The offset relative to the <paramref name="origin"/> to seek to</param>
		/// <param name="origin">The <see cref="SeekOrigin"/> to seek from.</param>
		/// <returns>The new position in the stream.</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			return outputStream.Seek(offset, origin);
		}

		/// <summary>
		/// Set the length of the current stream
		/// </summary>
		/// <param name="value">The new stream length.</param>
		public override void SetLength(long value)
		{
			outputStream.SetLength(value);
		}

		/// <summary>
		/// Read a byte from the stream and advance the position within the stream
		/// by one byte or returns -1 if at the end of the stream.
		/// </summary>
		/// <returns>The byte value or -1 if at end of stream</returns>
		public override int ReadByte()
		{
			return outputStream.ReadByte();
		}

		/// <summary>
		/// read bytes from the current stream and advance the position within the
		/// stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">The buffer to store read bytes in.</param>
		/// <param name="offset">The index into the buffer to being storing bytes at.</param>
		/// <param name="count">The desired number of bytes to read.</param>
		/// <returns>The total number of bytes read, or zero if at the end of the stream.
		/// The number of bytes may be less than the <paramref name="count">count</paramref>
		/// requested if data is not available.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			return outputStream.Read(buffer, offset, count);
		}

		/// <summary>
		/// All buffered data is written to destination
		/// </summary>
		public override void Flush()
		{
			outputStream.Flush();
		}

		/// <summary>
		/// Ends the TAR archive without closing the underlying OutputStream.
		/// The result is that the EOF block of nulls is written.
		/// </summary>
		public void Finish()
		{
			if (IsEntryOpen)
			{
				CloseEntry();
			}
			WriteEofBlock();
		}

		/// <summary>
		/// Ends the TAR archive and closes the underlying OutputStream.
		/// </summary>
		/// <remarks>This means that Finish() is called followed by calling the
		/// TarBuffer's Close().</remarks>
		protected override void Dispose(bool disposing)
		{
			if (!isClosed)
			{
				isClosed = true;
				Finish();
				buffer.Close();
			}
		}

		/// <summary>
		/// Get the record size being used by this stream's TarBuffer.
		/// </summary>
		public int RecordSize
		{
			get { return buffer.RecordSize; }
		}

		/// <summary>
		/// Get the record size being used by this stream's TarBuffer.
		/// </summary>
		/// <returns>
		/// The TarBuffer record size.
		/// </returns>
		[Obsolete("Use RecordSize property instead")]
		public int GetRecordSize()
		{
			return buffer.RecordSize;
		}

		/// <summary>
		/// Get a value indicating whether an entry is open, requiring more data to be written.
		/// </summary>
		private bool IsEntryOpen
		{
			get { return (currBytes < currSize); }
		}

		/// <summary>
		/// Put an entry on the output stream. This writes the entry's
		/// header and positions the output stream for writing
		/// the contents of the entry. Once this method is called, the
		/// stream is ready for calls to write() to write the entry's
		/// contents. Once the contents are written, closeEntry()
		/// <B>MUST</B> be called to ensure that all buffered data
		/// is completely written to the output stream.
		/// </summary>
		/// <param name="entry">
		/// The TarEntry to be written to the archive.
		/// </param>
		public void PutNextEntry(TarEntry entry)
		{
			if (entry == null)
			{
				throw new ArgumentNullException(nameof(entry));
			}

			var namelen = nameEncoding != null ? nameEncoding.GetByteCount(entry.TarHeader.Name) : entry.TarHeader.Name.Length;

			if (namelen > TarHeader.NAMELEN)
			{
				var longHeader = new TarHeader();
				longHeader.TypeFlag = TarHeader.LF_GNU_LONGNAME;
				longHeader.Name = longHeader.Name + "././@LongLink";
				longHeader.Mode = 420;//644 by default
				longHeader.UserId = entry.UserId;
				longHeader.GroupId = entry.GroupId;
				longHeader.GroupName = entry.GroupName;
				longHeader.UserName = entry.UserName;
				longHeader.LinkName = "";
				longHeader.Size = namelen + 1;  // Plus one to avoid dropping last char

				longHeader.WriteHeader(blockBuffer, nameEncoding);
				buffer.WriteBlock(blockBuffer);  // Add special long filename header block

				int nameCharIndex = 0;

				while (nameCharIndex < namelen + 1 /* we've allocated one for the null char, now we must make sure it gets written out */)
				{
					Array.Clear(blockBuffer, 0, blockBuffer.Length);
					TarHeader.GetAsciiBytes(entry.TarHeader.Name, nameCharIndex, this.blockBuffer, 0, TarBuffer.BlockSize, nameEncoding); // This func handles OK the extra char out of string length
					nameCharIndex += TarBuffer.BlockSize;
					buffer.WriteBlock(blockBuffer);
				}
			}

			entry.WriteEntryHeader(blockBuffer, nameEncoding);
			buffer.WriteBlock(blockBuffer);

			currBytes = 0;

			currSize = entry.IsDirectory ? 0 : entry.Size;
		}

		/// <summary>
		/// Close an entry. This method MUST be called for all file
		/// entries that contain data. The reason is that we must
		/// buffer data written to the stream in order to satisfy
		/// the buffer's block based writes. Thus, there may be
		/// data fragments still being assembled that must be written
		/// to the output stream before this entry is closed and the
		/// next entry written.
		/// </summary>
		public void CloseEntry()
		{
			if (assemblyBufferLength > 0)
			{
				Array.Clear(assemblyBuffer, assemblyBufferLength, assemblyBuffer.Length - assemblyBufferLength);

				buffer.WriteBlock(assemblyBuffer);

				currBytes += assemblyBufferLength;
				assemblyBufferLength = 0;
			}

			if (currBytes < currSize)
			{
				string errorText = string.Format(
					"Entry closed at '{0}' before the '{1}' bytes specified in the header were written",
					currBytes, currSize);
				throw new TarException(errorText);
			}
		}

		/// <summary>
		/// Writes a byte to the current tar archive entry.
		/// This method simply calls Write(byte[], int, int).
		/// </summary>
		/// <param name="value">
		/// The byte to be written.
		/// </param>
		public override void WriteByte(byte value)
		{
			Write(new byte[] { value }, 0, 1);
		}

		/// <summary>
		/// Writes bytes to the current tar archive entry. This method
		/// is aware of the current entry and will throw an exception if
		/// you attempt to write bytes past the length specified for the
		/// current entry. The method is also (painfully) aware of the
		/// record buffering required by TarBuffer, and manages buffers
		/// that are not a multiple of recordsize in length, including
		/// assembling records from small buffers.
		/// </summary>
		/// <param name = "buffer">
		/// The buffer to write to the archive.
		/// </param>
		/// <param name = "offset">
		/// The offset in the buffer from which to get bytes.
		/// </param>
		/// <param name = "count">
		/// The number of bytes to write.
		/// </param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "Cannot be negative");
			}

			if (buffer.Length - offset < count)
			{
				throw new ArgumentException("offset and count combination is invalid");
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), "Cannot be negative");
			}

			if ((currBytes + count) > currSize)
			{
				string errorText = string.Format("request to write '{0}' bytes exceeds size in header of '{1}' bytes",
					count, this.currSize);
				throw new ArgumentOutOfRangeException(nameof(count), errorText);
			}

			//
			// We have to deal with assembly!!!
			// The programmer can be writing little 32 byte chunks for all
			// we know, and we must assemble complete blocks for writing.
			// TODO  REVIEW Maybe this should be in TarBuffer? Could that help to
			//        eliminate some of the buffer copying.
			//
			if (assemblyBufferLength > 0)
			{
				if ((assemblyBufferLength + count) >= blockBuffer.Length)
				{
					int aLen = blockBuffer.Length - assemblyBufferLength;

					Array.Copy(assemblyBuffer, 0, blockBuffer, 0, assemblyBufferLength);
					Array.Copy(buffer, offset, blockBuffer, assemblyBufferLength, aLen);

					this.buffer.WriteBlock(blockBuffer);

					currBytes += blockBuffer.Length;

					offset += aLen;
					count -= aLen;

					assemblyBufferLength = 0;
				}
				else
				{
					Array.Copy(buffer, offset, assemblyBuffer, assemblyBufferLength, count);
					offset += count;
					assemblyBufferLength += count;
					count -= count;
				}
			}

			//
			// When we get here we have EITHER:
			//   o An empty "assembly" buffer.
			//   o No bytes to write (count == 0)
			//
			while (count > 0)
			{
				if (count < blockBuffer.Length)
				{
					Array.Copy(buffer, offset, assemblyBuffer, assemblyBufferLength, count);
					assemblyBufferLength += count;
					break;
				}

				this.buffer.WriteBlock(buffer, offset);

				int bufferLength = blockBuffer.Length;
				currBytes += bufferLength;
				count -= bufferLength;
				offset += bufferLength;
			}
		}

		/// <summary>
		/// Write an EOF (end of archive) block to the tar archive.
		/// The	end of the archive is indicated	by two blocks consisting entirely of zero bytes.
		/// </summary>
		private void WriteEofBlock()
		{
			Array.Clear(blockBuffer, 0, blockBuffer.Length);
			buffer.WriteBlock(blockBuffer);
			buffer.WriteBlock(blockBuffer);
		}

		#region Instance Fields

		/// <summary>
		/// bytes written for this entry so far
		/// </summary>
		private long currBytes;

		/// <summary>
		/// current 'Assembly' buffer length
		/// </summary>
		private int assemblyBufferLength;

		/// <summary>
		/// Flag indicating whether this instance has been closed or not.
		/// </summary>
		private bool isClosed;

		/// <summary>
		/// Size for the current entry
		/// </summary>
		protected long currSize;

		/// <summary>
		/// single block working buffer
		/// </summary>
		protected byte[] blockBuffer;

		/// <summary>
		/// 'Assembly' buffer used to assemble data before writing
		/// </summary>
		protected byte[] assemblyBuffer;

		/// <summary>
		/// TarBuffer used to provide correct blocking factor
		/// </summary>
		protected TarBuffer buffer;

		/// <summary>
		/// the destination stream for the archive contents
		/// </summary>
		protected Stream outputStream;

		/// <summary>
		/// name encoding
		/// </summary>
		protected Encoding nameEncoding;

		#endregion Instance Fields
	}
}
