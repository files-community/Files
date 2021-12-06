using Files.Helpers;
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
        private Stream stream;
        private IInputStream iStream;
        public Action DisposeCallback { get; set; }

        public InputStreamWithDisposeCallback(Stream stream)
        {
            this.stream = stream;
            this.iStream = stream.AsInputStream();
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return iStream.ReadAsync(buffer, count, options);
        }

        public void Dispose()
        {
            iStream.Dispose();
            stream.Dispose();
            DisposeCallback?.Invoke();
        }
    }

    public class ProxiRandomAccessStream : IRandomAccessStream
    {
        private IRandomAccessStream imrac;
        private bool isWritten, isRead;
        private ulong byteSize, readPos;
        private AsyncManualResetEvent readFlag, writeFlag;

        public ProxiRandomAccessStream()
        {
            this.imrac = new InMemoryRandomAccessStream();
            this.readFlag = new AsyncManualResetEvent();
            this.writeFlag = new AsyncManualResetEvent();
            this.writeFlag.Set();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            if (position != 0)
            {
                throw new NotSupportedException();
            }
            return this;
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            if (position != 0)
            {
                throw new NotSupportedException();
            }
            return this;
        }

        public void Seek(ulong position)
        {
            if (position != 0)
            {
                throw new NotSupportedException();
            }
        }

        public IRandomAccessStream CloneStream() => throw new NotSupportedException();

        public bool CanRead => true;

        public bool CanWrite => true;

        public ulong Position => readPos;

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
                    if (!isWritten)
                    {
                        await readFlag.WaitAsync();
                    }
                    if (readPos >= byteSize)
                    {
                        isRead = true;
                        buffer.Length = 0;
                        return buffer;
                    }
                    var res = await imrac.ReadAsync(buffer, count, InputStreamOptions.Partial);
                    readPos += res.Length;
                    if (readPos >= byteSize)
                    {
                        readFlag.Reset();
                        writeFlag.Set();
                    }
                    return res;
                };

            return AsyncInfo.Run(taskProvider);
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            Func<CancellationToken, IProgress<uint>, Task<uint>> taskProvider =
                async (token, progress) =>
                {
                    await writeFlag.WaitAsync();
                    imrac.Seek(0);
                    imrac.Size = 0;
                    var res = await imrac.WriteAsync(buffer);
                    imrac.Seek(0);
                    byteSize += res;
                    writeFlag.Reset();
                    readFlag.Set();
                    return res;
                };

            return AsyncInfo.Run(taskProvider);
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            isWritten = true;

            return imrac.FlushAsync();
        }

        public void Dispose()
        {
            if (isWritten && isRead)
            {
                imrac.Dispose();
            }
        }
    }

    public class NonSeekableRandomAccessStreamForWrite : IRandomAccessStream
    {
        private Stream stream;
        private IOutputStream oStream;
        private IRandomAccessStream imrac;
        private ulong byteSize;
        private bool isWritten;

        public Action DisposeCallback { get; set; }

        public NonSeekableRandomAccessStreamForWrite(Stream stream)
        {
            this.stream = stream;
            this.oStream = stream.AsOutputStream();
            this.imrac = new InMemoryRandomAccessStream();
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            throw new NotSupportedException();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            if (position != 0)
            {
                throw new NotSupportedException();
            }
            return this;
        }

        public void Seek(ulong position)
        {
            if (position != 0)
            {
                throw new NotSupportedException();
            }
        }

        public IRandomAccessStream CloneStream() => throw new NotSupportedException();

        public bool CanRead => false;

        public bool CanWrite => true;

        public ulong Position => byteSize;

        public ulong Size
        {
            get => byteSize;
            set => throw new NotSupportedException();
        }

        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotSupportedException();
        }

        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            Func<CancellationToken, IProgress<uint>, Task<uint>> taskProvider =
                async (token, progress) =>
                {
                    var res = await oStream.WriteAsync(buffer);
                    byteSize += res;
                    return res;
                };

            return AsyncInfo.Run(taskProvider);
        }

        public IAsyncOperation<bool> FlushAsync()
        {
            if (isWritten)
            {
                return imrac.FlushAsync();
            }

            isWritten = true;

            return AsyncInfo.Run<bool>(async (cancellationToken) =>
            {
                await stream.FlushAsync();
                return true;
            });
        }

        public void Dispose()
        {
            oStream.Dispose();
            stream.Dispose();
            imrac.Dispose();
            DisposeCallback?.Invoke();
        }
    }

    public class NonSeekableRandomAccessStreamForRead : IRandomAccessStream
    {
        private Stream stream;
        private IRandomAccessStream imrac;
        private ulong virtualPosition;
        private ulong readToByte;
        private ulong byteSize;

        public Action DisposeCallback { get; set; }

        public NonSeekableRandomAccessStreamForRead(Stream baseStream, ulong size)
        {
            this.stream = baseStream;
            this.imrac = new InMemoryRandomAccessStream();
            this.virtualPosition = 0;
            this.readToByte = 0;
            this.byteSize = size;
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            Seek(position);
            return this;
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

        public IRandomAccessStream CloneStream() => throw new NotSupportedException();

        public bool CanRead => true;

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
