using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    internal static class BitmapHelper
    {
        public static async Task<BitmapImage> ToBitmapAsync(this byte[] data)
        {
            if (data is null)
            {
                return null;
            }

            using var ms = new MemoryStream(data);
            var image = new BitmapImage();
            await image.SetSourceAsync(ms.AsRandomAccessStream());
            return image;
        }


        /// <summary>
        /// Rotates the image at the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to the image.</param>
        /// <param name="rotation">The rotation direction.</param>
        public static async Task Rotate(string filePath, BitmapRotation rotation)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var file = await StorageFile.GetFileFromPathAsync(filePath);

            using IRandomAccessStream fs = await file.OpenAsync(FileAccessMode.ReadWrite), ms = new InMemoryRandomAccessStream();

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fs);
            BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(ms, decoder);

            encoder.BitmapTransform.Rotation = rotation;

            await encoder.FlushAsync();
            await RandomAccessStream.CopyAsync(ms, fs);
        }
    }
}