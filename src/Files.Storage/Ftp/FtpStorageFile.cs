// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Storage
{
	public sealed class FtpStorageFile : FtpStorable, IChildFile
	{
		public FtpStorageFile(string path, string name, IFolder? parent)
			: base(path, name, parent)
		{
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			var ftpClient = GetFtpClient();
			try
			{
				await ftpClient.EnsureConnectedAsync(cancellationToken);
				var ftpPath = FtpHelpers.GetFtpPath(Id);
				Stream stream;
				if (access.HasFlag(FileAccess.Write))
					stream = await ftpClient.OpenWrite(ftpPath, token: cancellationToken);
				else if (access.HasFlag(FileAccess.Read))
					stream = await ftpClient.OpenRead(ftpPath, token: cancellationToken);
				else
					throw new ArgumentException($"Invalid {nameof(access)} flag.");

				return new FtpClientStream(stream, ftpClient);
			}
			catch
			{
				ftpClient.Dispose();
				throw;
			}
		}

		private sealed class FtpClientStream(Stream stream, IDisposable owner) : Stream
		{
			private Stream? _stream = stream;
			private IDisposable? _owner = owner;

			private Stream Inner => _stream ?? throw new ObjectDisposedException(nameof(FtpClientStream));

			public override bool CanRead => _stream?.CanRead ?? false;
			public override bool CanSeek => _stream?.CanSeek ?? false;
			public override bool CanWrite => _stream?.CanWrite ?? false;
			public override long Length => Inner.Length;
			public override long Position { get => Inner.Position; set => Inner.Position = value; }

			public override void Flush() => Inner.Flush();
			public override Task FlushAsync(CancellationToken cancellationToken) => Inner.FlushAsync(cancellationToken);
			public override int Read(byte[] buffer, int offset, int count) => Inner.Read(buffer, offset, count);
			public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Inner.ReadAsync(buffer, offset, count, cancellationToken);
			public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => Inner.ReadAsync(buffer, cancellationToken);
			public override long Seek(long offset, SeekOrigin origin) => Inner.Seek(offset, origin);
			public override void SetLength(long value) => Inner.SetLength(value);
			public override void Write(byte[] buffer, int offset, int count) => Inner.Write(buffer, offset, count);
			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Inner.WriteAsync(buffer, offset, count, cancellationToken);
			public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => Inner.WriteAsync(buffer, cancellationToken);

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					try
					{
						_stream?.Dispose();
					}
					finally
					{
						_owner?.Dispose();
						_stream = null;
						_owner = null;
					}
				}

				base.Dispose(disposing);
			}

			public override async ValueTask DisposeAsync()
			{
				try
				{
					if (_stream is { } streamToDispose)
						await streamToDispose.DisposeAsync();
				}
				finally
				{
					_owner?.Dispose();
					_stream = null;
					_owner = null;
					GC.SuppressFinalize(this);
				}
			}
		}
	}
}
