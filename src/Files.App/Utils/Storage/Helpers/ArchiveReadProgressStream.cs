// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;

namespace Files.App.Utils.Storage
{
	internal sealed class ArchiveReadProgressStream : Stream
	{
		private readonly Stream _stream;
		private readonly Action<double> _reportProgress;
		private readonly CancellationToken _cancellationToken;
		private long _furthestReadPosition;
		private bool _isReporting;
		private bool _disposed;

		public override bool CanRead => _stream.CanRead;
		public override bool CanSeek => _stream.CanSeek;
		public override bool CanWrite => false;
		public override long Length => _stream.Length;
		public override long Position
		{
			get => _stream.Position;
			set => _stream.Position = value;
		}

		public ArchiveReadProgressStream(Stream stream, Action<double> reportProgress, CancellationToken cancellationToken)
		{
			_stream = stream;
			_reportProgress = reportProgress;
			_cancellationToken = cancellationToken;
		}

		public void StartReporting()
		{
			_furthestReadPosition = 0;
			_isReporting = true;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			int read = _stream.Read(buffer, offset, count);
			ReportRead(read);
			return read;
		}

		public override int Read(Span<byte> buffer)
		{
			_cancellationToken.ThrowIfCancellationRequested();
			int read = _stream.Read(buffer);
			ReportRead(read);
			return read;
		}

		public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
		public override void Flush() => _stream.Flush();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		private void ReportRead(int read)
		{
			if (!_isReporting || read == 0 || Length == 0 || Position <= _furthestReadPosition)
				return;

			_furthestReadPosition = Position;
			// Input can be buffered ahead of decompression and destination writes.
			_reportProgress(Math.Min(99d, _furthestReadPosition * 100d / Length));
		}

		protected override void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
					_stream.Dispose();

				_disposed = true;
			}

			base.Dispose(disposing);
		}
	}
}
