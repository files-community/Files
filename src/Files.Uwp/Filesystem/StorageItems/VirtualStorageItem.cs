using Files.Uwp.Extensions;
using Files.Uwp.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using static Files.Uwp.Helpers.NativeFindStorageItemHelper;

namespace Files.Uwp.Filesystem.StorageItems
{
    /// <summary>
    /// Implements IStorageItem, allowing us to get an instance of IStorageItem for a ListedItem
    /// representing a standard filesystem item. As such, VirtualStorageItem does not support hidden, 
    /// shortcut, or link items.
    /// </summary>
    public class VirtualStorageItem : IStorageItem
    {
        private readonly ListedItem item;
        private static BasicProperties props;

        public Windows.Storage.FileAttributes Attributes => item.PrimaryItemAttribute == StorageItemTypes.File ? Windows.Storage.FileAttributes.Normal : Windows.Storage.FileAttributes.Directory;

        public DateTimeOffset DateCreated => item.ItemDateCreatedReal;

        public string Name => item.ItemName;

        public string Path => item.ItemPath;

        public VirtualStorageItem(ListedItem item)
        {
            this.item = item;
            SetBasicProperties();
        }

        public VirtualStorageItem(string path)
        {
            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path, findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);
            if (hFile.ToInt64() != -1)
            {
                this.item = GetListedItemFromFindData(findData);
                SetBasicProperties();
            }

            FindClose(hFile);
        }

        private async void SetBasicProperties()
        {
            if (props is null)
            {
                var streamedFile = await StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriter, null);
                props = await streamedFile.GetBasicPropertiesAsync();
            }
        }

        private async void StreamedFileWriter(StreamedFileDataRequest request)
        {
            try
            {
                using (var stream = request.AsStreamForWrite())
                using (var streamWriter = new StreamWriter(stream))
                {
                    await streamWriter.WriteLineAsync(Name);
                }
                request.Dispose();
            }
            catch (Exception)
            {
                request.FailAndClose(StreamedFileFailureMode.Incomplete);
            }
        }

        public IAsyncAction RenameAsync(string desiredName)
        {
            throw new NotImplementedException();
        }

        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            throw new NotImplementedException();
        }

        public IAsyncAction DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
        {
            return Task.FromResult(props).AsAsyncOperation();
        }

        public bool IsOfType(StorageItemTypes type)
        {
            return item.PrimaryItemAttribute == type;
        }

        private ListedItem GetListedItemFromFindData(WIN32_FIND_DATA findData)
        {
            bool isHidden = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.Hidden) == System.IO.FileAttributes.Hidden;

            // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
            bool isReparsePoint = ((System.IO.FileAttributes)findData.dwFileAttributes & System.IO.FileAttributes.ReparsePoint) == System.IO.FileAttributes.ReparsePoint;
            bool isSymlink = isReparsePoint && findData.dwReserved0 == NativeFileOperationsHelper.IO_REPARSE_TAG_SYMLINK;

            if (!(isHidden && isSymlink))
            {
                var itemPath = Path;
                var itemName = findData.cFileName;
                DateTime itemCreatedDate;
                StorageItemTypes itemPrimaryType;

                try
                {
                    FileTimeToSystemTime(ref findData.ftCreationTime, out SYSTEMTIME systemCreatedDateOutput);
                    itemCreatedDate = systemCreatedDateOutput.ToDateTime();
                }
                catch (ArgumentException)
                {
                    // Invalid date means invalid findData, do not add to list
                    return null;
                }

                itemPrimaryType = (System.IO.Path.HasExtension(Path)) ? StorageItemTypes.File : StorageItemTypes.Folder;

                return new ListedItem(null, DateTimeExtensions.GetDateFormat(Shared.Enums.TimeStyle.System))
                {
                    PrimaryItemAttribute = itemPrimaryType,
                    FileImage = null,
                    ItemNameRaw = itemName,
                    IsHiddenItem = isHidden,
                    ItemDateCreatedReal = itemCreatedDate,
                    ItemPath = itemPath,
                };
            }

            return null;
        }
    }
}
