using Files.DataModels;
using Files.Filesystem;
using GalaSoft.MvvmLight;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.UI.Core;
using FileAttributes = System.IO.FileAttributes;
using Files.Helpers;
using static Files.Helpers.NativeFindStorageItemHelper;

namespace Files.View_Models
{
    public class SelectedItemsPropertiesViewModel : ViewModelBase
    {
        #region ItemProperties
        private string _ItemName;
        public string ItemName
        {
            get => _ItemName;
            set => Set(ref _ItemName, value);
        }
        private string _ItemType;
        public string ItemType
        {
            get => _ItemType;
            set => Set(ref _ItemType, value);
        }
        private string _ItemPath;
        public string ItemPath
        {
            get => _ItemPath;
            set => Set(ref _ItemPath, value);
        }
        private long _ItemSizeReal;

        public long ItemSizeReal
        {
            get => _ItemSizeReal;
            set => Set(ref _ItemSizeReal, value);
        }
        private Visibility _ItemSizeProgressVisibility = Visibility.Collapsed;

        public Visibility ItemSizeProgressVisibility
        {
            get => _ItemSizeProgressVisibility;
            set => Set(ref _ItemSizeProgressVisibility, value);
        }
        private bool _SizeCalcError;

        public bool SizeCalcError
        {
            get => _SizeCalcError;
            set => Set(ref _SizeCalcError, value);
        }
        private string _ItemModifiedTimestamp;
        public string ItemModifiedTimestamp
        {
            get => _ItemModifiedTimestamp;
            set => Set(ref _ItemModifiedTimestamp, value);
        }
        private ImageSource _FileIconSource;
        public ImageSource FileIconSource
        {
            get => _FileIconSource;
            set => Set(ref _FileIconSource, value);
        }
        private bool _LoadFolderGlyph;
        public bool LoadFolderGlyph
        {
            get => _LoadFolderGlyph;
            set => Set(ref _LoadFolderGlyph, value);
        }
        private bool _LoadUnknownTypeGlyph;
        public bool LoadUnknownTypeGlyph
        {
            get => _LoadUnknownTypeGlyph;
            set => Set(ref _LoadUnknownTypeGlyph, value);
        }
        private bool _LoadFileIcon;
        public bool LoadFileIcon
        {
            get => _LoadFileIcon;
            set => Set(ref _LoadFileIcon, value);
        }

        private string _ItemsSize;

        public string ItemsSize
        {
            get => _ItemsSize;
            set => Set(ref _ItemsSize, value);
        }

        private string _SelectedItemsCount;

        public string SelectedItemsCount
        {
            get => _SelectedItemsCount;
            set => Set(ref _SelectedItemsCount, value);
        }

        private bool _IsItemSelected;

        public bool IsItemSelected
        {
            get => _IsItemSelected;
            set => Set(ref _IsItemSelected, value);
        }
        private string _ItemCreatedTimestamp;

        public string ItemCreatedTimestamp
        {
            get => _ItemCreatedTimestamp;
            set => Set(ref _ItemCreatedTimestamp, value);
        }
        public string _ItemAccessedTimestamp;
        public string ItemAccessedTimestamp
        {
            get => _ItemAccessedTimestamp;
            set => Set(ref _ItemAccessedTimestamp, value);
        }
        public string _ItemFileOwner;
        public string ItemFileOwner
        {
            get => _ItemFileOwner;
            set => Set(ref _ItemFileOwner, value);
        }
        public string _ItemMD5Hash;
        public string ItemMD5Hash
        {
            get => _ItemMD5Hash;
            set
            {
                if (!string.IsNullOrEmpty(value) && value != _ItemMD5Hash)
                {
                    Set(ref _ItemMD5Hash, value);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
        }
        private bool _ItemMD5HashCalcError;
        public bool ItemMD5HashCalcError
        {
            get => _ItemMD5HashCalcError;
            set => Set(ref _ItemMD5HashCalcError, value);
        }

        public Visibility _ItemMD5HashVisibility = Visibility.Collapsed;

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => Set(ref _ItemMD5HashVisibility, value);
        }
        public Visibility _ItemMD5HashProgressVisibiity = Visibility.Collapsed;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibiity;
            set => Set(ref _ItemMD5HashProgressVisibiity, value);
        }

        public int _FoldersCount;

        public int FoldersCount
        {
            get => _FoldersCount;
            set => Set(ref _FoldersCount, value);
        }

        public int _FilesCount;

        public int FilesCount
        {
            get => _FilesCount;
            set => Set(ref _FilesCount, value);
        }

        public string _FilesAndFoldersCountString;

        public string FilesAndFoldersCountString
        {
            get => _FilesAndFoldersCountString;
            set => Set(ref _FilesAndFoldersCountString, value);
        }

        public Visibility _FilesAndFoldersCountVisibility = Visibility.Collapsed;

        public Visibility FilesAndFoldersCountVisibility
        {
            get => _FilesAndFoldersCountVisibility;
            set => Set(ref _FilesAndFoldersCountVisibility, value);
        }

        #endregion
        #region Properties

        public Microsoft.UI.Xaml.Controls.ProgressBar ItemMD5HashProgress { get; set; }

        public ListedItem Item { get; }

        public CoreDispatcher Dispatcher { get; set; }
        #endregion
        #region Constructors
        public SelectedItemsPropertiesViewModel(ListedItem item)
        {
            Item = item;

            ItemName = Item?.ItemName;
            ItemType = Item?.ItemType;
            ItemPath = Item?.ItemPath;
            ItemModifiedTimestamp = Item?.ItemDateModified;
            FileIconSource = Item?.FileImage;
            LoadFolderGlyph = Item != null ? Item.LoadFolderGlyph : false;
            LoadUnknownTypeGlyph = Item != null ? Item.LoadUnknownTypeGlyph : false;
            LoadFileIcon = Item != null ? Item.LoadFileIcon : false;
        }
        #endregion
        #region Methods
        public async void GetOtherPropeties(StorageItemContentProperties properties)
        {
            string dateAccessedProperty = "System.DateAccessed";
            string fileOwnerProperty = "System.FileOwner";
            List<string> propertiesName = new List<string>();
            propertiesName.Add(dateAccessedProperty);
            propertiesName.Add(fileOwnerProperty);
            IDictionary<string, object> extraProperties = await properties.RetrievePropertiesAsync(propertiesName);
            ItemAccessedTimestamp = ListedItem.GetFriendlyDate((DateTimeOffset)extraProperties[dateAccessedProperty]);
            ItemFileOwner = extraProperties[fileOwnerProperty].ToString();
        }
        private async void GetFolderSize(StorageFolder storageFolder, CancellationToken token)
        {
            ItemSizeProgressVisibility = Visibility.Visible;

            var fileSizeTask = Task.Run(async () =>
            {
                var size = await CalculateFolderSizeAsync(storageFolder.Path, token);
                return size;
            });
            try
            {
                var folderSize = await fileSizeTask;
                ItemSizeReal = folderSize;
                ItemsSize = ByteSizeLib.ByteSize.FromBytes(folderSize).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSizeLib.ByteSize.FromBytes(folderSize).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                SizeCalcError = true;
            }
            ItemSizeProgressVisibility = Visibility.Collapsed;
            SetItemsCountString();
        }
        public async Task<long> CalculateFolderSizeAsync(string path, CancellationToken token)
        {
            long size = 0;

            FINDEX_INFO_LEVELS findInfoLevel = FINDEX_INFO_LEVELS.FindExInfoBasic;
            int additionalFlags = FIND_FIRST_EX_LARGE_FETCH;

            IntPtr hFile = FindFirstFileExFromApp(path + "\\*.*", findInfoLevel, out WIN32_FIND_DATA findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero,
                                                  additionalFlags);

            var count = 0;
            if (hFile.ToInt64() != -1)
            {
                do
                {
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                    {
                        long fDataFSize = findData.nFileSizeLow;
                        long fileSize;
                        if (fDataFSize < 0 && findData.nFileSizeHigh > 0)
                        {
                            fileSize = fDataFSize + 4294967296 + (findData.nFileSizeHigh * 4294967296);
                        }
                        else
                        {
                            if (findData.nFileSizeHigh > 0)
                            {
                                fileSize = fDataFSize + (findData.nFileSizeHigh * 4294967296);
                            }
                            else if (fDataFSize < 0)
                            {
                                fileSize = fDataFSize + 4294967296;
                            }
                            else
                            {
                                fileSize = fDataFSize;
                            }
                        }
                        size += fileSize;
                        ++count;
                        FilesCount++;
                    }
                    else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (findData.cFileName != "." && findData.cFileName != "..")
                        {
                            var itemPath = Path.Combine(path, findData.cFileName);

                            size += await CalculateFolderSizeAsync(itemPath, token);
                            ++count;
                            FoldersCount++;
                        }
                    }

                    if (size > ItemSizeReal)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            ItemSizeReal = size;
                            ItemsSize = ByteSizeLib.ByteSize.FromBytes(size).ToBinaryString().ConvertSizeAbbreviation();
                            SetItemsCountString();
                        });
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                } while (FindNextFile(hFile, out findData));
                FindClose(hFile);
                return size;
            }
            else
            {
                return 0;
            }
        }
        public async Task GetPropertiesAsync(CancellationTokenSource _tokenSource)
        {
            if (Item.PrimaryItemAttribute == StorageItemTypes.File)
            {
                var file = await StorageFile.GetFileFromPathAsync(Item.ItemPath);
                ItemCreatedTimestamp = ListedItem.GetFriendlyDate(file.DateCreated);
                GetOtherPropeties(file.Properties);
                ItemsSize = ByteSizeLib.ByteSize.FromBytes(Item.FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSizeLib.ByteSize.FromBytes(Item.FileSizeBytes).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";

                // Get file MD5 hash
                var hashAlgTypeName = HashAlgorithmNames.Md5;
                ItemMD5HashProgressVisibility = Visibility.Visible;
                ItemMD5HashVisibility = Visibility.Visible;
                try
                {
                    ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(Item, hashAlgTypeName, _tokenSource.Token, ItemMD5HashProgress);
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                    ItemMD5HashCalcError = true;
                }
            }
            else if (Item.PrimaryItemAttribute == StorageItemTypes.Folder)
            {
                StorageFolder storageFolder = null;
                if (App.CurrentInstance.ContentPage.IsItemSelected)
                {
                    storageFolder = await StorageFolder.GetFolderFromPathAsync(Item.ItemPath);
                }
                else
                {
                    var parentDirectory = App.CurrentInstance.FilesystemViewModel.CurrentFolder;
                    if (parentDirectory.ItemPath.StartsWith(App.AppSettings.RecycleBinPath))
                    {
                        // GetFolderFromPathAsync cannot access recyclebin folder
                        // Currently a fake timestamp is used                
                    }
                    else
                    {
                        storageFolder = await StorageFolder.GetFolderFromPathAsync(parentDirectory.ItemPath);
                    }
                }
                ItemCreatedTimestamp = ListedItem.GetFriendlyDate(storageFolder.DateCreated);
                GetOtherPropeties(storageFolder.Properties);
                GetFolderSize(storageFolder, _tokenSource.Token);
            }
        }

        private void SetItemsCountString()
        {
            FilesAndFoldersCountString = string.Format(ResourceController.GetTranslation("PropertiesFilesAndFoldersCountString"), FilesCount, FoldersCount);
            FilesAndFoldersCountVisibility = Visibility.Visible;
        }
        #endregion
    }
}