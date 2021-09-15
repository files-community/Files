﻿using ByteSizeLib;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem.Cloud;
using Files.Filesystem.StorageItems;
using Files.Helpers;
using Files.Services;
using Files.ViewModels.Properties;
using FluentFTP;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem : ObservableObject, IGroupableItem
    {
        private static IUserSettingsService UserSettingsService = Ioc.Default.GetService<IUserSettingsService>();

        public bool IsHiddenItem { get; set; } = false;
        public StorageItemTypes PrimaryItemAttribute { get; set; }

        private volatile int itemPropertiesInitialized = 0;
        public bool ItemPropertiesInitialized
        {
            get => itemPropertiesInitialized == 1;
            set
            {
                Interlocked.Exchange(ref itemPropertiesInitialized, value ? 1 : 0);
            }
        }

        public string ItemTooltipText
        {
            get
            {
                return $"{"ToolTipDescriptionName".GetLocalized()} {itemName}{Environment.NewLine}" +
                    $"{"ToolTipDescriptionType".GetLocalized()} {itemType}{Environment.NewLine}" +
                    $"{"ToolTipDescriptionDate".GetLocalized()} {ItemDateModified}";
            }
        }

        public string FolderRelativeId { get; set; }
        public bool ContainsFilesOrFolders { get; set; }
        private bool loadFolderGlyph;
        private bool loadFileIcon;

        public bool LoadFolderGlyph
        {
            get => loadFolderGlyph;
            set => SetProperty(ref loadFolderGlyph, value);
        }

        public bool LoadFileIcon
        {
            get => loadFileIcon;
            set => SetProperty(ref loadFileIcon, value);
        }

        private bool loadUnknownTypeGlyph;

        public bool LoadUnknownTypeGlyph
        {
            get => loadUnknownTypeGlyph;
            set => SetProperty(ref loadUnknownTypeGlyph, value);
        }

        private bool loadWebShortcutGlyph;

        public bool LoadWebShortcutGlyph
        {
            get => loadWebShortcutGlyph;
            set => SetProperty(ref loadWebShortcutGlyph, value);
        }

        private bool loadCustomIcon;

        public bool LoadCustomIcon
        {
            get => loadCustomIcon;
            set => SetProperty(ref loadCustomIcon, value);
        }

        // Note: Never attempt to call this from a secondary window or another thread, create a new instance from CustomIconSource instead
        // TODO: eventually we should remove this b/c it's not thread safe
        private BitmapImage customIcon;

        public BitmapImage CustomIcon
        {
            get => customIcon;
            set
            {
                LoadCustomIcon = true;
                SetProperty(ref customIcon, value);
            }
        }

        public ulong? FileFRN { get; set; }

        private string fileTag;

        public string FileTag
        {
            get => fileTag;
            set
            {
                if (value != fileTag)
                {
                    FileTagsHelper.DbInstance.SetTag(ItemPath, FileFRN, value);
                    FileTagsHelper.WriteFileTag(ItemPath, value);
                }
                SetProperty(ref fileTag, value);
                OnPropertyChanged(nameof(FileTagUI));
            }
        }

        public FileTag FileTagUI
        {
            get => UserSettingsService.FilesAndFoldersSettingsService.AreFileTagsEnabled ? App.AppSettings.FileTagsSettings.GetTagByID(FileTag) : null;
        }

        private Uri customIconSource;

        public Uri CustomIconSource
        {
            get => customIconSource;
            set => SetProperty(ref customIconSource, value);
        }

        private byte[] customIconData;

        [JsonIgnore]
        public byte[] CustomIconData
        {
            get => customIconData;
            set => SetProperty(ref customIconData, value);
        }

        private double opacity;

        public double Opacity
        {
            get => opacity;
            set => SetProperty(ref opacity, value);
        }

        private CloudDriveSyncStatusUI syncStatusUI = new CloudDriveSyncStatusUI();

        [JsonIgnore]
        public CloudDriveSyncStatusUI SyncStatusUI
        {
            get => syncStatusUI;
            set
            {
                // For some reason this being null will cause a crash with bindings
                if (value is null)
                {
                    value = new CloudDriveSyncStatusUI();
                }
                if (SetProperty(ref syncStatusUI, value))
                {
                    OnPropertyChanged(nameof(SyncStatusString));
                }
            }
        }

        // This is used to avoid passing a null value to AutomationProperties.Name, which causes a crash
        public string SyncStatusString
        {
            get => string.IsNullOrEmpty(SyncStatusUI?.SyncStatusString) ? "CloudDriveSyncStatus_Unknown".GetLocalized() : SyncStatusUI.SyncStatusString;
        }

        private BitmapImage fileImage;

        [JsonIgnore]
        public BitmapImage FileImage
        {
            get => fileImage;
            set => SetProperty(ref fileImage, value);
        }

        public bool IsItemPinnedToStart => App.SecondaryTileHelper.CheckFolderPinned(ItemPath);

        private BitmapImage iconOverlay;

        [JsonIgnore]
        public BitmapImage IconOverlay
        {
            get => iconOverlay;
            set
            {
                if (value != null)
                {
                    SetProperty(ref iconOverlay, value);
                }
            }
        }

        private string itemPath;

        public string ItemPath
        {
            get => itemPath;
            set => SetProperty(ref itemPath, value);
        }

        private string itemName;

        public string ItemName
        {
            get => itemName;
            set => SetProperty(ref itemName, value);
        }

        private string itemType;

        public string ItemType
        {
            get => itemType;
            set
            {
                if (value != null)
                {
                    SetProperty(ref itemType, value);
                }
            }
        }

        public string FileExtension { get; set; }

        private string fileSize;

        public string FileSize
        {
            get => fileSize;
            set
            {
                SetProperty(ref fileSize, value);
                OnPropertyChanged(nameof(FileSizeDisplay));
            }
        }

        public string FileSizeDisplay => string.IsNullOrEmpty(FileSize) ? "ItemSizeNotCalcluated".GetLocalized() : FileSize;
        public long FileSizeBytes { get; set; }
        public string ItemDateModified { get; private set; }
        public string ItemDateCreated { get; private set; }
        public string ItemDateAccessed { get; private set; }

        public DateTimeOffset ItemDateModifiedReal
        {
            get => itemDateModifiedReal;
            set
            {
                ItemDateModified = value.GetFriendlyDateFromFormat(DateReturnFormat);
                itemDateModifiedReal = value;
                OnPropertyChanged(nameof(ItemDateModified));
            }
        }

        private DateTimeOffset itemDateModifiedReal;

        public DateTimeOffset ItemDateCreatedReal
        {
            get => itemDateCreatedReal;
            set
            {
                ItemDateCreated = value.GetFriendlyDateFromFormat(DateReturnFormat);
                itemDateCreatedReal = value;
                OnPropertyChanged(nameof(ItemDateCreated));
            }
        }

        private DateTimeOffset itemDateCreatedReal;

        public DateTimeOffset ItemDateAccessedReal
        {
            get => itemDateAccessedReal;
            set
            {
                ItemDateAccessed = value.GetFriendlyDateFromFormat(DateReturnFormat);
                itemDateAccessedReal = value;
                OnPropertyChanged(nameof(ItemDateAccessed));
            }
        }

        private DateTimeOffset itemDateAccessedReal;

        private ObservableCollection<FileProperty> itemProperties;

        public ObservableCollection<FileProperty> ItemProperties
        {
            get => itemProperties;
            set => SetProperty(ref itemProperties, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListedItem" /> class, optionally with an explicitly-specified dateReturnFormat.
        /// </summary>
        /// <param name="folderRelativeId"></param>
        /// <param name="dateReturnFormat">Specify a date return format to reduce redundant checks of this setting.</param>
        public ListedItem(string folderRelativeId, string dateReturnFormat = null)
        {
            FolderRelativeId = folderRelativeId;
            if (dateReturnFormat != null)
            {
                DateReturnFormat = dateReturnFormat;
            }
            else
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                DateReturnFormat = returnformat;
            }
        }

        // Parameterless constructor for JsonConvert
        public ListedItem()
        { }

        protected string DateReturnFormat { get; }

        private ObservableCollection<FileProperty> fileDetails;

        [JsonIgnore]
        public ObservableCollection<FileProperty> FileDetails
        {
            get => fileDetails;
            set => SetProperty(ref fileDetails, value);
        }

        public override string ToString()
        {
            string suffix;
            if (IsRecycleBinItem)
            {
                suffix = "RecycleBinItemAutomation".GetLocalized();
            }
            else if (IsShortcutItem)
            {
                suffix = "ShortcutItemAutomation".GetLocalized();
            }
            else if (IsLibraryItem)
            {
                suffix = "LibraryItemAutomation".GetLocalized();
            }
            else
            {
                suffix = PrimaryItemAttribute == StorageItemTypes.File ? "FileItemAutomation".GetLocalized() : "FolderItemAutomation".GetLocalized();
            }
            return $"{ItemName}, {suffix}";
        }

        public bool IsRecycleBinItem => this is RecycleBinItem;
        public bool IsShortcutItem => this is ShortcutItem;
        public bool IsLibraryItem => this is LibraryItem;
        public bool IsLinkItem => IsShortcutItem && ((ShortcutItem)this).IsUrl;
        public bool IsFtpItem => this is FtpItem;
        public bool IsZipItem => this is ZipItem;

        public virtual bool IsExecutable => new[] { ".exe", ".bat", ".cmd" }.Contains(Path.GetExtension(ItemPath), StringComparer.OrdinalIgnoreCase);
        public bool IsPinned => App.SidebarPinnedController.Model.FavoriteItems.Contains(itemPath);

        private BaseStorageFile itemFile;

        public BaseStorageFile ItemFile
        {
            get => itemFile;
            set => SetProperty(ref itemFile, value);
        }

        // This is a hack used because x:Bind casting did not work properly
        [JsonIgnore]
        public RecycleBinItem AsRecycleBinItem => this as RecycleBinItem;

        public string Key { get; set; }
    }

    public class RecycleBinItem : ListedItem
    {
        public RecycleBinItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        // Parameterless constructor for JsonConvert
        public RecycleBinItem() : base()
        { }

        public string ItemDateDeleted { get; private set; }

        public DateTimeOffset ItemDateDeletedReal
        {
            get => itemDateDeletedReal;
            set
            {
                ItemDateDeleted = value.GetFriendlyDateFromFormat(DateReturnFormat);
                itemDateDeletedReal = value;
            }
        }

        private DateTimeOffset itemDateDeletedReal;

        // For recycle bin elements (path + name)
        public string ItemOriginalPath { get; set; }

        // For recycle bin elements (path)
        public string ItemOriginalFolder => Path.IsPathRooted(ItemOriginalPath) ? Path.GetDirectoryName(ItemOriginalPath) : ItemOriginalPath;

        public string ItemOriginalFolderName => Path.GetFileName(ItemOriginalFolder);
    }

    public class FtpItem : ListedItem
    {
        public FtpItem(FtpListItem item, string folder, string dateReturnFormat = null) : base(null, dateReturnFormat)
        {
            var isFile = item.Type == FtpFileSystemObjectType.File;
            ItemDateCreatedReal = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
            ItemDateModifiedReal = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
            ItemName = item.Name;
            FileExtension = Path.GetExtension(item.Name);
            ItemPath = PathNormalization.Combine(folder, item.Name);
            PrimaryItemAttribute = isFile ? StorageItemTypes.File : StorageItemTypes.Folder;
            ItemPropertiesInitialized = false;

            var itemType = isFile ? "ItemTypeFile".GetLocalized() : "FileFolderListItem".GetLocalized();
            if (isFile && ItemName.Contains("."))
            {
                itemType = FileExtension.Trim('.') + " " + itemType;
            }

            ItemType = itemType;
            LoadFolderGlyph = !isFile;
            FileSizeBytes = item.Size;
            ContainsFilesOrFolders = !isFile;
            LoadUnknownTypeGlyph = isFile;
            FileImage = null;
            FileSize = ByteSize.FromBytes(FileSizeBytes).ToBinaryString().ConvertSizeAbbreviation();
            Opacity = 1;
            IsHiddenItem = false;
        }
    }

    public class ShortcutItem : ListedItem
    {
        public ShortcutItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        // Parameterless constructor for JsonConvert
        public ShortcutItem() : base()
        { }

        // For shortcut elements (.lnk and .url)
        public string TargetPath { get; set; }

        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool IsUrl { get; set; }
        public override bool IsExecutable => Path.GetExtension(TargetPath)?.ToLower() == ".exe";
    }

    public class ZipItem : ListedItem
    {
        public ZipItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        // Parameterless constructor for JsonConvert
        public ZipItem() : base()
        { }
    }

    public class LibraryItem : ListedItem
    {
        public LibraryItem(LibraryLocationItem lib, string returnFormat = null) : base(null, returnFormat)
        {
            ItemPath = lib.Path;
            ItemName = lib.Text;
            PrimaryItemAttribute = StorageItemTypes.Folder;
            ItemType = "ItemTypeLibrary".GetLocalized();
            LoadCustomIcon = true;
            CustomIcon = lib.Icon;
            //CustomIconSource = lib.IconSource;
            CustomIconData = lib.IconData;
            LoadFileIcon = CustomIconData != null;

            IsEmpty = lib.IsEmpty;
            DefaultSaveFolder = lib.DefaultSaveFolder;
            Folders = lib.Folders;
        }

        public bool IsEmpty { get; }

        public string DefaultSaveFolder { get; }

        public ReadOnlyCollection<string> Folders { get; }
    }
}