using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// Provides simple <see cref="Stream"/>" utilities.
	/// </summary>
	public static class StreamUtils
	{
		/// <summary>
		/// Read from a <see cref="Stream"/> ensuring all the required data is read.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="buffer">The buffer to fill.</param>
		/// <seealso cref="ReadFully(Stream,byte[],int,int)"/>
		public static void ReadFully(Stream stream, byte[] buffer)
		{
			ReadFully(stream, buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Read from a <see cref="Stream"/>" ensuring all the required data is read.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="buffer">The buffer to store data in.</param>
		/// <param name="offset">The offset at which to begin storing data.</param>
		/// <param name="count">The number of bytes of data to store.</param>
		/// <exception cref="ArgumentNullException">Required parameter is null</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> and or <paramref name="count"/> are invalid.</exception>
		/// <exception cref="EndOfStreamException">End of stream is encountered before all the data has been read.</exception>
		public static void ReadFully(Stream stream, byte[] buffer, int offset, int count)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			// Offset can equal length when buffer and count are 0.
			if ((offset < 0) || (offset > buffer.Length))
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			if ((count < 0) || (offset + count > buffer.Length))
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			while (count > 0)
			{
				int readCount = stream.Read(buffer, offset, count);
				if (readCount <= 0)
				{
					throw new EndOfStreamException();
				}
				offset += readCount;
				count -= readCount;
			}
		}

		/// <summary>
		/// Read as much data as possible from a <see cref="Stream"/>", up to the requested number of bytes
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="buffer">The buffer to store data in.</param>
		/// <param name="offset">The offset at which to begin storing data.</param>
		/// <param name="count">The number of bytes of data to store.</param>
		/// <exception cref="ArgumentNullException">Required parameter is null</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> and or <paramref name="count"/> are invalid.</exception>
		public static int ReadRequestedBytes(Stream stream, byte[] buffer, int offset, int count)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			// Offset can equal length when buffer and count are 0.
			if ((offset < 0) || (offset > buffer.Length))
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			if ((count < 0) || (offset + count > buffer.Length))
			{
				throw new ArgumentOutOfRangeException(nameof(count));
			}

			int totalReadCount = 0;
			while (count > 0)
			{
				int readCount = stream.Read(buffer, offset, count);
				if (readCount <= 0)
				{
					break;
				}
				offset += readCount;
				count -= readCount;
				totalReadCount += readCount;
			}

			return totalReadCount;
		}

		/// <summary>
		/// Copy the contents of one <see cref="Stream"/> to another.
		/// </summary>
		/// <param name="source">The stream to source data from.</param>
		/// <param name="destination">The stream to write data to.</param>
		/// <param name="buffer">The buffer to use during copying.</param>
		public static void Copy(Stream source, Stream destination, byte[] buffer)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			// Ensure a reasonable size of buffer is used without being prohibitive.
			if (buffer.Length < 128)
			{
				throw new ArgumentException("Buffer is too small", nameof(buffer));
			}

			bool copying = true;

			while (copying)
			{
				int bytesRead = source.Read(buffer, 0, buffer.Length);
				if (bytesRead > 0)
				{
					destination.Write(buffer, 0, bytesRead);
				}
				else
				{
					destination.Flush();
					copying = false;
				}
			}
		}

		/// <summary>
		/// Copy the contents of one <see cref="Stream"/> to another.
		/// </summary>
		/// <param name="source">The stream to source data from.</param>
		/// <param name="destination">The stream to write data to.</param>
		/// <param name="buffer">The buffer to use during copying.</param>
		/// <param name="progressHandler">The <see cref="ProgressHandler">progress handler delegate</see> to use.</param>
		/// <param name="updateInterval">The minimum <see cref="TimeSpan"/> between progress updates.</param>
		/// <param name="sender">The source for this event.</param>
		/// <param name="name">The name to use with the event.</param>
		/// <remarks>This form is specialised for use within #Zip to support events during archive operations.</remarks>
		public static void Copy(Stream source, Stream destination,
			byte[] buffer, ProgressHandler progressHandler, TimeSpan updateInterval, object sender, string name)
		{
			Copy(source, destination, buffer, progressHandler, updateInterval, sender, name, -1);
		}

		/// <summary>
		/// Copy the contents of one <see cref="Stream"/> to another.
		/// </summary>
		/// <param name="source">The stream to source data from.</param>
		/// <param name="destination">The stream to write data to.</param>
		/// <param name="buffer">The buffer to use during copying.</param>
		/// <param name="progressHandler">The <see cref="ProgressHandler">progress handler delegate</see> to use.</param>
		/// <param name="updateInterval">The minimum <see cref="TimeSpan"/> between progress updates.</param>
		/// <param name="sender">The source for this event.</param>
		/// <param name="name">The name to use with the event.</param>
		/// <param name="fixedTarget">A predetermined fixed target value to use with progress updates.
		/// If the value is negative the target is calculated by looking at the stream.</param>
		/// <remarks>This form is specialised for use within #Zip to support events during archive operations.</remarks>
		public static void Copy(Stream source, Stream destination,
			byte[] buffer,
			ProgressHandler progressHandler, TimeSpan updateInterval,
			object sender, string name, long fixedTarget)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			// Ensure a reasonable size of buffer is used without being prohibitive.
			if (buffer.Length < 128)
			{
				throw new ArgumentException("Buffer is too small", nameof(buffer));
			}

			if (progressHandler == null)
			{
				throw new ArgumentNullException(nameof(progressHandler));
			}

			bool copying = true;

			DateTime marker = DateTime.Now;
			long processed = 0;
			long target = 0;

			if (fixedTarget >= 0)
			{
				target = fixedTarget;
			}
			else if (source.CanSeek)
			{
				target = source.Length - source.Position;
			}

			// Always fire 0% progress..
			var args = new ProgressEventArgs(name, processed, target);
			progressHandler(sender, args);

			bool progressFired = true;

			while (copying)
			{
				int bytesRead = source.Read(buffer, 0, buffer.Length);
				if (bytesRead > 0)
				{
					processed += bytesRead;
					progressFired = false;
					destination.Write(buffer, 0, bytesRead);
				}
				else
				{
					destination.Flush();
					copying = false;
				}

				if (DateTime.Now - marker > updateInterval)
				{
					progressFired = true;
					marker = DateTime.Now;
					args = new ProgressEventArgs(name, processed, target);
					progressHandler(sender, args);

					copying = args.ContinueRunning;
				}
			}

			if (!progressFired)
			{
				args = new ProgressEventArgs(name, processed, target);
				progressHandler(sender, args);
			}
		}
		
		internal static async Task WriteProcToStreamAsync(this Stream targetStream, MemoryStream bufferStream, Action<Stream> writeProc, CancellationToken ct)
		{
			bufferStream.SetLength(0);
			writeProc(bufferStream);
			bufferStream.Position = 0;
			await bufferStream.CopyToAsync(targetStream, 81920, ct);
			bufferStream.SetLength(0);
		}
		
		internal static async Task WriteProcToStreamAsync(this Stream targetStream, Action<Stream> writeProc, CancellationToken ct)
		{
			using (var ms = new MemoryStream())
			{
				await WriteProcToStreamAsync(targetStream, ms, writeProc, ct);
			}
		}
	}
}
