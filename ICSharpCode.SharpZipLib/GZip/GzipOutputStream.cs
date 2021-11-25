using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;
using System.Text;

namespace ICSharpCode.SharpZipLib.GZip
{
	/// <summary>
	/// This filter stream is used to compress a stream into a "GZIP" stream.
	/// The "GZIP" format is described in RFC 1952.
	///
	/// author of the original java version : John Leuner
	/// </summary>
	/// <example> This sample shows how to gzip a file
	/// <code>
	/// using System;
	/// using System.IO;
	///
	/// using ICSharpCode.SharpZipLib.GZip;
	/// using ICSharpCode.SharpZipLib.Core;
	///
	/// class MainClass
	/// {
	/// 	public static void Main(string[] args)
	/// 	{
	/// 			using (Stream s = new GZipOutputStream(File.Create(args[0] + ".gz")))
	/// 			using (FileStream fs = File.OpenRead(args[0])) {
	/// 				byte[] writeData = new byte[4096];
	/// 				Streamutils.Copy(s, fs, writeData);
	/// 			}
	/// 		}
	/// 	}
	/// }
	/// </code>
	/// </example>
	public class GZipOutputStream : DeflaterOutputStream
	{
		private enum OutputState
		{
			Header,
			Footer,
			Finished,
			Closed,
		};

		#region Instance Fields

		/// <summary>
		/// CRC-32 value for uncompressed data
		/// </summary>
		protected Crc32 crc = new Crc32();

		private OutputState state_ = OutputState.Header;

		private string fileName;

		private GZipFlags flags = 0;

		#endregion Instance Fields

		#region Constructors

		/// <summary>
		/// Creates a GzipOutputStream with the default buffer size
		/// </summary>
		/// <param name="baseOutputStream">
		/// The stream to read data (to be compressed) from
		/// </param>
		public GZipOutputStream(Stream baseOutputStream)
			: this(baseOutputStream, 4096)
		{
		}

		/// <summary>
		/// Creates a GZipOutputStream with the specified buffer size
		/// </summary>
		/// <param name="baseOutputStream">
		/// The stream to read data (to be compressed) from
		/// </param>
		/// <param name="size">
		/// Size of the buffer to use
		/// </param>
		public GZipOutputStream(Stream baseOutputStream, int size) : base(baseOutputStream, new Deflater(Deflater.DEFAULT_COMPRESSION, true), size)
		{
		}

		#endregion Constructors

		#region Public API

		/// <summary>
		/// Sets the active compression level (0-9).  The new level will be activated
		/// immediately.
		/// </summary>
		/// <param name="level">The compression level to set.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Level specified is not supported.
		/// </exception>
		/// <see cref="Deflater"/>
		public void SetLevel(int level)
		{
			if (level < Deflater.NO_COMPRESSION || level > Deflater.BEST_COMPRESSION)
				throw new ArgumentOutOfRangeException(nameof(level), "Compression level must be 0-9");

			deflater_.SetLevel(level);
		}

		/// <summary>
		/// Get the current compression level.
		/// </summary>
		/// <returns>The current compression level.</returns>
		public int GetLevel()
		{
			return deflater_.GetLevel();
		}

		/// <summary>
		/// Original filename
		/// </summary>
		public string FileName
		{
			get => fileName;
			set
			{
				fileName = CleanFilename(value);
				if (string.IsNullOrEmpty(fileName))
				{
					flags &= ~GZipFlags.FNAME;
				}
				else
				{
					flags |= GZipFlags.FNAME;
				}
			}
		}

		#endregion Public API

		#region Stream overrides

		/// <summary>
		/// Write given buffer to output updating crc
		/// </summary>
		/// <param name="buffer">Buffer to write</param>
		/// <param name="offset">Offset of first byte in buf to write</param>
		/// <param name="count">Number of bytes to write</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (state_ == OutputState.Header)
			{
				WriteHeader();
			}

			if (state_ != OutputState.Footer)
			{
				throw new InvalidOperationException("Write not permitted in current state");
			}

			crc.Update(new ArraySegment<byte>(buffer, offset, count));
			base.Write(buffer, offset, count);
		}

		/// <summary>
		/// Writes remaining compressed output data to the output stream
		/// and closes it.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			try
			{
				Finish();
			}
			finally
			{
				if (state_ != OutputState.Closed)
				{
					state_ = OutputState.Closed;
					if (IsStreamOwner)
					{
						baseOutputStream_.Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Flushes the stream by ensuring the header is written, and then calling <see cref="DeflaterOutputStream.Flush">Flush</see>
		/// on the deflater.
		/// </summary>
		public override void Flush()
		{
			if (state_ == OutputState.Header)
			{
				WriteHeader();
			}

			base.Flush();
		}

		#endregion Stream overrides

		#region DeflaterOutputStream overrides

		/// <summary>
		/// Finish compression and write any footer information required to stream
		/// </summary>
		public override void Finish()
		{
			// If no data has been written a header should be added.
			if (state_ == OutputState.Header)
			{
				WriteHeader();
			}

			if (state_ == OutputState.Footer)
			{
				state_ = OutputState.Finished;
				base.Finish();

				var totalin = (uint)(deflater_.TotalIn & 0xffffffff);
				var crcval = (uint)(crc.Value & 0xffffffff);

				byte[] gzipFooter;

				unchecked
				{
					gzipFooter = new byte[] {
					(byte) crcval, (byte) (crcval >> 8),
					(byte) (crcval >> 16), (byte) (crcval >> 24),

					(byte) totalin, (byte) (totalin >> 8),
					(byte) (totalin >> 16), (byte) (totalin >> 24)
				};
				}

				baseOutputStream_.Write(gzipFooter, 0, gzipFooter.Length);
			}
		}

		#endregion DeflaterOutputStream overrides

		#region Support Routines

		private static string CleanFilename(string path)
			=> path.Substring(path.LastIndexOf('/') + 1);

		private void WriteHeader()
		{
			if (state_ == OutputState.Header)
			{
				state_ = OutputState.Footer;

				var mod_time = (int)((DateTime.Now.Ticks - new DateTime(1970, 1, 1).Ticks) / 10000000L);  // Ticks give back 100ns intervals
				byte[] gzipHeader = {
					// The two magic bytes
					GZipConstants.ID1, 
					GZipConstants.ID2,

					// The compression type
					GZipConstants.CompressionMethodDeflate,

					// The flags (not set)
					(byte)flags,

					// The modification time
					(byte) mod_time, (byte) (mod_time >> 8),
					(byte) (mod_time >> 16), (byte) (mod_time >> 24),

					// The extra flags
					0,

					// The OS type (unknown)
					255
				};

				baseOutputStream_.Write(gzipHeader, 0, gzipHeader.Length);

				if (flags.HasFlag(GZipFlags.FNAME))
				{
					var fname = GZipConstants.Encoding.GetBytes(fileName);
					baseOutputStream_.Write(fname, 0, fname.Length);

					// End filename string with a \0
					baseOutputStream_.Write(new byte[] { 0 }, 0, 1);
				}
			}
		}

		#endregion Support Routines
	}
}
