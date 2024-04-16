﻿using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.Helpers
{
    internal static class BitmapHelper
    {
        public static async Task<BitmapImage> ToBitmapAsync(this byte[] data, int decodeSize = -1)
        {
            if (data is null)
            {
                return null;
            }

            using var ms = new MemoryStream(data);
            var image = new BitmapImage();
            if (decodeSize > 0)
            {
                image.DecodePixelWidth = decodeSize;
                image.DecodePixelHeight = decodeSize;
            }
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

        /// <summary>
        /// This function encodes a software bitmap with the specified encoder and saves it to a file
        /// </summary>
        /// <param name="softwareBitmap"></param>
        /// <param name="outputFile"></param>
        /// <param name="encoderId">The guid of the image encoder type</param>
        /// <returns></returns>
        public static async Task SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, BaseStorageFile outputFile, Guid encoderId)
        {
            using IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
            // Create an encoder with the desired format
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, stream);

            // Set the software bitmap
            encoder.SetSoftwareBitmap(softwareBitmap);

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