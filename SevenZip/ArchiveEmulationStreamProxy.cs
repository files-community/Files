namespace SevenZip
{
    using System;
    using System.IO;

    /// <summary>
    /// The Stream extension class to emulate the archive part of a stream.
    /// </summary>
    internal class ArchiveEmulationStreamProxy : Stream, IDisposable
    {
        public bool LeaveOpen { get; }

        /// <summary>
        /// Gets the file offset.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The source wrapped stream.
        /// </summary>
        public Stream Source { get; }

        /// <summary>
        /// Initializes a new instance of the ArchiveEmulationStream class.
        /// </summary>
        /// <param name="stream">The stream to wrap.</param>
        /// <param name="offset">The stream offset.</param>
        public ArchiveEmulationStreamProxy(Stream stream, int offset, bool leaveOpen = false)
        {
            Source = stream;
            Offset = offset;
            Source.Position = offset;
            LeaveOpen = leaveOpen;
        }

        public override bool CanRead => Source.CanRead;

        public override bool CanSeek => Source.CanSeek;

        public override bool CanWrite => Source.CanWrite;

        public override void Flush()
        {
            Source.Flush();
        }

        public override long Length => Source.Length - Offset;

        public override long Position
        {
            get => Source.Position - Offset;
            set => Source.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Source.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Source.Seek(origin == SeekOrigin.Begin ? offset + Offset : offset,
                origin) - Offset;
        }

        public override void SetLength(long value)
        {
            Source.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Source.Write(buffer, offset, count);
        }

        public new void Dispose()
        {
            if (!LeaveOpen)
            {
                Source.Dispose();
            }
        }

        public override void Close()
        {
            if (!LeaveOpen)
            {
                Source.Close();
            }
        }
    }
}
