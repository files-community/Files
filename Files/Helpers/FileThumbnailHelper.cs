using Files.Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

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

        public static async Task<byte[]> LoadOverlayAsync(string filePath)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection != null)
            {
                var value = new ValueSet
                {
                    { "Arguments", "GetIconOverlay" },
                    { "filePath", filePath },
                    { "thumbnailSize", 0 }, // Must pass in arbitrary int value for this to work
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
    }
}