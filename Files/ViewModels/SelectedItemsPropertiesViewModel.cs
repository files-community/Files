using ByteSizeLib;
using Files.Extensions;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.ViewModels
{
    public class SelectedItemsPropertiesViewModel : ObservableObject
    {
        //TODO add code regions

        public Visibility driveCapacityVisibiity = Visibility.Collapsed;
        public Visibility driveFreeSpaceVisibiity = Visibility.Collapsed;
        public Visibility driveUsedSpaceVisibiity = Visibility.Collapsed;
        public string filesAndFoldersCountString;
        public Visibility filesAndFoldersCountVisibility = Visibility.Collapsed;
        public int filesCount;
        public int foldersCount;
        public string itemAccessedTimestamp;
        public Visibility itemCreatedTimestampVisibiity = Visibility.Collapsed;
        public string itemFileOwner;
        public string itemMD5Hash;
        public Visibility itemMD5HashProgressVisibiity = Visibility.Collapsed;
        public Visibility itemMD5HashVisibility = Visibility.Collapsed;

        // For libraries
        public int locationsCount;

        private IBaseLayout contentPage;
        private SvgImageSource customIcon;
        private string driveCapacity;
        private double driveCapacityDoubleValue;
        private ulong driveCapacityValue;
        private string driveFileSystem;
        private Visibility driveFileSystemVisibility = Visibility.Collapsed;
        private string driveFreeSpace;
        private ulong driveFreeSpaceValue;
        private string driveUsedSpace;
        private double driveUsedSpaceDoubleValue;
        private ulong driveUsedSpaceValue;
        private ImageSource fileIconSource;
        private ObservableCollection<FileProperty> fileProperties = new ObservableCollection<FileProperty>();
        private bool isHidden;
        private bool isItemSelected;
        private bool isReadOnly;
        private bool isReadOnlyEnabled;
        private bool isSelectedItemImage = false;
        private bool isSelectedItemShortcut = false;
        private Visibility itemAccessedTimestampVisibility = Visibility.Collapsed;
        private Visibility itemAttributesVisibility = Visibility.Visible;
        private string itemCreatedTimestamp;
        private Visibility itemFileOwnerVisibility = Visibility.Collapsed;
        private bool itemMD5HashCalcError;
        private string itemModifiedTimestamp;
        private Visibility itemModifiedTimestampVisibility = Visibility.Collapsed;
        private string itemName;
        private Visibility itemNameVisibility = Visibility.Collapsed;
        private string itemPath;
        private Visibility itemPathVisibility = Visibility.Collapsed;
        private string itemSize;
        private long itemSizeBytes;
        private Visibility itemSizeProgressVisibility = Visibility.Collapsed;
        private Visibility itemSizeVisibility = Visibility.Collapsed;
        private string itemType;
        private Visibility itemTypeVisibility = Visibility.Collapsed;
        private Visibility lastSeparatorVisibility = Visibility.Visible;
        private bool loadCombinedItemsGlyph;
        private bool loadCustomIcon;
        private bool loadFileIcon;
        private bool loadFolderGlyph;

        private bool loadLinkIcon;

        private bool loadUnknownTypeGlyph;

        private string originalItemName;

        private ObservableCollection<FilePropertySection> propertySections = new ObservableCollection<FilePropertySection>();

        private int selectedItemsCount;

        private string selectedItemsCountString;

        private string shortcutItemArguments;

        private Visibility shortcutItemArgumentsVisibility = Visibility.Collapsed;

        private RelayCommand shortcutItemOpenLinkCommand;

        private string shortcutItemPath;

        private string shortcutItemType;

        private string shortcutItemWorkingDir;

        private Visibility shortcutItemWorkingDirVisibility = Visibility.Collapsed;

        public SelectedItemsPropertiesViewModel(IBaseLayout contentPage)
        {
            this.contentPage = contentPage;
        }

        public bool ContainsFilesOrFolders { get; set; }

        public SvgImageSource CustomIcon
        {
            get => customIcon;
            set => SetProperty(ref customIcon, value);
        }

        public string DriveCapacity
        {
            get => driveCapacity;
            set
            {
                DriveCapacityVisibiity = Visibility.Visible;
                SetProperty(ref driveCapacity, value);
            }
        }

        public double DriveCapacityDoubleValue
        {
            get => driveCapacityDoubleValue;
            set => SetProperty(ref driveCapacityDoubleValue, value);
        }

        public ulong DriveCapacityValue
        {
            get => driveCapacityValue;
            set
            {
                SetProperty(ref driveCapacityValue, value);
                DriveCapacity = $"{ByteSize.FromBytes(DriveCapacityValue).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(DriveCapacityValue).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
                DriveCapacityDoubleValue = Convert.ToDouble(DriveCapacityValue);
            }
        }

        public Visibility DriveCapacityVisibiity
        {
            get => driveCapacityVisibiity;
            set => SetProperty(ref driveCapacityVisibiity, value);
        }

        public string DriveFileSystem
        {
            get => driveFileSystem;
            set
            {
                DriveFileSystemVisibility = Visibility.Visible;
                SetProperty(ref driveFileSystem, value);
            }
        }

        public Visibility DriveFileSystemVisibility
        {
            get => driveFileSystemVisibility;
            set => SetProperty(ref driveFileSystemVisibility, value);
        }

        public string DriveFreeSpace
        {
            get => driveFreeSpace;
            set
            {
                DriveFreeSpaceVisibiity = Visibility.Visible;
                SetProperty(ref driveFreeSpace, value);
            }
        }

        public ulong DriveFreeSpaceValue
        {
            get => driveFreeSpaceValue;
            set
            {
                SetProperty(ref driveFreeSpaceValue, value);
                DriveFreeSpace = $"{ByteSize.FromBytes(DriveFreeSpaceValue).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(DriveFreeSpaceValue).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
            }
        }

        public Visibility DriveFreeSpaceVisibiity
        {
            get => driveFreeSpaceVisibiity;
            set => SetProperty(ref driveFreeSpaceVisibiity, value);
        }

        public string DriveUsedSpace
        {
            get => driveUsedSpace;
            set
            {
                DriveUsedSpaceVisibiity = Visibility.Visible;
                SetProperty(ref driveUsedSpace, value);
            }
        }

        public double DriveUsedSpaceDoubleValue
        {
            get => driveUsedSpaceDoubleValue;
            set => SetProperty(ref driveUsedSpaceDoubleValue, value);
        }

        public ulong DriveUsedSpaceValue
        {
            get => driveUsedSpaceValue;
            set
            {
                SetProperty(ref driveUsedSpaceValue, value);
                DriveUsedSpace = $"{ByteSize.FromBytes(DriveUsedSpaceValue).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(DriveUsedSpaceValue).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
                DriveUsedSpaceDoubleValue = Convert.ToDouble(DriveUsedSpaceValue);
            }
        }

        public Visibility DriveUsedSpaceVisibiity
        {
            get => driveUsedSpaceVisibiity;
            set => SetProperty(ref driveUsedSpaceVisibiity, value);
        }

        public ImageSource FileIconSource
        {
            get => fileIconSource;
            set => SetProperty(ref fileIconSource, value);
        }

        public ObservableCollection<FileProperty> FileProperties
        {
            get => fileProperties;
            set => SetProperty(ref fileProperties, value);
        }

        public string FilesAndFoldersCountString
        {
            get => filesAndFoldersCountString;
            set
            {
                if (FilesAndFoldersCountVisibility == Visibility.Collapsed)
                {
                    FilesAndFoldersCountVisibility = Visibility.Visible;
                }
                SetProperty(ref filesAndFoldersCountString, value);
            }
        }

        public Visibility FilesAndFoldersCountVisibility
        {
            get => filesAndFoldersCountVisibility;
            set => SetProperty(ref filesAndFoldersCountVisibility, value);
        }

        public int FilesCount
        {
            get => filesCount;
            set => SetProperty(ref filesCount, value);
        }

        public Uri FolderIconSource
        {
            get
            {
                return ContainsFilesOrFolders ? new Uri("ms-appx:///Assets/FolderIcon2.svg") : new Uri("ms-appx:///Assets/FolderIcon.svg");
            }
        }

        public int FoldersCount
        {
            get => foldersCount;
            set => SetProperty(ref foldersCount, value);
        }

        public bool IsHidden
        {
            get => isHidden;
            set => SetProperty(ref isHidden, value);
        }

        public bool IsItemSelected
        {
            get => isItemSelected;
            set => SetProperty(ref isItemSelected, value);
        }

        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                IsReadOnlyEnabled = true;
                SetProperty(ref isReadOnly, value);
            }
        }

        public bool IsReadOnlyEnabled
        {
            get => isReadOnlyEnabled;
            set => SetProperty(ref isReadOnlyEnabled, value);
        }

        public bool IsSelectedItemImage
        {
            get => isSelectedItemImage;
            set => SetProperty(ref isSelectedItemImage, value);
        }

        public bool IsSelectedItemShortcut
        {
            get => isSelectedItemShortcut;
            set => SetProperty(ref isSelectedItemShortcut, value);
        }

        public string ItemAccessedTimestamp
        {
            get => itemAccessedTimestamp;
            set
            {
                ItemAccessedTimestampVisibility = Visibility.Visible;
                SetProperty(ref itemAccessedTimestamp, value);
            }
        }

        public Visibility ItemAccessedTimestampVisibility
        {
            get => itemAccessedTimestampVisibility;
            set => SetProperty(ref itemAccessedTimestampVisibility, value);
        }

        public Visibility ItemAttributesVisibility
        {
            get => itemAttributesVisibility;
            set => SetProperty(ref itemAttributesVisibility, value);
        }

        public string ItemCreatedTimestamp
        {
            get => itemCreatedTimestamp;
            set
            {
                ItemCreatedTimestampVisibiity = Visibility.Visible;
                SetProperty(ref itemCreatedTimestamp, value);
            }
        }

        public Visibility ItemCreatedTimestampVisibiity
        {
            get => itemCreatedTimestampVisibiity;
            set => SetProperty(ref itemCreatedTimestampVisibiity, value);
        }

        public string ItemFileOwner
        {
            get => itemFileOwner;
            set
            {
                ItemFileOwnerVisibility = Visibility.Visible;
                SetProperty(ref itemFileOwner, value);
            }
        }

        public Visibility ItemFileOwnerVisibility
        {
            get => itemFileOwnerVisibility;
            set => SetProperty(ref itemFileOwnerVisibility, value);
        }

        public string ItemMD5Hash
        {
            get => itemMD5Hash;
            set
            {
                if (!string.IsNullOrEmpty(value) && value != itemMD5Hash)
                {
                    SetProperty(ref itemMD5Hash, value);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
        }

        public bool ItemMD5HashCalcError
        {
            get => itemMD5HashCalcError;
            set => SetProperty(ref itemMD5HashCalcError, value);
        }

        public Visibility ItemMD5HashProgressVisibility
        {
            get => itemMD5HashProgressVisibiity;
            set => SetProperty(ref itemMD5HashProgressVisibiity, value);
        }

        public Visibility ItemMD5HashVisibility
        {
            get => itemMD5HashVisibility;
            set => SetProperty(ref itemMD5HashVisibility, value);
        }

        public string ItemModifiedTimestamp
        {
            get => itemModifiedTimestamp;
            set
            {
                ItemModifiedTimestampVisibility = Visibility.Visible;
                SetProperty(ref itemModifiedTimestamp, value);
            }
        }

        public Visibility ItemModifiedTimestampVisibility
        {
            get => itemModifiedTimestampVisibility;
            set => SetProperty(ref itemModifiedTimestampVisibility, value);
        }

        public string ItemName
        {
            get => itemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref itemName, value);
            }
        }

        public Visibility ItemNameVisibility
        {
            get => itemNameVisibility;
            set => SetProperty(ref itemNameVisibility, value);
        }

        public string ItemPath
        {
            get => itemPath;
            set
            {
                ItemPathVisibility = Visibility.Visible;
                SetProperty(ref itemPath, value);
            }
        }

        public Visibility ItemPathVisibility
        {
            get => itemPathVisibility;
            set => SetProperty(ref itemPathVisibility, value);
        }

        public string ItemSize
        {
            get => itemSize;
            set => SetProperty(ref itemSize, value);
        }

        public long ItemSizeBytes
        {
            get => itemSizeBytes;
            set => SetProperty(ref itemSizeBytes, value);
        }

        public Visibility ItemSizeProgressVisibility
        {
            get => itemSizeProgressVisibility;
            set => SetProperty(ref itemSizeProgressVisibility, value);
        }

        public Visibility ItemSizeVisibility
        {
            get => itemSizeVisibility;
            set => SetProperty(ref itemSizeVisibility, value);
        }

        public string ItemType
        {
            get => itemType;
            set
            {
                ItemTypeVisibility = Visibility.Visible;
                SetProperty(ref itemType, value);
            }
        }

        public Visibility ItemTypeVisibility
        {
            get => itemTypeVisibility;
            set => SetProperty(ref itemTypeVisibility, value);
        }

        public Visibility LastSeparatorVisibility
        {
            get => lastSeparatorVisibility;
            set => SetProperty(ref lastSeparatorVisibility, value);
        }

        public bool LoadCombinedItemsGlyph
        {
            get => loadCombinedItemsGlyph;
            set => SetProperty(ref loadCombinedItemsGlyph, value);
        }

        public bool LoadCustomIcon
        {
            get => loadCustomIcon;
            set => SetProperty(ref loadCustomIcon, value);
        }

        public bool LoadFileIcon
        {
            get => loadFileIcon;
            set => SetProperty(ref loadFileIcon, value);
        }

        public bool LoadFolderGlyph
        {
            get => loadFolderGlyph;
            set => SetProperty(ref loadFolderGlyph, value);
        }

        public bool LoadLinkIcon
        {
            get => loadLinkIcon;
            set => SetProperty(ref loadLinkIcon, value);
        }

        public bool LoadUnknownTypeGlyph
        {
            get => loadUnknownTypeGlyph;
            set => SetProperty(ref loadUnknownTypeGlyph, value);
        }

        public int LocationsCount
        {
            get => locationsCount;
            set => SetProperty(ref locationsCount, value);
        }

        public string OriginalItemName
        {
            get => originalItemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref originalItemName, value);
            }
        }

        public ObservableCollection<FilePropertySection> PropertySections
        {
            get => propertySections;
            set => SetProperty(ref propertySections, value);
        }

        public int SelectedItemsCount
        {
            get => selectedItemsCount;
            set => SetProperty(ref selectedItemsCount, value);
        }

        public string SelectedItemsCountString
        {
            get => selectedItemsCountString;
            set => SetProperty(ref selectedItemsCountString, value);
        }

        public string ShortcutItemArguments
        {
            get => shortcutItemArguments;
            set
            {
                SetProperty(ref shortcutItemArguments, value);
            }
        }

        public Visibility ShortcutItemArgumentsVisibility
        {
            get => shortcutItemArgumentsVisibility;
            set => SetProperty(ref shortcutItemArgumentsVisibility, value);
        }

        public RelayCommand ShortcutItemOpenLinkCommand
        {
            get => shortcutItemOpenLinkCommand;
            set
            {
                SetProperty(ref shortcutItemOpenLinkCommand, value);
            }
        }

        public string ShortcutItemPath
        {
            get => shortcutItemPath;
            set => SetProperty(ref shortcutItemPath, value);
        }

        public string ShortcutItemType
        {
            get => shortcutItemType;
            set => SetProperty(ref shortcutItemType, value);
        }

        public string ShortcutItemWorkingDir
        {
            get => shortcutItemWorkingDir;
            set => SetProperty(ref shortcutItemWorkingDir, value);
        }

        public Visibility ShortcutItemWorkingDirVisibility
        {
            get => shortcutItemWorkingDirVisibility;
            set => SetProperty(ref shortcutItemWorkingDirVisibility, value);
        }

        public void CheckFileExtension()
        {
            // Set properties to false
            IsSelectedItemImage = false;
            IsSelectedItemShortcut = false;

            //check if the selected item is an image file
            string ItemExtension = contentPage?.SelectedItem?.FileExtension;
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
    }
}