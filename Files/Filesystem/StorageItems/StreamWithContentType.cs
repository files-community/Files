using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Files.Filesystem.StorageItems
{
    public class InputStreamWithDisposeCallback : IInputStream
    {
        private IInputStream stream;
        public Action DisposeCallback { get; set; }

        public InputStreamWithDisposeCallback(Stream stream)
        {
            this.stream = stream.AsInputStream();
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return stream.ReadAsync(buffer, count, options);
        }

        public void Dispose()
        {
            stream.Dispose();
            DisposeCallback?.Invoke();
        }
    }

    public class RandomAccessStreamWithFlushCallback : IRandomAccessStream
    {
        private IRandomAccessStream imrac;
        private bool isWritten;
        public Func<IRandomAccessStream, IAsyncOperation<bool>> FlushCallback { get; set; }
        public Action DisposeCallback { get; set; }

        public RandomAccessStreamWithFlushCallback()
        {
            this.imrac = new InMemoryRandomAccessStream();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            return imrac.GetInputStreamAt(position);
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            return imrac.GetOutputStreamAt(position);
        }

        public void Seek(ulong position)
        {
            imrac.Seek(position);
        }

        public IRandomAccessStream CloneStream()
        {
            return imrac.CloneStream();
        }

        public bool CanRead => imrac.CanRead;

        public bool CanWrite => imrac.CanWrite;

        public ulong Position => imrac.Position;

        public ulong Size { get => imrac.Size; set => imrac.Size = value; }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return imrac.ReadAsync(buffer, count, options);
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            return imrac.WriteAsync(buffer);
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            if (isWritten)
            {
                return imrac.FlushAsync();
            }

            isWritten = true;

            return FlushCallback(this) ?? imrac.FlushAsync();
        }

        public void Dispose()
        {
            imrac.Dispose();
            DisposeCallback?.Invoke();
        }
    }

    public class NonSeekableRandomAccessStream : IRandomAccessStream
    {
        private Stream stream;
        private IRandomAccessStream imrac;
        private ulong virtualPosition;
        private ulong readToByte;
        private ulong byteSize;

        public Action DisposeCallback { get; set; }

        public NonSeekableRandomAccessStream(Stream baseStream, ulong size)
        {
            this.stream = baseStream;
            this.imrac = new InMemoryRandomAccessStream();
            this.virtualPosition = 0;
            this.readToByte = 0;
            this.byteSize = size;
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            throw new NotSupportedException();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            throw new NotSupportedException();
        }

        public void Seek(ulong position)
        {
            imrac.Size = Math.Max(imrac.Size, position);
            this.virtualPosition = position;
        }

        public IRandomAccessStream CloneStream() => imrac.CloneStream();

        public bool CanRead => imrac.CanRead;

        public bool CanWrite => false;

        public ulong Position => virtualPosition;

        public ulong Size
        {
            get => byteSize;
            set => throw new NotSupportedException();
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            Func<CancellationToken, IProgress<uint>, Task<IBuffer>> taskProvider =
                async (token, progress) =>
                {
                    int read;
                    var tempBuffer = new byte[16384];
                    imrac.Seek(readToByte);
                    while (imrac.Position < virtualPosition + count)
                    {
                        read = await stream.ReadAsync(tempBuffer, 0, tempBuffer.Length);
                        if (read == 0)
                        {
                            break;
                        }
                        await imrac.WriteAsync(tempBuffer.AsBuffer(0, read));
                    }
                    readToByte = imrac.Position;

                    imrac.Seek(virtualPosition);
                    var res = await imrac.ReadAsync(buffer, count, options);
                    virtualPosition = imrac.Position;
                    return res;
                };

            return AsyncInfo.Run(taskProvider);
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            throw new NotSupportedException();
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            return imrac.FlushAsync();
        }

        public void Dispose()
        {
            stream.Dispose();
            imrac.Dispose();
            DisposeCallback?.Invoke();
        }
    }

    public class StreamWithContentType : IRandomAccessStreamWithContentType
    {
        private IRandomAccessStream baseStream;

        public StreamWithContentType(IRandomAccessStream stream)
        {
            baseStream = stream;
        }

        public IInputStream GetInputStreamAt(ulong position) => baseStream.GetInputStreamAt(position);

        public IOutputStream GetOutputStreamAt(ulong position) => baseStream.GetOutputStreamAt(position);

        public void Seek(ulong position) => baseStream.Seek(position);

        public IRandomAccessStream CloneStream() => baseStream.CloneStream();

        public bool CanRead => baseStream.CanRead;

        public bool CanWrite => baseStream.CanWrite;

        public ulong Position => baseStream.Position;

        public ulong Size { get => baseStream.Size; set => baseStream.Size = value; }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return baseStream.ReadAsync(buffer, count, options);
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => baseStream.WriteAsync(buffer);

        public IAsyncOperation<bool> FlushAsync() => baseStream.FlushAsync();

        public void Dispose()
        {
            baseStream.Dispose();
        }

        public string ContentType { get; set; } = "application/octet-stream";
    }
}
