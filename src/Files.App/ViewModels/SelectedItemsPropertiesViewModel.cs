using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.ViewModels.Properties;
using Files.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.App.ViewModels
{
	public class SelectedItemsPropertiesViewModel : ObservableObject
	{
		private bool loadFolderGlyph;
		public bool LoadFolderGlyph
		{
			get => loadFolderGlyph;
			set => SetProperty(ref loadFolderGlyph, value);
		}

		private bool loadUnknownTypeGlyph;
		public bool LoadUnknownTypeGlyph
		{
			get => loadUnknownTypeGlyph;
			set => SetProperty(ref loadUnknownTypeGlyph, value);
		}

		private bool loadCombinedItemsGlyph;
		public bool LoadCombinedItemsGlyph
		{
			get => loadCombinedItemsGlyph;
			set => SetProperty(ref loadCombinedItemsGlyph, value);
		}

		private Uri customIconSource;
		public Uri CustomIconSource
		{
			get => customIconSource;
			set => SetProperty(ref customIconSource, value);
		}

		private bool loadCustomIcon;
		public bool LoadCustomIcon
		{
			get => loadCustomIcon;
			set => SetProperty(ref loadCustomIcon, value);
		}

		private bool loadFileIcon;
		public bool LoadFileIcon
		{
			get => loadFileIcon;
			set => SetProperty(ref loadFileIcon, value);
		}

		private byte[] iconData;
		public byte[] IconData
		{
			get => iconData;
			set => SetProperty(ref iconData, value);
		}

		private string itemName;
		public string ItemName
		{
			get => itemName;
			set
			{
				ItemNameVisibility = true;
				SetProperty(ref itemName, value);
			}
		}

		private string originalItemName;
		public string OriginalItemName
		{
			get => originalItemName;
			set
			{
				ItemNameVisibility = true;
				SetProperty(ref originalItemName, value);
			}
		}

		private bool itemNameVisibility = false;
		public bool ItemNameVisibility
		{
			get => itemNameVisibility;
			set => SetProperty(ref itemNameVisibility, value);
		}

		private string itemType;
		public string ItemType
		{
			get => itemType;
			set
			{
				ItemTypeVisibility = true;
				SetProperty(ref itemType, value);
			}
		}

		private bool itemTypeVisibility = false;
		public bool ItemTypeVisibility
		{
			get => itemTypeVisibility;
			set => SetProperty(ref itemTypeVisibility, value);
		}

		private string driveFileSystem;
		public string DriveFileSystem
		{
			get => driveFileSystem;
			set
			{
				DriveFileSystemVisibility = true;
				SetProperty(ref driveFileSystem, value);
			}
		}

		private bool driveFileSystemVisibility = false;
		public bool DriveFileSystemVisibility
		{
			get => driveFileSystemVisibility;
			set => SetProperty(ref driveFileSystemVisibility, value);
		}

		private string itemPath;
		public string ItemPath
		{
			get => itemPath;
			set
			{
				ItemPathVisibility = true;
				SetProperty(ref itemPath, value);
			}
		}

		private bool itemPathVisibility = false;
		public bool ItemPathVisibility
		{
			get => itemPathVisibility;
			set => SetProperty(ref itemPathVisibility, value);
		}

		private string itemSize;
		public string ItemSize
		{
			get => itemSize;
			set => SetProperty(ref itemSize, value);
		}

		private string uncompresseditemSize;
		public string UncompressedItemSize
		{
			get => uncompresseditemSize;
			set
			{
				IsUncompressedItemSizeVisibile = true;
				SetProperty(ref uncompresseditemSize, value);
			}
		}

		private bool itemSizeVisibility = false;
		public bool ItemSizeVisibility
		{
			get => itemSizeVisibility;
			set => SetProperty(ref itemSizeVisibility, value);
		}

		private bool isUncompressedItemSizeVisibile = false;
		public bool IsUncompressedItemSizeVisibile
		{
			get => isUncompressedItemSizeVisibile;
			set => SetProperty(ref isUncompressedItemSizeVisibile, value);
		}

		private long itemSizeBytes;
		public long ItemSizeBytes
		{
			get => itemSizeBytes;
			set => SetProperty(ref itemSizeBytes, value);
		}

		private long uncompresseditemSizeBytes;
		public long UncompressedItemSizeBytes
		{
			get => uncompresseditemSizeBytes;
			set => SetProperty(ref uncompresseditemSizeBytes, value);
		}

		private bool itemSizeProgressVisibility = false;
		public bool ItemSizeProgressVisibility
		{
			get => itemSizeProgressVisibility;
			set => SetProperty(ref itemSizeProgressVisibility, value);
		}

		// For libraries
		public int locationsCount;
		public int LocationsCount
		{
			get => locationsCount;
			set => SetProperty(ref locationsCount, value);
		}

		public int foldersCount;
		public int FoldersCount
		{
			get => foldersCount;
			set => SetProperty(ref foldersCount, value);
		}

		public int filesCount;
		public int FilesCount
		{
			get => filesCount;
			set => SetProperty(ref filesCount, value);
		}

		public string filesAndFoldersCountString;
		public string FilesAndFoldersCountString
		{
			get => filesAndFoldersCountString;
			set
			{
				if (FilesAndFoldersCountVisibility == false)
				{
					FilesAndFoldersCountVisibility = true;
				}
				SetProperty(ref filesAndFoldersCountString, value);
			}
		}

		public bool filesAndFoldersCountVisibility = false;
		public bool FilesAndFoldersCountVisibility
		{
			get => filesAndFoldersCountVisibility;
			set => SetProperty(ref filesAndFoldersCountVisibility, value);
		}

		private ulong driveUsedSpaceValue;
		public ulong DriveUsedSpaceValue
		{
			get => driveUsedSpaceValue;
			set
			{
				SetProperty(ref driveUsedSpaceValue, value);
				DriveUsedSpace = DriveUsedSpaceValue.ToSizeString();
				DriveUsedSpaceLongSize = DriveUsedSpaceValue.ToLongSizeString();
				OnPropertyChanged(nameof(DrivePercentageValue));
			}
		}

		private string driveUsedSpace;
		public string DriveUsedSpace
		{
			get => driveUsedSpace;
			set
			{
				DriveUsedSpaceVisibility = true;
				SetProperty(ref driveUsedSpace, value);
			}
		}

		private string driveUsedSpaceLongSize;
		public string DriveUsedSpaceLongSize
		{
			get => driveUsedSpaceLongSize;
			set
			{
				SetProperty(ref driveUsedSpaceLongSize, value);
			}
		}

		public bool driveUsedSpaceVisibility = false;
		public bool DriveUsedSpaceVisibility
		{
			get => driveUsedSpaceVisibility;
			set => SetProperty(ref driveUsedSpaceVisibility, value);
		}

		private ulong driveFreeSpaceValue;
		public ulong DriveFreeSpaceValue
		{
			get => driveFreeSpaceValue;
			set
			{
				SetProperty(ref driveFreeSpaceValue, value);
				DriveFreeSpace = DriveFreeSpaceValue.ToSizeString();
				DriveFreeSpaceLongSize = DriveFreeSpaceValue.ToLongSizeString();

			}
		}

		private string driveFreeSpace;
		public string DriveFreeSpace
		{
			get => driveFreeSpace;
			set
			{
				DriveFreeSpaceVisibility = true;
				SetProperty(ref driveFreeSpace, value);
			}
		}

		private string driveFreeSpaceLongSize;
		public string DriveFreeSpaceLongSize
		{
			get => driveFreeSpaceLongSize;
			set
			{
				SetProperty(ref driveFreeSpaceLongSize, value);
			}
		}

		public bool driveFreeSpaceVisibility = false;
		public bool DriveFreeSpaceVisibility
		{
			get => driveFreeSpaceVisibility;
			set => SetProperty(ref driveFreeSpaceVisibility, value);
		}

		private string itemCreatedTimestamp;
		public string ItemCreatedTimestamp
		{
			get => itemCreatedTimestamp;
			set
			{
				ItemCreatedTimestampVisibility = true;
				SetProperty(ref itemCreatedTimestamp, value);
			}
		}

		public bool itemCreatedTimestampVisibility = false;
		public bool ItemCreatedTimestampVisibility
		{
			get => itemCreatedTimestampVisibility;
			set => SetProperty(ref itemCreatedTimestampVisibility, value);
		}

		private string itemModifiedTimestamp;
		public string ItemModifiedTimestamp
		{
			get => itemModifiedTimestamp;
			set
			{
				ItemModifiedTimestampVisibility = true;
				SetProperty(ref itemModifiedTimestamp, value);
			}
		}

		private bool itemModifiedTimestampVisibility = false;
		public bool ItemModifiedTimestampVisibility
		{
			get => itemModifiedTimestampVisibility;
			set => SetProperty(ref itemModifiedTimestampVisibility, value);
		}

		public string itemAccessedTimestamp;
		public string ItemAccessedTimestamp
		{
			get => itemAccessedTimestamp;
			set
			{
				ItemAccessedTimestampVisibility = true;
				SetProperty(ref itemAccessedTimestamp, value);
			}
		}

		private bool itemAccessedTimestampVisibility = false;
		public bool ItemAccessedTimestampVisibility
		{
			get => itemAccessedTimestampVisibility;
			set => SetProperty(ref itemAccessedTimestampVisibility, value);
		}

		private bool lastSeparatorVisibility = true;
		public bool LastSeparatorVisibility
		{
			get => lastSeparatorVisibility;
			set => SetProperty(ref lastSeparatorVisibility, value);
		}

		private ulong driveCapacityValue;
		public ulong DriveCapacityValue
		{
			get => driveCapacityValue;
			set
			{
				SetProperty(ref driveCapacityValue, value);
				DriveCapacity = DriveCapacityValue.ToSizeString();
				DriveCapacityLongSize = DriveCapacityValue.ToLongSizeString();
				OnPropertyChanged(nameof(DrivePercentageValue));
			}
		}

		private string driveCapacity;
		public string DriveCapacity
		{
			get => driveCapacity;
			set
			{
				DriveCapacityVisibility = true;
				SetProperty(ref driveCapacity, value);
			}
		}

		private string driveCapacityLongSize;
		public string DriveCapacityLongSize
		{
			get => driveCapacityLongSize;
			set
			{
				SetProperty(ref driveCapacityLongSize, value);
			}
		}

		public bool driveCapacityVisibility = false;
		public bool DriveCapacityVisibility
		{
			get => driveCapacityVisibility;
			set => SetProperty(ref driveCapacityVisibility, value);
		}

		public double DrivePercentageValue
		{
			get => DriveCapacityValue > 0 ? DriveUsedSpaceValue / (double)DriveCapacityValue * 100 : 0;
		}

		private bool itemAttributesVisibility = true;
		public bool ItemAttributesVisibility
		{
			get => itemAttributesVisibility;
			set => SetProperty(ref itemAttributesVisibility, value);
		}

		private string selectedItemsCountString;
		public string SelectedItemsCountString
		{
			get => selectedItemsCountString;
			set => SetProperty(ref selectedItemsCountString, value);
		}

		private int selectedItemsCount;
		public int SelectedItemsCount
		{
			get => selectedItemsCount;
			set => SetProperty(ref selectedItemsCount, value);
		}

		private bool isItemSelected;
		public bool IsItemSelected
		{
			get => isItemSelected;
			set => SetProperty(ref isItemSelected, value);
		}

		public SelectedItemsPropertiesViewModel()
		{
		}

		private bool isSelectedItemImage = false;
		public bool IsSelectedItemImage
		{
			get => isSelectedItemImage;
			set => SetProperty(ref isSelectedItemImage, value);
		}

		private bool isSelectedItemShortcut = false;
		public bool IsSelectedItemShortcut
		{
			get => isSelectedItemShortcut;
			set => SetProperty(ref isSelectedItemShortcut, value);
		}

		public void CheckAllFileExtensions(List<string> itemExtensions)
		{
			// Checks if all the item extensions are image extensions of some kind.
			IsSelectedItemImage = itemExtensions.TrueForAll(itemExtension => FileExtensionHelpers.IsImageFile(itemExtension));
			// Checks if there is only one selected item and if it's a shortcut.
			IsSelectedItemShortcut = (itemExtensions.Count == 1) && (itemExtensions.TrueForAll(itemExtension => FileExtensionHelpers.IsShortcutFile(itemExtension)));
		}

		private string shortcutItemType;
		public string ShortcutItemType
		{
			get => shortcutItemType;
			set => SetProperty(ref shortcutItemType, value);
		}

		private string shortcutItemPath;
		public string ShortcutItemPath
		{
			get => shortcutItemPath;
			set => SetProperty(ref shortcutItemPath, value);
		}

		private bool isShortcutItemPathReadOnly;
		public bool IsShortcutItemPathReadOnly
		{
			get => isShortcutItemPathReadOnly;
			set => SetProperty(ref isShortcutItemPathReadOnly, value);
		}

		private string shortcutItemWorkingDir;
		public string ShortcutItemWorkingDir
		{
			get => shortcutItemWorkingDir;
			set => SetProperty(ref shortcutItemWorkingDir, value);
		}

		private bool shortcutItemWorkingDirVisibility = false;
		public bool ShortcutItemWorkingDirVisibility
		{
			get => shortcutItemWorkingDirVisibility;
			set => SetProperty(ref shortcutItemWorkingDirVisibility, value);
		}

		private string shortcutItemArguments;
		public string ShortcutItemArguments
		{
			get => shortcutItemArguments;
			set
			{
				SetProperty(ref shortcutItemArguments, value);
			}
		}

		private bool shortcutItemArgumentsVisibility = false;
		public bool ShortcutItemArgumentsVisibility
		{
			get => shortcutItemArgumentsVisibility;
			set => SetProperty(ref shortcutItemArgumentsVisibility, value);
		}

		private bool loadLinkIcon;
		public bool LoadLinkIcon
		{
			get => loadLinkIcon;
			set => SetProperty(ref loadLinkIcon, value);
		}

		private RelayCommand shortcutItemOpenLinkCommand;
		public RelayCommand ShortcutItemOpenLinkCommand
		{
			get => shortcutItemOpenLinkCommand;
			set
			{
				SetProperty(ref shortcutItemOpenLinkCommand, value);
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

		private ObservableCollection<FilePropertySection> propertySections = new();
		public ObservableCollection<FilePropertySection> PropertySections
		{
			get => propertySections;
			set => SetProperty(ref propertySections, value);
		}

		private ObservableCollection<FileProperty> fileProperties = new();
		public ObservableCollection<FileProperty> FileProperties
		{
			get => fileProperties;
			set => SetProperty(ref fileProperties, value);
		}

		private bool isReadOnly;
		public bool IsReadOnly
		{
			get => isReadOnly;
			set
			{
				IsReadOnlyEnabled = true;
				SetProperty(ref isReadOnly, value);
			}
		}

		private bool isReadOnlyEnabled;
		public bool IsReadOnlyEnabled
		{
			get => isReadOnlyEnabled;
			set => SetProperty(ref isReadOnlyEnabled, value);
		}

		private bool isHidden;
		public bool IsHidden
		{
			get => isHidden;
			set => SetProperty(ref isHidden, value);
		}

		private bool runAsAdmin;
		public bool RunAsAdmin
		{
			get => runAsAdmin;
			set
			{
				RunAsAdminEnabled = true;
				SetProperty(ref runAsAdmin, value);
			}
		}

		private bool runAsAdminEnabled;
		public bool RunAsAdminEnabled
		{
			get => runAsAdminEnabled;
			set => SetProperty(ref runAsAdminEnabled, value);
		}
	}
}
