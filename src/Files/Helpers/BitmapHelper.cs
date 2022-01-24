using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Files.Filesystem;
using Files.Filesystem.StorageItems;
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
                return;

            Guid encoderType;
            switch (file.FileType)
            {
                case ".jpeg" or ".jpg":
                    encoderType = BitmapEncoder.JpegEncoderId;
                    break;
                case ".png":
                    encoderType = BitmapEncoder.PngEncoderId;
                    break;
                case ".bmp":
                    encoderType = BitmapEncoder.BmpEncoderId;
                    break;
                case ".tiff":
                    encoderType = BitmapEncoder.TiffEncoderId;
                    break;
                case ".gif":
                    encoderType = BitmapEncoder.GifEncoderId;
                    break;
                default:
                    return;
            }

            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderType, stream);
            encoder.SetSoftwareBitmap(softwareBitmap);
            encoder.BitmapTransform.Rotation = rotation;
            encoder.IsThumbnailGenerated = true;

            try
            {
                await encoder.FlushAsync();
            }
            catch (Exception err)
            {
                const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                switch (err.HResult)
                {
                    case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                        // If the encoder does not support writing a thumbnail, then try again
                        // but disable thumbnail generation.
                        encoder.IsThumbnailGenerated = false;
                        break;
                    default:
                        throw;
                }
            }

            if (encoder.IsThumbnailGenerated == false)
            {
                await encoder.FlushAsync();
            }
        }
    }
}