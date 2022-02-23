using Files.Common;
using Files.Extensions;
using Files.Filesystem;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.Helpers
{
    public static class FileThumbnailHelper
    {
        public static async Task<(byte[] IconData, byte[] OverlayData)> LoadIconAndOverlayAsync(string filePath, uint thumbnailSize)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "GetIconOverlay" },
                    { "filePath", filePath },
                    { "thumbnailSize", (int)thumbnailSize },
                    { "isOverlayOnly", false }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);
                if (status == AppServiceResponseStatus.Success)
                {
                    var icon = response.Get("Icon", (string)null);
                    var overlay = response.Get("Overlay", (string)null);

                    // BitmapImage can only be created on UI thread, so return raw data and create
                    // BitmapImage later to prevent exceptions once SynchorizationContext lost
                    return (icon == null ? null : Convert.FromBase64String(icon),
                        overlay == null ? null : Convert.FromBase64String(overlay));
                }
            }
            return (null, null);
        }

        public static async Task<byte[]> LoadOverlayAsync(string filePath, uint thumbnailSize)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "GetIconOverlay" },
                    { "filePath", filePath },
                    { "thumbnailSize", thumbnailSize },
                    { "isOverlayOnly", true }
                };
                var (status, response) = await connection.SendMessageForResponseAsync(value);
                if (status == AppServiceResponseStatus.Success)
                {
                    var overlay = response.Get("Overlay", (string)null);

                    // BitmapImage can only be created on UI thread, so return raw data and create
                    // BitmapImage later to prevent exceptions once SynchorizationContext lost
                    return overlay == null ? null : Convert.FromBase64String(overlay);
                }
            }
            return null;
        }

        public static async Task<byte[]> LoadIconWithoutOverlayAsync(string filePath, uint thumbnailSize)
        {
            var Connection = await AppServiceConnectionHelper.Instance;
            if (Connection != null)
            {
                var value = new ValueSet();
                value.Add("Arguments", "GetIconWithoutOverlay");
                value.Add("filePath", filePath);
                value.Add("thumbnailSize", (int)thumbnailSize);
                var (status, response) = await Connection.SendMessageForResponseAsync(value);
                if (status == AppServiceResponseStatus.Success)
                {
                    var icon = response.Get("Icon", (string)null);

                    // BitmapImage can only be created on UI thread, so return raw data and create
                    // BitmapImage later to prevent exceptions once SynchorizationContext lost
                    return (icon == null ? null : Convert.FromBase64String(icon));
                }
            }
            return null;
        }

        public static async Task<byte[]> LoadIconFromStorageItemAsync(IStorageItem item, uint thumbnailSize, ThumbnailMode thumbnailMode)
        {
            if (item.IsOfType(StorageItemTypes.File))
            {
                using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(
                    () => item.AsBaseStorageFile().GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());
                if (thumbnail != null)
                {
                    return await thumbnail.ToByteArrayAsync();
                }
            }
            else if (item.IsOfType(StorageItemTypes.Folder))
            {
                using var thumbnail = (StorageItemThumbnail)await FilesystemTasks.Wrap(
                    () => item.AsBaseStorageFolder().GetThumbnailAsync(thumbnailMode, thumbnailSize, ThumbnailOptions.ResizeThumbnail).AsTask());
                if (thumbnail != null)
                {
                    return await thumbnail.ToByteArrayAsync();
                }
            }
            return null;
        }

        public static async Task<byte[]> LoadIconFromPathAsync(string filePath, uint thumbnailSize, ThumbnailMode thumbnailMode)
        {
            if (!filePath.EndsWith(".lnk", StringComparison.Ordinal) && !filePath.EndsWith(".url", StringComparison.Ordinal))
            {
                var item = await StorageHelpers.ToStorageItem<IStorageItem>(filePath);
                if (item != null)
                {
                    var iconData = await LoadIconFromStorageItemAsync(item, thumbnailSize, thumbnailMode);
                    if (iconData != null)
                    {
                        return iconData;
                    }
                }
            }
            return await LoadIconWithoutOverlayAsync(filePath, thumbnailSize);
        }
    }
}