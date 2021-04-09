using Files.Enums;
using Files.Filesystem.Cloud;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
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

            IsEmpty = lib.IsEmpty;
            DefaultSaveFolder = lib.DefaultSaveFolder;
            Folders = lib.Folders;
        }

        public string DefaultSaveFolder { get; }
        public ReadOnlyCollection<string> Folders { get; }
        public bool IsEmpty { get; }
    }

    public class ListedItem : ObservableObject
    {
        private SvgImageSource customIcon;
        private ObservableCollection<FileProperty> fileDetails;
        private BitmapImage fileImage;
        private BitmapImage iconOverlay;
        private DateTimeOffset itemDateAccessedReal;
        private DateTimeOffset itemDateCreatedReal;
        private DateTimeOffset itemDateModifiedReal;
        private StorageFile itemFile;
        private string itemName;
        private string itemPath;
        private ObservableCollection<FileProperty> itemProperties;
        private string itemType;
        private bool loadCustomIcon;
        private bool loadFileIcon;
        private bool loadFolderGlyph;
        private bool loadUnknownTypeGlyph;
        private bool loadWebShortcutGlyph;
        private double opacity;
        private CloudDriveSyncStatusUI syncStatusUI;

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

        public bool ContainsFilesOrFolders { get; set; }

        public SvgImageSource CustomIcon
        {
            get => customIcon;
            set
            {
                LoadCustomIcon = true;
                SetProperty(ref customIcon, value);
            }
        }

        [JsonIgnore]
        public ObservableCollection<FileProperty> FileDetails
        {
            get => fileDetails;
            set => SetProperty(ref fileDetails, value);
        }

        public string FileExtension { get; set; }

        [JsonIgnore]
        public BitmapImage FileImage
        {
            get => fileImage;
            set
            {
                if (value != null)
                {
                    SetProperty(ref fileImage, value);
                }
            }
        }

        public string FileSize { get; set; }
        public long FileSizeBytes { get; set; }

        public Uri FolderIconSource
        {
            get
            {
                return ContainsFilesOrFolders ? new Uri("ms-appx:///Assets/FolderIcon2.svg") : new Uri("ms-appx:///Assets/FolderIcon.svg");
            }
        }

        public Uri FolderIconSourceLarge
        {
            get
            {
                return ContainsFilesOrFolders ? new Uri("ms-appx:///Assets/FolderIcon2Large.svg") : new Uri("ms-appx:///Assets/FolderIconLarge.svg");
            }
        }

        public string FolderRelativeId { get; set; }
        public string FolderTooltipText { get; set; }

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

        public bool IsHiddenItem { get; set; } = false;
        public bool IsItemPinnedToStart => App.SecondaryTileHelper.CheckFolderPinned(ItemPath);
        public bool IsLibraryItem => this is LibraryItem;
        public bool IsLinkItem => IsShortcutItem && ((ShortcutItem)this).IsUrl;
        public bool IsPinned => App.SidebarPinnedController.Model.FavoriteItems.Contains(itemPath);
        public bool IsRecycleBinItem => this is RecycleBinItem;
        public bool IsShortcutItem => this is ShortcutItem;
        public string ItemDateAccessed { get; private set; }

        public DateTimeOffset ItemDateAccessedReal
        {
            get => itemDateAccessedReal;
            set
            {
                ItemDateAccessed = GetFriendlyDateFromFormat(value, DateReturnFormat);
                itemDateAccessedReal = value;
            }
        }

        public string ItemDateCreated { get; private set; }

        public DateTimeOffset ItemDateCreatedReal
        {
            get => itemDateCreatedReal;
            set
            {
                ItemDateCreated = GetFriendlyDateFromFormat(value, DateReturnFormat);
                itemDateCreatedReal = value;
            }
        }

        public string ItemDateModified { get; private set; }

        public DateTimeOffset ItemDateModifiedReal
        {
            get => itemDateModifiedReal;
            set
            {
                ItemDateModified = GetFriendlyDateFromFormat(value, DateReturnFormat);
                itemDateModifiedReal = value;
            }
        }

        public StorageFile ItemFile
        {
            get => itemFile;
            set => SetProperty(ref itemFile, value);
        }

        public string ItemName
        {
            get => itemName;
            set => SetProperty(ref itemName, value);
        }

        public string ItemPath
        {
            get => itemPath;
            set => SetProperty(ref itemPath, value);
        }

        public ObservableCollection<FileProperty> ItemProperties
        {
            get => itemProperties;
            set => SetProperty(ref itemProperties, value);
        }

        public bool ItemPropertiesInitialized { get; set; } = false;

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

        public bool LoadUnknownTypeGlyph
        {
            get => loadUnknownTypeGlyph;
            set => SetProperty(ref loadUnknownTypeGlyph, value);
        }

        public bool LoadWebShortcutGlyph
        {
            get => loadWebShortcutGlyph;
            set => SetProperty(ref loadWebShortcutGlyph, value);
        }

        public double Opacity
        {
            get => opacity;
            set => SetProperty(ref opacity, value);
        }

        public StorageItemTypes PrimaryItemAttribute { get; set; }

        [JsonIgnore]
        public CloudDriveSyncStatusUI SyncStatusUI
        {
            get => syncStatusUI;
            set => SetProperty(ref syncStatusUI, value);
        }

        protected string DateReturnFormat { get; }

        public static string GetFriendlyDateFromFormat(DateTimeOffset d, string returnFormat)
        {
            var elapsed = DateTimeOffset.Now - d;

            if (elapsed.TotalDays > 7 || returnFormat == "g")
            {
                return d.ToLocalTime().ToString(returnFormat);
            }
            else if (elapsed.TotalDays > 2)
            {
                return string.Format("DaysAgo".GetLocalized(), elapsed.Days);
            }
            else if (elapsed.TotalDays > 1)
            {
                return string.Format("DayAgo".GetLocalized(), elapsed.Days);
            }
            else if (elapsed.TotalHours > 2)
            {
                return string.Format("HoursAgo".GetLocalized(), elapsed.Hours);
            }
            else if (elapsed.TotalHours > 1)
            {
                return string.Format("HoursAgo".GetLocalized(), elapsed.Hours);
            }
            else if (elapsed.TotalMinutes > 2)
            {
                return string.Format("MinutesAgo".GetLocalized(), elapsed.Minutes);
            }
            else if (elapsed.TotalMinutes > 1)
            {
                return string.Format("MinutesAgo".GetLocalized(), elapsed.Minutes);
            }
            else
            {
                return string.Format("SecondsAgo".GetLocalized(), elapsed.Seconds);
            }
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
            return $"{ItemName}, {ItemPath}, {suffix}";
        }
    }

    public class RecycleBinItem : ListedItem
    {
        private DateTimeOffset itemDateDeletedReal;

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
                ItemDateDeleted = GetFriendlyDateFromFormat(value, DateReturnFormat);
                itemDateDeletedReal = value;
            }
        }

        // For recycle bin elements (path)
        public string ItemOriginalFolder => Path.IsPathRooted(ItemOriginalPath) ? Path.GetDirectoryName(ItemOriginalPath) : ItemOriginalPath;

        // For recycle bin elements (path + name)
        public string ItemOriginalPath { get; set; }
    }

    public class ShortcutItem : ListedItem
    {
        public ShortcutItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        // Parameterless constructor for JsonConvert
        public ShortcutItem() : base()
        { }

        public string Arguments { get; set; }

        public bool IsUrl { get; set; }

        public bool RunAsAdmin { get; set; }

        // For shortcut elements (.lnk and .url)
        public string TargetPath { get; set; }

        public string WorkingDirectory { get; set; }
    }
}