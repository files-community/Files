using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Extensions
{
    internal static class ImageSourceExtensions
    {
        internal static async Task<byte[]> ToByteArrayAsync(this IInputStream stream)
        {
            var readStream = stream.AsStreamForRead();
            var byteArray = new byte[readStream.Length];
            await readStream.ReadAsync(byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}
