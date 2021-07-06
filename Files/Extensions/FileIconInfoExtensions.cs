using Files.Common;
using Files.Helpers;
using System;
using System.Threading.Tasks;

namespace Files.Extensions
{
    public static class FileIconInfoExtensions
    {
        public static async Task LoadImageFromModelString(this IconFileInfo info)
        {
            var dataBytes = Convert.FromBase64String(info.IconData);
            info.IconDataBytes = dataBytes;
            info.Image = await dataBytes.ToBitmapAsync();
        }
    }
}