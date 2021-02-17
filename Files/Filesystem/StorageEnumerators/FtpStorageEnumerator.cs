using ByteSizeLib;
using Files.Extensions;
using FluentFTP;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.Filesystem.StorageEnumerators
{
    public static class FtpStorageEnumerator
    {
        private static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
        {
            var utc = dateTime.ToUniversalTime();
            if (utc <= DateTimeOffset.MinValue.UtcDateTime)
            {
                return DateTimeOffset.MinValue;
            }
            if (utc >= DateTimeOffset.MaxValue.UtcDateTime)
            {
                return DateTimeOffset.MaxValue;
            }
            return new DateTimeOffset(dateTime);
        }

        public static async Task<List<ListedItem>> ListEntries(
               string address,
               string path,
               string returnformat,
               IFtpClient ftpClient,
               CancellationToken cancellationToken
           )
        {
            var tempList = new List<ListedItem>();

            if (!ftpClient.IsConnected)
            {
                await ftpClient.AutoConnectAsync(cancellationToken);
            }

            var items = await ftpClient.GetListingAsync(path, FtpListOption.Auto, cancellationToken);

            foreach (var i in items)
            {
                string itemType;
                string itemFileExtension = null;

                if (i.Type == FtpFileSystemObjectType.Directory)
                {
                    itemType = "FileFolderListItem".GetLocalized();
                }
                else
                {
                    itemType = "ItemTypeFile".GetLocalized();
                    if (i.Name.Contains('.'))
                    {
                        itemFileExtension = Path.GetExtension(i.Name);
                        itemType = itemFileExtension.Trim('.') + " " + itemType;
                    }
                }

                var item = new ListedItem(null, returnformat)
                {
                    PrimaryItemAttribute = i.Type == FtpFileSystemObjectType.Directory ? StorageItemTypes.Folder : StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    LoadFolderGlyph = i.Type == FtpFileSystemObjectType.Directory,
                    LoadUnknownTypeGlyph = i.Type != FtpFileSystemObjectType.Directory,
                    ItemName = i.Name,
                    IsHiddenItem = false,
                    Opacity = 1,
                    ItemDateCreatedReal = i.Created.ToDateTimeOffset(),
                    ItemDateModifiedReal = i.Modified.ToDateTimeOffset(),
                    ItemType = itemType,
                    FileSizeBytes = i.Size,
                    FileSize = ByteSize.FromBytes(i.Size).ToBinaryString().ConvertSizeAbbreviation(),
                    ItemPath = address + i.FullName,
                    ItemPropertiesInitialized = true
                };

                tempList.Add(item);
            }

            return tempList;
        }
    }
}
