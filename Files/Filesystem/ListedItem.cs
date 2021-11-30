using ByteSizeLib;
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
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Files.Filesystem
{
    public class ListedItem : ObservableObject, IGroupableItem
    {
        protected static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        protected static IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        public bool IsHiddenItem { get; set; } = false;

        public StorageItemTypes PrimaryItemAttribute { get; set; }

        private volatile int itemPropertiesInitialized = 0;
        public bool ItemPropertiesInitialized
        {
            get => itemPropertiesInitialized == 1;
            set => Interlocked.Exchange(ref itemPropertiesInitialized, value ? 1 : 0);
        }

        public string ItemTooltipText
        {
            get
            {
                return $"{"ToolTipDescriptionName".GetLocalized()} {ItemName}{Environment.NewLine}" +
                    $"{"ToolTipDescriptionType".GetLocalized()} {itemType}{Environment.NewLine}" +
                    $"{"ToolTipDescriptionDate".GetLocalized()} {ItemDateModified}";
            }
        }

        public string FolderRelativeId { get; set; }

        public bool ContainsFilesOrFolders { get; set; } = true;

        private bool needsPlaceholderGlyph = true;
        public bool NeedsPlaceholderGlyph
        {
            get => needsPlaceholderGlyph;
            set => SetProperty(ref needsPlaceholderGlyph, value);
        }

        private bool loadFileIcon;
        public bool LoadFileIcon
        {
            get => loadFileIcon;
            set => SetProperty(ref loadFileIcon, value);
        }

        private bool loadDefaultIcon = false;
        public bool LoadDefaultIcon
        {
            get => loadDefaultIcon;
            [Obsolete("The set accessor is used internally and should not be used outside ListedItem and derived classes.")]
            set => SetProperty(ref loadDefaultIcon, value);
        }

        private bool loadWebShortcutGlyph;
        public bool LoadWebShortcutGlyph
        {
            get => loadWebShortcutGlyph;
            set
            {
                if (SetProperty(ref loadWebShortcutGlyph, value))
                {
                    LoadDefaultIcon = !value;
                }
            }
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
                if (SetProperty(ref fileTag, value))
                {
                    FileTagsHelper.DbInstance.SetTag(ItemPath, FileFRN, value);
                    FileTagsHelper.WriteFileTag(ItemPath, value);
                    OnPropertyChanged(nameof(FileTagUI));
                }
            }
        }

        public FileTag FileTagUI
        {
            get => UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled ? FileTagsSettingsService.GetTagById(FileTag) : null;
        }

        private Uri customIconSource;
        public Uri CustomIconSource
        {
            get => customIconSource;
            set => SetProperty(ref customIconSource, value);
        }

        private double opacity;
        public double Opacity
        {
            get => opacity;
            set => SetProperty(ref opacity, value);
        }

        private CloudDriveSyncStatusUI syncStatusUI = new CloudDriveSyncStatusUI();
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

        public BitmapImage FileImage
        {
            get => fileImage;
            set
            {
                if (value is BitmapImage imgOld)
                {
                    imgOld.ImageOpened -= Img_ImageOpened;
                }
                if (SetProperty(ref fileImage, value))
                {
                    if (value is BitmapImage img)
                    {
                        if (img.PixelWidth > 0)
                        {
                            Img_ImageOpened(img, null);
                        }
                        else
                        {
                            img.ImageOpened += Img_ImageOpened;
                        }
                    }
                }
            }
        }

        private void Img_ImageOpened(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is BitmapImage image)
            {
                image.ImageOpened -= Img_ImageOpened;

                if (image.PixelWidth > 0)
                {
                    Common.Extensions.IgnoreExceptions(() =>
                    {
                        LoadFileIcon = true;
                        PlaceholderDefaultIcon = null;
                        NeedsPlaceholderGlyph = false;
                        LoadDefaultIcon = false;
                        LoadWebShortcutGlyph = false;
                    }, App.Logger); // 2009482836u
                }
            }
        }

        public bool IsItemPinnedToStart => App.SecondaryTileHelper.CheckFolderPinned(ItemPath);

        private BitmapImage iconOverlay;

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

        private BitmapImage placeholderDefaultIcon;
        public BitmapImage PlaceholderDefaultIcon
        {
            get => placeholderDefaultIcon;
            set => SetProperty(ref placeholderDefaultIcon, value);
        }

        private string itemPath;
        public string ItemPath
        {
            get => itemPath;
            set => SetProperty(ref itemPath, value);
        }

        private string itemNameRaw;
        public string ItemNameRaw
        {
            get => itemNameRaw;
            set
            {
                if (SetProperty(ref itemNameRaw, value))
                {
                    OnPropertyChanged(nameof(ItemName));
                }
            }
        }

        public virtual string ItemName
        {
            get
            {
                if (PrimaryItemAttribute == StorageItemTypes.File)
                {
                    var nameWithoutExtension = Path.GetFileNameWithoutExtension(itemNameRaw);
                    if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
                    {
                        return nameWithoutExtension;
                    }
                }
                return itemNameRaw;
            }
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

        public string FileSizeDisplay => string.IsNullOrEmpty(FileSize) ? "ItemSizeNotCalculated".GetLocalized() : FileSize;

        public long FileSizeBytes { get; set; }

        public string ItemDateModified { get; private set; }

        public string ItemDateCreated { get; private set; }

        public string ItemDateAccessed { get; private set; }

        private DateTimeOffset itemDateModifiedReal;
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

        private DateTimeOffset itemDateCreatedReal;
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

        private DateTimeOffset itemDateAccessedReal;
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
        public ListedItem() { }

        protected string DateReturnFormat { get; }

        private ObservableCollection<FileProperty> fileDetails;
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
        public RecycleBinItem AsRecycleBinItem => this as RecycleBinItem;

        public string Key { get; set; }

        /// <summary>
        /// Manually check if a folder path contains child items, 
        /// updating the ContainsFilesOrFolders property from its default value of true
        /// </summary>
        public void UpdateContainsFilesFolders()
        {
            ContainsFilesOrFolders = FolderHelpers.CheckForFilesFolders(ItemPath);
        }

        public void SetDefaultIcon(BitmapImage img)
        {
            NeedsPlaceholderGlyph = false;
            LoadDefaultIcon = true;
            PlaceholderDefaultIcon = img;
        }
    }

    public class RecycleBinItem : ListedItem
    {
        public RecycleBinItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

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
            ItemNameRaw = item.Name;
            FileExtension = Path.GetExtension(item.Name);
            ItemPath = PathNormalization.Combine(folder, item.Name);
            PrimaryItemAttribute = isFile ? StorageItemTypes.File : StorageItemTypes.Folder;
            ItemPropertiesInitialized = false;

            var itemType = isFile ? "ItemTypeFile".GetLocalized() : "FileFolderListItem".GetLocalized();
            if (isFile && ItemName.Contains(".", StringComparison.Ordinal))
            {
                itemType = FileExtension.Trim('.') + " " + itemType;
            }

            ItemType = itemType;
            FileSizeBytes = item.Size;
            ContainsFilesOrFolders = !isFile;
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

        public override string ItemName => Path.GetFileNameWithoutExtension(ItemNameRaw); // Always hide extension

        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool IsUrl { get; set; }
        public bool IsSymLink { get; set; }
        public override bool IsExecutable => string.Equals(Path.GetExtension(TargetPath), ".exe", StringComparison.OrdinalIgnoreCase);
    }

    public class ZipItem : ListedItem
    {
        public ZipItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        public override string ItemName
        {
            get
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(ItemNameRaw);
                if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.PreferencesSettingsService.ShowFileExtensions)
                {
                    return nameWithoutExtension;
                }
                return ItemNameRaw;
            }
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
            ItemNameRaw = lib.Text;
            PrimaryItemAttribute = StorageItemTypes.Folder;
            ItemType = "ItemTypeLibrary".GetLocalized();
            LoadCustomIcon = true;
            CustomIcon = lib.Icon;
            //CustomIconSource = lib.IconSource;
            LoadFileIcon = true;

            IsEmpty = lib.IsEmpty;
            DefaultSaveFolder = lib.DefaultSaveFolder;
            Folders = lib.Folders;
        }

        public bool IsEmpty { get; }

        public string DefaultSaveFolder { get; }

        public override string ItemName => ItemNameRaw;

        public ReadOnlyCollection<string> Folders { get; }
    }
}