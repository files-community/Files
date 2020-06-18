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
using static Files.Helpers.NativeFindStorageItemHelper;
using System.IO;
using Windows.UI.Core;
using FileAttributes = System.IO.FileAttributes;

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
        private Visibility _ItemSizeProgressVisibility;

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
            set => Set(ref _ItemMD5Hash, value);
        }
        public Visibility _ItemMD5HashVisibility;

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => Set(ref _ItemMD5HashVisibility, value);
        }
        public Visibility _ItemMD5HashProgressVisibiity;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibiity;
            set => Set(ref _ItemMD5HashProgressVisibiity, value);
        }
        #endregion
        #region Properties
        public Microsoft.UI.Xaml.Controls.ProgressBar ItemMD5HashProgress
        {
            get; set;
        }
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
            var fileSizeTask = Task.Run(async () =>
            {
                var size = await CalculateFolderSizeAsync(storageFolder.Path, token);
                return size;
            });
            try
            {
                var folderSize = await fileSizeTask;
                ItemSizeReal = folderSize;
                ItemsSize = ByteSizeLib.ByteSize.FromBytes(folderSize).ToString();
                ItemSizeProgressVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, ex.Message);
                SizeCalcError = true;
            }
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
                    if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) != FileAttributes.Hidden && ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) != FileAttributes.System)
                    {
                        if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
                        {
                            if (!findData.cFileName.EndsWith(".lnk") && !findData.cFileName.EndsWith(".url"))
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
                            }
                        }
                        else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if (findData.cFileName != "." && findData.cFileName != "..")
                            {
                                var itemPath = Path.Combine(path, findData.cFileName);

                                size += await CalculateFolderSizeAsync(itemPath, token);
                                ++count;
                            }
                        }
                    }

                    if (size > ItemSizeReal)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            ItemSizeReal = size;
                            ItemsSize = ByteSizeLib.ByteSize.FromBytes(size).ToString();
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
            if (App.CurrentInstance.ContentPage.IsItemSelected)
            {
                IStorageItem selectedStorageItem = null;

                if (Item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    var file = await StorageFile.GetFileFromPathAsync(Item.ItemPath);
                    selectedStorageItem = file;
                    GetOtherPropeties(file.Properties);
                    ItemsSize = Item.FileSize;
                }
                else if (Item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    var storageFolder = await StorageFolder.GetFolderFromPathAsync(Item.ItemPath);
                    selectedStorageItem = storageFolder;
                    GetOtherPropeties(storageFolder.Properties);
                    GetFolderSize(storageFolder, _tokenSource.Token);
                }
                ItemCreatedTimestamp = ListedItem.GetFriendlyDate(selectedStorageItem.DateCreated);
                if (Item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    // Get file MD5 hash
                    var hashAlgTypeName = HashAlgorithmNames.Md5;
                    ItemMD5HashProgressVisibility = Visibility.Visible;
                    ItemMD5Hash = await App.CurrentInstance.InteractionOperations.GetHashForFile(Item, hashAlgTypeName, _tokenSource.Token, ItemMD5HashProgress);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                    ItemMD5HashVisibility = Visibility.Visible;
                }
                else if (Item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    ItemMD5HashVisibility = Visibility.Collapsed;
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                var parentDirectory = App.CurrentInstance.ViewModel.CurrentFolder;
                if (parentDirectory.ItemPath.StartsWith(App.AppSettings.RecycleBinPath))
                {
                    // GetFolderFromPathAsync cannot access recyclebin folder
                    // Currently a fake timestamp is used                
                }
                else
                {
                    var parentDirectoryStorageItem = await StorageFolder.GetFolderFromPathAsync(parentDirectory.ItemPath);
                    ItemCreatedTimestamp = ListedItem.GetFriendlyDate(parentDirectoryStorageItem.DateCreated);
                }

                ItemMD5HashVisibility = Visibility.Collapsed;
                ItemMD5HashProgressVisibility = Visibility.Collapsed;
            }
        }
        #endregion
    }    
}