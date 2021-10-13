using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Helpers
{
    public static class StorageItemIconHelpers
    {
        private const string CachedEmptyItemName = "cachedEmpty";

        public enum IconPersistenceOptions
        {
            LoadOnce,
            Persist
        }

        /// <summary>
        /// Retrieves the default non-thumbnail icon for a provided item type
        /// </summary>
        /// <param name="requestedSize">Desired size of icon</param>
        /// <param name="persistenceOptions">Optionally choose not to persist icon-backing item in LocalCache</param>
        /// <param name="fileExtension">The file type (extension) of the generic icon to retrieve. Leave empty if a directory icon is desired</param>
        /// <returns></returns>
        public static async Task<StorageItemThumbnail> GetIconForItemType(uint requestedSize, IconPersistenceOptions persistenceOptions = IconPersistenceOptions.Persist, string fileExtension = null)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
                return await localFolder.GetThumbnailAsync(ThumbnailMode.ListView, requestedSize, ThumbnailOptions.UseCurrentScale);
            }
            else
            {
                StorageFile emptyFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(string.Join(CachedEmptyItemName, fileExtension), CreationCollisionOption.OpenIfExists);
                var icon = await emptyFile.GetThumbnailAsync(ThumbnailMode.ListView, requestedSize, ThumbnailOptions.UseCurrentScale);
                
                if (persistenceOptions == IconPersistenceOptions.LoadOnce)
                {
                    await emptyFile.DeleteAsync();
                }

                return icon;
            }
        }
    }
}
