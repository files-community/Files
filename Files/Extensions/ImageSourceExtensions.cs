using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Files.Extensions
{
    internal static class ImageSourceExtensions
    {
        internal static async Task<byte[]> ToByteArrayAsync(this IInputStream stream)
        {
            if (stream == null)
            {
                return null;
            }

            using var readStream = stream.AsStreamForRead();
            return await readStream.ToByteArrayAsync();
        }

        private static async Task<byte[]> ToByteArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            MemoryStream memoryStream;
            if (stream.CanSeek)
            {
                var length = stream.Length - stream.Position;
                memoryStream = new MemoryStream((int)length);
            }
            else
            {
                memoryStream = new MemoryStream();
            }

            using (memoryStream)
            {
                await stream.CopyToAsync(memoryStream, bufferSize: 81920, cancellationToken).ConfigureAwait(false);
                return memoryStream.ToArray();
            }
        }
    }
}