namespace SevenZip
{
    using System;
    using System.IO;

    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// The stream which decompresses data with LZMA on the fly.
    /// </summary>
    public class LzmaDecodeStream : Stream
    {
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly Decoder _decoder = new Decoder();
        private readonly Stream _input;
        private byte[] _commonProperties;
        private bool _error;
        private bool _firstChunkRead;

        /// <summary>
        /// Initializes a new instance of the LzmaDecodeStream class.
        /// </summary>
        /// <param name="encodedStream">A compressed stream.</param>
        public LzmaDecodeStream(Stream encodedStream)
        {
            if (!encodedStream.CanRead)
            {
                throw new ArgumentException("The specified stream can not read.", "encodedStream");
            }
            _input = encodedStream;
        }

        /// <summary>
        /// Gets the chunk size.
        /// </summary>
        public int ChunkSize => (int) _buffer.Length;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Gets the length in bytes of the output stream.
        /// </summary>
        public override long Length
        {
            get
            {
                if (_input.CanSeek)
                {
                    return _input.Length;
                }

                return _buffer.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position within the output stream.
        /// </summary>
        public override long Position
        {
            get
            {
                if (_input.CanSeek)
                {
                    return _input.Position;
                }
                return _buffer.Position;
            }
            set => throw new NotSupportedException();
        }

        private void ReadChunk()
        {
            long size;
            byte[] properties;
            try
            {
                properties = SevenZipExtractor.GetLzmaProperties(_input, out size);
            }
            catch (LzmaException)
            {
                _error = true;
                return;
            }
            if (!_firstChunkRead)
            {
                _commonProperties = properties;
            }
            if (_commonProperties[0] != properties[0] ||
                _commonProperties[1] != properties[1] ||
                _commonProperties[2] != properties[2] ||
                _commonProperties[3] != properties[3] ||
                _commonProperties[4] != properties[4])
            {
                _error = true;
                return;
            }
            if (_buffer.Capacity < (int) size)
            {
                _buffer.Capacity = (int) size;
            }
            _buffer.SetLength(size);
            _decoder.SetDecoderProperties(properties);
            _buffer.Position = 0;
            _decoder.Code(
                _input, _buffer, 0, size, null);
            _buffer.Position = 0;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override void Flush() {}

        /// <summary>
        /// Reads a sequence of bytes from the current stream and decompresses data if necessary.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_error)
            {
                return 0;
            }

            if (!_firstChunkRead)
            {
                ReadChunk();
                _firstChunkRead = true;
            }
            int readCount = 0;
            while (count > _buffer.Length - _buffer.Position && !_error)
            {
                var buf = new byte[_buffer.Length - _buffer.Position];
                _buffer.Read(buf, 0, buf.Length);
                buf.CopyTo(buffer, offset);
                offset += buf.Length;
                count -= buf.Length;
                readCount += buf.Length;
                ReadChunk();
            }
            if (!_error)
            {
                _buffer.Read(buffer, offset, count);
                readCount += count;
            }
            return readCount;
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>       
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
