using System;
using Files.Filesystem;
using Files.ViewModels;

namespace Files.Helpers
{
    public static class ListedItemHelpers
    {
        /// <summary>
        /// Check if the <see cref="ListedItem"/> is a image file.
        /// </summary>
        /// <param name="listedItem"></param>
        /// <returns><c>true</c> if the <see cref="ListedItem"/> is an image, otherwise <c>false</c></returns>
        public static bool IsImage(this ListedItem listedItem)
        {
            if (listedItem is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(listedItem.FileExtension))
            {
                return false;
            }

            return listedItem.FileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase) || 
                   listedItem.FileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || 
                   listedItem.FileExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) || 
                   listedItem.FileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
