using System;
using System.Collections.Generic;
using System.Linq;
using Files.Filesystem;

namespace Files.Helpers
{
    public static class ListedItemHelpers
    {
        /// <summary>
        /// Check if the <see cref="ListedItem"/> is a image file.
        /// </summary>
        /// <param name="listedItem">The <see cref="ListedItem"/> to check the file extension of.</param>
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

        /// <summary>
        /// Check if the first <see cref="ListedItem"/> in the list is a image file.
        /// </summary>
        /// <param name="listedItems">List of <see cref="ListedItem"/>s.</param>
        /// <returns><c>true</c> if the <see cref="ListedItem"/> is an image, otherwise <c>false</c></returns>
        public static bool IsImage(this IReadOnlyList<ListedItem> listedItems)
        {
            if (listedItems is null)
            {
                return false;
            }

            return listedItems.Any() && listedItems.First().IsImage();
        }
    }
}
