using System.IO;
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
            var readStream = stream.AsStreamForRead();
            var byteArray = new byte[readStream.Length];
            await readStream.ReadAsync(byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}