using Files.Filesystem;
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
        /// <remarks>
        /// https://docs.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapdecoder?view=winrt-22000
        /// https://docs.microsoft.com/en-us/uwp/api/windows.graphics.imaging.bitmapencoder?view=winrt-22000
        /// </remarks>
        public static async Task Rotate(string filePath, BitmapRotation rotation)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            var file = await StorageHelpers.ToStorageItem<IStorageFile>(filePath);
            if (file == null)
            {
                return;
            }

            var fileStreamRes = await FilesystemTasks.Wrap(() => file.OpenAsync(FileAccessMode.ReadWrite).AsTask());
            using IRandomAccessStream fileStream = fileStreamRes.Result;
            if (fileStream == null)
            {
                return;
            }

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
            using var memStream = new InMemoryRandomAccessStream();
            BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(memStream, decoder);

            encoder.BitmapTransform.Rotation = rotation;

            await encoder.FlushAsync();

            memStream.Seek(0);
            fileStream.Seek(0);
            fileStream.Size = 0;

            await RandomAccessStream.CopyAsync(memStream, fileStream);
        }
    }
}