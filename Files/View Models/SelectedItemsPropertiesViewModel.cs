using ByteSizeLib;
using Files.Helpers;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SelectedItemsPropertiesViewModel : ObservableObject
    {
        private bool _LoadFolderGlyph;

        public bool LoadFolderGlyph
        {
            get => _LoadFolderGlyph;
            set => SetProperty(ref _LoadFolderGlyph, value);
        }

        private bool _LoadUnknownTypeGlyph;

        public bool LoadUnknownTypeGlyph
        {
            get => _LoadUnknownTypeGlyph;
            set => SetProperty(ref _LoadUnknownTypeGlyph, value);
        }

        private bool _LoadCombinedItemsGlyph;

        public bool LoadCombinedItemsGlyph
        {
            get => _LoadCombinedItemsGlyph;
            set => SetProperty(ref _LoadCombinedItemsGlyph, value);
        }

        private string _DriveItemGlyphSource;

        public string DriveItemGlyphSource
        {
            get => _DriveItemGlyphSource;
            set => SetProperty(ref _DriveItemGlyphSource, value);
        }

        private bool _LoadDriveItemGlyph;

        public bool LoadDriveItemGlyph
        {
            get => _LoadDriveItemGlyph;
            set => SetProperty(ref _LoadDriveItemGlyph, value);
        }

        private bool _LoadFileIcon;

        public bool LoadFileIcon
        {
            get => _LoadFileIcon;
            set => SetProperty(ref _LoadFileIcon, value);
        }

        private ImageSource _FileIconSource;

        public ImageSource FileIconSource
        {
            get => _FileIconSource;
            set => SetProperty(ref _FileIconSource, value);
        }

        private string _ItemName;

        public string ItemName
        {
            get => _ItemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref _ItemName, value);
            }
        }

        private string _OriginalItemName;

        public string OriginalItemName
        {
            get => _OriginalItemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref _OriginalItemName, value);
            }
        }

        private Visibility _ItemNameVisibility = Visibility.Collapsed;

        public Visibility ItemNameVisibility
        {
            get => _ItemNameVisibility;
            set => SetProperty(ref _ItemNameVisibility, value);
        }

        private string _ItemType;

        public string ItemType
        {
            get => _ItemType;
            set
            {
                ItemTypeVisibility = Visibility.Visible;
                SetProperty(ref _ItemType, value);
            }
        }

        private Visibility _ItemTypeVisibility = Visibility.Collapsed;

        public Visibility ItemTypeVisibility
        {
            get => _ItemTypeVisibility;
            set => SetProperty(ref _ItemTypeVisibility, value);
        }

        private string _DriveFileSystem;

        public string DriveFileSystem
        {
            get => _DriveFileSystem;
            set
            {
                DriveFileSystemVisibility = Visibility.Visible;
                SetProperty(ref _DriveFileSystem, value);
            }
        }

        private Visibility _DriveFileSystemVisibility = Visibility.Collapsed;

        public Visibility DriveFileSystemVisibility
        {
            get => _DriveFileSystemVisibility;
            set => SetProperty(ref _DriveFileSystemVisibility, value);
        }

        private string _ItemPath;

        public string ItemPath
        {
            get => _ItemPath;
            set
            {
                ItemPathVisibility = Visibility.Visible;
                SetProperty(ref _ItemPath, value);
            }
        }

        private Visibility _ItemPathVisibility = Visibility.Collapsed;

        public Visibility ItemPathVisibility
        {
            get => _ItemPathVisibility;
            set => SetProperty(ref _ItemPathVisibility, value);
        }

        private string _ItemSize;

        public string ItemSize
        {
            get => _ItemSize;
            set => SetProperty(ref _ItemSize, value);
        }

        private Visibility _ItemSizeVisibility = Visibility.Collapsed;

        public Visibility ItemSizeVisibility
        {
            get => _ItemSizeVisibility;
            set => SetProperty(ref _ItemSizeVisibility, value);
        }

        private long _ItemSizeBytes;

        public long ItemSizeBytes
        {
            get => _ItemSizeBytes;
            set => SetProperty(ref _ItemSizeBytes, value);
        }

        private Visibility _ItemSizeProgressVisibility = Visibility.Collapsed;

        public Visibility ItemSizeProgressVisibility
        {
            get => _ItemSizeProgressVisibility;
            set => SetProperty(ref _ItemSizeProgressVisibility, value);
        }

        public string _ItemMD5Hash;

        public string ItemMD5Hash
        {
            get => _ItemMD5Hash;
            set
            {
                if (!string.IsNullOrEmpty(value) && value != _ItemMD5Hash)
                {
                    SetProperty(ref _ItemMD5Hash, value);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
        }

        private bool _ItemMD5HashCalcError;

        public bool ItemMD5HashCalcError
        {
            get => _ItemMD5HashCalcError;
            set => SetProperty(ref _ItemMD5HashCalcError, value);
        }

        public Visibility _ItemMD5HashVisibility = Visibility.Collapsed;

        public Visibility ItemMD5HashVisibility
        {
            get => _ItemMD5HashVisibility;
            set => SetProperty(ref _ItemMD5HashVisibility, value);
        }

        public Visibility _ItemMD5HashProgressVisibiity = Visibility.Collapsed;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => _ItemMD5HashProgressVisibiity;
            set => SetProperty(ref _ItemMD5HashProgressVisibiity, value);
        }

        public int _FoldersCount;

        public int FoldersCount
        {
            get => _FoldersCount;
            set => SetProperty(ref _FoldersCount, value);
        }

        public int _FilesCount;

        public int FilesCount
        {
            get => _FilesCount;
            set => SetProperty(ref _FilesCount, value);
        }

        public string _FilesAndFoldersCountString;

        public string FilesAndFoldersCountString
        {
            get => _FilesAndFoldersCountString;
            set
            {
                if (FilesAndFoldersCountVisibility == Visibility.Collapsed)
                {
                    FilesAndFoldersCountVisibility = Visibility.Visible;
                }
                SetProperty(ref _FilesAndFoldersCountString, value);
            }
        }

        public Visibility _FilesAndFoldersCountVisibility = Visibility.Collapsed;

        public Visibility FilesAndFoldersCountVisibility
        {
            get => _FilesAndFoldersCountVisibility;
            set => SetProperty(ref _FilesAndFoldersCountVisibility, value);
        }

        private ulong _DriveUsedSpaceValue;

        public ulong DriveUsedSpaceValue
        {
            get => _DriveUsedSpaceValue;
            set
            {
                SetProperty(ref _DriveUsedSpaceValue, value);
                DriveUsedSpace = ByteSize.FromBytes(DriveUsedSpaceValue).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(DriveUsedSpaceValue).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
                DriveUsedSpaceDoubleValue = Convert.ToDouble(DriveUsedSpaceValue);
            }
        }

        private string _DriveUsedSpace;

        public string DriveUsedSpace
        {
            get => _DriveUsedSpace;
            set
            {
                DriveUsedSpaceVisibiity = Visibility.Visible;
                SetProperty(ref _DriveUsedSpace, value);
            }
        }

        public Visibility _DriveUsedSpaceVisibiity = Visibility.Collapsed;

        public Visibility DriveUsedSpaceVisibiity
        {
            get => _DriveUsedSpaceVisibiity;
            set => SetProperty(ref _DriveUsedSpaceVisibiity, value);
        }

        private ulong _DriveFreeSpaceValue;

        public ulong DriveFreeSpaceValue
        {
            get => _DriveFreeSpaceValue;
            set
            {
                SetProperty(ref _DriveFreeSpaceValue, value);
                DriveFreeSpace = ByteSize.FromBytes(DriveFreeSpaceValue).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(DriveFreeSpaceValue).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
            }
        }

        private string _DriveFreeSpace;

        public string DriveFreeSpace
        {
            get => _DriveFreeSpace;
            set
            {
                DriveFreeSpaceVisibiity = Visibility.Visible;
                SetProperty(ref _DriveFreeSpace, value);
            }
        }

        public Visibility _DriveFreeSpaceVisibiity = Visibility.Collapsed;

        public Visibility DriveFreeSpaceVisibiity
        {
            get => _DriveFreeSpaceVisibiity;
            set => SetProperty(ref _DriveFreeSpaceVisibiity, value);
        }

        private string _ItemCreatedTimestamp;

        public string ItemCreatedTimestamp
        {
            get => _ItemCreatedTimestamp;
            set
            {
                ItemCreatedTimestampVisibiity = Visibility.Visible;
                SetProperty(ref _ItemCreatedTimestamp, value);
            }
        }

        public Visibility _ItemCreatedTimestampVisibiity = Visibility.Collapsed;

        public Visibility ItemCreatedTimestampVisibiity
        {
            get => _ItemCreatedTimestampVisibiity;
            set => SetProperty(ref _ItemCreatedTimestampVisibiity, value);
        }

        private string _ItemModifiedTimestamp;

        public string ItemModifiedTimestamp
        {
            get => _ItemModifiedTimestamp;
            set
            {
                ItemModifiedTimestampVisibility = Visibility.Visible;
                SetProperty(ref _ItemModifiedTimestamp, value);
            }
        }

        private Visibility _ItemModifiedTimestampVisibility = Visibility.Collapsed;

        public Visibility ItemModifiedTimestampVisibility
        {
            get => _ItemModifiedTimestampVisibility;
            set => SetProperty(ref _ItemModifiedTimestampVisibility, value);
        }

        public string _ItemAccessedTimestamp;

        public string ItemAccessedTimestamp
        {
            get => _ItemAccessedTimestamp;
            set
            {
                ItemAccessedTimestampVisibility = Visibility.Visible;
                SetProperty(ref _ItemAccessedTimestamp, value);
            }
        }

        private Visibility _ItemAccessedTimestampVisibility = Visibility.Collapsed;

        public Visibility ItemAccessedTimestampVisibility
        {
            get => _ItemAccessedTimestampVisibility;
            set => SetProperty(ref _ItemAccessedTimestampVisibility, value);
        }

        public string _ItemFileOwner;

        public string ItemFileOwner
        {
            get => _ItemFileOwner;
            set
            {
                ItemFileOwnerVisibility = Visibility.Visible;
                SetProperty(ref _ItemFileOwner, value);
            }
        }

        private Visibility _ItemFileOwnerVisibility = Visibility.Collapsed;

        public Visibility ItemFileOwnerVisibility
        {
            get => _ItemFileOwnerVisibility;
            set => SetProperty(ref _ItemFileOwnerVisibility, value);
        }

        private Visibility _LastSeparatorVisibility = Visibility.Visible;

        public Visibility LastSeparatorVisibility
        {
            get => _LastSeparatorVisibility;
            set => SetProperty(ref _LastSeparatorVisibility, value);
        }

        private ulong _DriveCapacityValue;

        public ulong DriveCapacityValue
        {
            get => _DriveCapacityValue;
            set
            {
                SetProperty(ref _DriveCapacityValue, value);
                DriveCapacity = ByteSize.FromBytes(DriveCapacityValue).ToBinaryString().ConvertSizeAbbreviation()
                    + " (" + ByteSize.FromBytes(DriveCapacityValue).Bytes.ToString("#,##0") + " " + ResourceController.GetTranslation("ItemSizeBytes") + ")";
                DriveCapacityDoubleValue = Convert.ToDouble(DriveCapacityValue);
            }
        }

        private string _DriveCapacity;

        public string DriveCapacity
        {
            get => _DriveCapacity;
            set
            {
                DriveCapacityVisibiity = Visibility.Visible;
                SetProperty(ref _DriveCapacity, value);
            }
        }

        public Visibility _DriveCapacityVisibiity = Visibility.Collapsed;

        public Visibility DriveCapacityVisibiity
        {
            get => _DriveCapacityVisibiity;
            set => SetProperty(ref _DriveCapacityVisibiity, value);
        }

        private double _DriveCapacityDoubleValue;

        public double DriveCapacityDoubleValue
        {
            get => _DriveCapacityDoubleValue;
            set => SetProperty(ref _DriveCapacityDoubleValue, value);
        }

        private double _DriveUsedSpaceDoubleValue;

        public double DriveUsedSpaceDoubleValue
        {
            get => _DriveUsedSpaceDoubleValue;
            set => SetProperty(ref _DriveUsedSpaceDoubleValue, value);
        }

        private Visibility _ItemAttributesVisibility = Visibility.Visible;

        public Visibility ItemAttributesVisibility
        {
            get => _ItemAttributesVisibility;
            set => SetProperty(ref _ItemAttributesVisibility, value);
        }

        private string _SelectedItemsCountString;

        public string SelectedItemsCountString
        {
            get => _SelectedItemsCountString;
            set => SetProperty(ref _SelectedItemsCountString, value);
        }

        private int _SelectedItemsCount;

        public int SelectedItemsCount
        {
            get => _SelectedItemsCount;
            set => SetProperty(ref _SelectedItemsCount, value);
        }

        private bool _IsItemSelected;

        public bool IsItemSelected
        {
            get => _IsItemSelected;
            set => SetProperty(ref _IsItemSelected, value);
        }

        public SelectedItemsPropertiesViewModel()
        {
        }

        private bool _IsSelectedItemImage = false;

        public bool IsSelectedItemImage
        {
            get => _IsSelectedItemImage;
            set => SetProperty(ref _IsSelectedItemImage, value);
        }

        private bool _IsSelectedItemShortcut = false;

        public bool IsSelectedItemShortcut
        {
            get => _IsSelectedItemShortcut;
            set => SetProperty(ref _IsSelectedItemShortcut, value);
        }

        public async void CheckFileExtension()
        {
            // Set properties to false
            IsSelectedItemImage = false;
            IsSelectedItemShortcut = false;

            //check if the selected item is an image file
            string ItemExtension = await CoreApplication.MainView.ExecuteOnUIThreadAsync(() => App.CurrentInstance.ContentPage.SelectedItem.FileExtension);
            if (!string.IsNullOrEmpty(ItemExtension) && SelectedItemsCount == 1)
            {
                if (ItemExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || ItemExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    // Since item is an image, set the IsSelectedItemImage property to true
                    IsSelectedItemImage = true;
                }
                else if (ItemExtension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a shortcut, so set the IsSelectedItemShortcut property to true
                    IsSelectedItemShortcut = true;
                }
            }
        }

        private string _ShortcutItemType;

        public string ShortcutItemType
        {
            get => _ShortcutItemType;
            set => SetProperty(ref _ShortcutItemType, value);
        }

        private string _ShortcutItemPath;

        public string ShortcutItemPath
        {
            get => _ShortcutItemPath;
            set => SetProperty(ref _ShortcutItemPath, value);
        }

        private string _ShortcutItemWorkingDir;

        public string ShortcutItemWorkingDir
        {
            get => _ShortcutItemWorkingDir;
            set => SetProperty(ref _ShortcutItemWorkingDir, value);
        }

        private Visibility _ShortcutItemWorkingDirVisibility = Visibility.Collapsed;

        public Visibility ShortcutItemWorkingDirVisibility
        {
            get => _ShortcutItemWorkingDirVisibility;
            set => SetProperty(ref _ShortcutItemWorkingDirVisibility, value);
        }

        private string _ShortcutItemArguments;

        public string ShortcutItemArguments
        {
            get => _ShortcutItemArguments;
            set
            {
                SetProperty(ref _ShortcutItemArguments, value);
            }
        }

        private Visibility _ShortcutItemArgumentsVisibility = Visibility.Collapsed;

        public Visibility ShortcutItemArgumentsVisibility
        {
            get => _ShortcutItemArgumentsVisibility;
            set => SetProperty(ref _ShortcutItemArgumentsVisibility, value);
        }

        private bool _LoadLinkIcon;

        public bool LoadLinkIcon
        {
            get => _LoadLinkIcon;
            set => SetProperty(ref _LoadLinkIcon, value);
        }

        private RelayCommand _ShortcutItemOpenLinkCommand;

        public RelayCommand ShortcutItemOpenLinkCommand
        {
            get => _ShortcutItemOpenLinkCommand;
            set
            {
                SetProperty(ref _ShortcutItemOpenLinkCommand, value);
            }
        }

        public bool ContainsFilesOrFolders { get; set; }

        public Uri FolderIconSource
        {
            get
            {
                return ContainsFilesOrFolders ? new Uri("ms-appx:///Assets/FolderIcon2.svg") : new Uri("ms-appx:///Assets/FolderIcon.svg");
            }
        }
    }
}