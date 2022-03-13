using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Files.Backend.Item.Tools
{
    internal static class ImageSourceConverter
    {
        public static async Task<byte[]> ToByteArrayAsync(this IInputStream stream)
        {
            using var readStream = stream.AsStreamForRead();
            return await ToByteArrayAsync(readStream);
        }

        private static async Task<byte[]> ToByteArrayAsync(Stream stream)
        {
            var memoryStream = ToMemoryStream(stream);
            using (memoryStream)
            {
                await stream.CopyToAsync(memoryStream, bufferSize: 81920).ConfigureAwait(false);
                return memoryStream.ToArray();
            }
        }

        private static MemoryStream ToMemoryStream(Stream stream)
        {
            if (!stream.CanSeek)
            {
                return new MemoryStream();
            }

            long length = stream.Length - stream.Position;
            return new MemoryStream((int)length);
        }
    }
}
