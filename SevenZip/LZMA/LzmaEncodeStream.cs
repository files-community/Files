namespace SevenZip
{
    using System;
    using System.IO;

    using SevenZip.Sdk.Compression.Lzma;

    /// <summary>
    /// The stream which compresses data with LZMA on the fly.
    /// </summary>
    public class LzmaEncodeStream : Stream
    {
        private const int MAX_BUFFER_CAPACITY = 1 << 30; //1 Gb
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly int _bufferCapacity = 1 << 18; //256 kb
        private readonly bool _ownOutput;
        private bool _disposed;
        private Encoder _lzmaEncoder;
        private Stream _output;

        /// <summary>
        /// Initializes a new instance of the LzmaEncodeStream class.
        /// </summary>
        public LzmaEncodeStream()
        {
            _output = new MemoryStream();
            _ownOutput = true;
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the LzmaEncodeStream class.
        /// </summary>
        /// <param name="bufferCapacity">The buffer size. The bigger size, the better compression.</param>
        public LzmaEncodeStream(int bufferCapacity)
        {
            _output = new MemoryStream();
            _ownOutput = true;
            if (bufferCapacity > MAX_BUFFER_CAPACITY)
            {
                throw new ArgumentException("Too large capacity.", "bufferCapacity");
            }
            _bufferCapacity = bufferCapacity;
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the LzmaEncodeStream class.
        /// </summary>
        /// <param name="outputStream">An output stream which supports writing.</param>
        public LzmaEncodeStream(Stream outputStream)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("The specified stream can not write.", "outputStream");
            }
            _output = outputStream;
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the LzmaEncodeStream class.
        /// </summary>
        /// <param name="outputStream">An output stream which supports writing.</param>
        /// <param name="bufferCapacity">A buffer size. The bigger size, the better compression.</param>
        public LzmaEncodeStream(Stream outputStream, int bufferCapacity)
        {
            if (!outputStream.CanWrite)
            {
                throw new ArgumentException("The specified stream can not write.", "outputStream");
            }
            _output = outputStream;
            if (bufferCapacity > 1 << 30)
            {
                throw new ArgumentException("Too large capacity.", "bufferCapacity");
            }
            _bufferCapacity = bufferCapacity;
            Init();
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                DisposedCheck();
                return _buffer.CanWrite;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the output stream.
        /// </summary>
        public override long Length
        {
            get
            {
                DisposedCheck();

                if (_output.CanSeek)
                {
                    return _output.Length;
                }

                return _buffer.Position;
            }
        }

        /// <summary>
        /// Gets or sets the position within the output stream.
        /// </summary>
        public override long Position
        {
            get
            {
                DisposedCheck();

                if (_output.CanSeek)
                {
                    return _output.Position;
                }

                return _buffer.Position;
            }
            set => throw new NotSupportedException();
        }

        private void Init()
        {
            _buffer.Capacity = _bufferCapacity;
            SevenZipCompressor.LzmaDictionarySize = _bufferCapacity;
            _lzmaEncoder = new Encoder();
            SevenZipCompressor.WriteLzmaProperties(_lzmaEncoder);
        }

        /// <summary>
        /// Checked whether the class was disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException" />
        private void DisposedCheck()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("SevenZipExtractor");
            }
        }

        private void WriteChunk()
        {
            _lzmaEncoder.WriteCoderProperties(_output);
            long streamSize = _buffer.Position;
            if (_buffer.Length != _buffer.Position)
            {
                _buffer.SetLength(_buffer.Position);
            }
            _buffer.Position = 0;
            for (int i = 0; i < 8; i++)
            {
                _output.WriteByte((byte) (streamSize >> (8*i)));
            }
            _lzmaEncoder.Code(_buffer, _output, -1, -1, null);
            _buffer.Position = 0;
        }

        /// <summary>
        /// Converts the LzmaEncodeStream to the LzmaDecodeStream to read data.
        /// </summary>
        /// <returns></returns>
        public LzmaDecodeStream ToDecodeStream()
        {
            DisposedCheck();
            Flush();
            return new LzmaDecodeStream(_output);
        }

        /// <summary>
        /// Clears all buffers for this stream and causes any buffered data to be compressed and written.
        /// </summary>
        public override void Flush()
        {
            DisposedCheck();
            WriteChunk();
        }

        /// <summary>
        /// Releases all unmanaged resources used by LzmaEncodeStream.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Flush();
                    _buffer.Close();
                    if (_ownOutput)
                    {
                        _output.Dispose();
                    }
                    _output = null;
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            DisposedCheck();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            DisposedCheck();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Writes a sequence of bytes to the current stream and compresses it if necessary.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            DisposedCheck();
            int dataLength = Math.Min(buffer.Length - offset, count);
            while (_buffer.Position + dataLength >= _bufferCapacity)
            {
                int length = _bufferCapacity - (int) _buffer.Position;
                _buffer.Write(buffer, offset, length);
                offset = length + offset;
                dataLength -= length;
                WriteChunk();
            }
            _buffer.Write(buffer, offset, dataLength);
        }
    }
}
