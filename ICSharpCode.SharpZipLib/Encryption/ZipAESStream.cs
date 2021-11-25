using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace ICSharpCode.SharpZipLib.Encryption
{
	/// <summary>
	/// Encrypts and decrypts AES ZIP
	/// </summary>
	/// <remarks>
	/// Based on information from http://www.winzip.com/aes_info.htm
	/// and http://www.gladman.me.uk/cryptography_technology/fileencrypt/
	/// </remarks>
	internal class ZipAESStream : CryptoStream
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stream">The stream on which to perform the cryptographic transformation.</param>
		/// <param name="transform">Instance of ZipAESTransform</param>
		/// <param name="mode">Read or Write</param>
		public ZipAESStream(Stream stream, ZipAESTransform transform, CryptoStreamMode mode)
			: base(stream, transform, mode)
		{
			_stream = stream;
			_transform = transform;
			_slideBuffer = new byte[1024];

			// mode:
			//  CryptoStreamMode.Read means we read from "stream" and pass decrypted to our Read() method.
			//  Write bypasses this stream and uses the Transform directly.
			if (mode != CryptoStreamMode.Read)
			{
				throw new Exception("ZipAESStream only for read");
			}
		}

		// The final n bytes of the AES stream contain the Auth Code.
		private const int AUTH_CODE_LENGTH = 10;

		// Blocksize is always 16 here, even for AES-256 which has transform.InputBlockSize of 32.
		private const int CRYPTO_BLOCK_SIZE = 16;

		// total length of block + auth code
		private const int BLOCK_AND_AUTH = CRYPTO_BLOCK_SIZE + AUTH_CODE_LENGTH;

		private Stream _stream;
		private ZipAESTransform _transform;
		private byte[] _slideBuffer;
		private int _slideBufStartPos;
		private int _slideBufFreePos;

		// Buffer block transforms to enable partial reads
		private byte[] _transformBuffer = null;// new byte[CRYPTO_BLOCK_SIZE];
		private int _transformBufferFreePos;
		private int _transformBufferStartPos;

		// Do we have some buffered data available?
		private bool HasBufferedData =>_transformBuffer != null && _transformBufferStartPos < _transformBufferFreePos;

		/// <summary>
		/// Reads a sequence of bytes from the current CryptoStream into buffer,
		/// and advances the position within the stream by the number of bytes read.
		/// </summary>
		public override int Read(byte[] buffer, int offset, int count)
		{
			// Nothing to do
			if (count == 0)
				return 0;

			// If we have buffered data, read that first
			int nBytes = 0;
			if (HasBufferedData)
			{
				nBytes = ReadBufferedData(buffer, offset, count);

				// Read all requested data from the buffer
				if (nBytes == count)
					return nBytes;

				offset += nBytes;
				count -= nBytes;
			}

			// Read more data from the input, if available
			if (_slideBuffer != null)
				nBytes += ReadAndTransform(buffer, offset, count);

			return nBytes;
		}

		/// <inheritdoc/>
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var readCount = Read(buffer, offset, count);
			return Task.FromResult(readCount);
		}

		// Read data from the underlying stream and decrypt it
		private int ReadAndTransform(byte[] buffer, int offset, int count)
		{
			int nBytes = 0;
			while (nBytes < count)
			{
				int bytesLeftToRead = count - nBytes;

				// Calculate buffer quantities vs read-ahead size, and check for sufficient free space
				int byteCount = _slideBufFreePos - _slideBufStartPos;

				// Need to handle final block and Auth Code specially, but don't know total data length.
				// Maintain a read-ahead equal to the length of (crypto block + Auth Code).
				// When that runs out we can detect these final sections.
				int lengthToRead = BLOCK_AND_AUTH - byteCount;
				if (_slideBuffer.Length - _slideBufFreePos < lengthToRead)
				{
					// Shift the data to the beginning of the buffer
					int iTo = 0;
					for (int iFrom = _slideBufStartPos; iFrom < _slideBufFreePos; iFrom++, iTo++)
					{
						_slideBuffer[iTo] = _slideBuffer[iFrom];
					}
					_slideBufFreePos -= _slideBufStartPos;      // Note the -=
					_slideBufStartPos = 0;
				}
				int obtained = StreamUtils.ReadRequestedBytes(_stream, _slideBuffer, _slideBufFreePos, lengthToRead);
				_slideBufFreePos += obtained;

				// Recalculate how much data we now have
				byteCount = _slideBufFreePos - _slideBufStartPos;
				if (byteCount >= BLOCK_AND_AUTH)
				{
					var read = TransformAndBufferBlock(buffer, offset, bytesLeftToRead, CRYPTO_BLOCK_SIZE);
					nBytes += read;
					offset += read;
				}
				else
				{
					// Last round.
					if (byteCount > AUTH_CODE_LENGTH)
					{
						// At least one byte of data plus auth code
						int finalBlock = byteCount - AUTH_CODE_LENGTH;
						nBytes += TransformAndBufferBlock(buffer, offset, bytesLeftToRead, finalBlock);
					}
					else if (byteCount < AUTH_CODE_LENGTH)
						throw new ZipException("Internal error missed auth code"); // Coding bug
																				// Final block done. Check Auth code.
					byte[] calcAuthCode = _transform.GetAuthCode();
					for (int i = 0; i < AUTH_CODE_LENGTH; i++)
					{
						if (calcAuthCode[i] != _slideBuffer[_slideBufStartPos + i])
						{
							throw new ZipException("AES Authentication Code does not match. This is a super-CRC check on the data in the file after compression and encryption. \r\n"
								+ "The file may be damaged.");
						}
					}

					// don't need this any more, so use it as a 'complete' flag
					_slideBuffer = null;

					break;  // Reached the auth code
				}
			}
			return nBytes;
		}

		// read some buffered data
		private int ReadBufferedData(byte[] buffer, int offset, int count)
		{
			int copyCount = Math.Min(count, _transformBufferFreePos - _transformBufferStartPos);

			Array.Copy(_transformBuffer, _transformBufferStartPos, buffer, offset, copyCount);
			_transformBufferStartPos += copyCount;

			return copyCount;
		}

		// Perform the crypto transform, and buffer the data if less than one block has been requested.
		private int TransformAndBufferBlock(byte[] buffer, int offset, int count, int blockSize)
		{
			// If the requested data is greater than one block, transform it directly into the output
			// If it's smaller, do it into a temporary buffer and copy the requested part
			bool bufferRequired = (blockSize > count);

			if (bufferRequired && _transformBuffer == null)
				_transformBuffer = new byte[CRYPTO_BLOCK_SIZE];

			var targetBuffer = bufferRequired ? _transformBuffer : buffer;
			var targetOffset = bufferRequired ? 0 : offset;

			// Transform the data
			_transform.TransformBlock(_slideBuffer,
									  _slideBufStartPos,
									  blockSize,
									  targetBuffer,
									  targetOffset);

			_slideBufStartPos += blockSize;

			if (!bufferRequired)
			{
				return blockSize;
			}
			else
			{
				Array.Copy(_transformBuffer, 0, buffer, offset, count);
				_transformBufferStartPos = count;
				_transformBufferFreePos = blockSize;

				return count;
			}
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
		/// <param name="offset">The byte offset in buffer at which to begin copying bytes to the current stream. </param>
		/// <param name="count">The number of bytes to be written to the current stream. </param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			// ZipAESStream is used for reading but not for writing. Writing uses the ZipAESTransform directly.
			throw new NotImplementedException();
		}
	}
}
