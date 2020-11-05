using Files.Enums;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem : ObservableObject
    {
        public StorageItemTypes PrimaryItemAttribute { get; set; }
        public bool ItemPropertiesInitialized { get; set; } = false;
        public string FolderTooltipText { get; set; }
        public string FolderRelativeId { get; set; }
        public bool ContainsFilesOrFolders { get; set; }
        private bool _LoadFolderGlyph;
        private bool _LoadFileIcon;

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

        public bool LoadFolderGlyph
        {
            get => _LoadFolderGlyph;
            set => SetProperty(ref _LoadFolderGlyph, value);
        }

        public bool LoadFileIcon
        {
            get => _LoadFileIcon;
            set => SetProperty(ref _LoadFileIcon, value);
        }

        private bool _LoadUnknownTypeGlyph;

        public bool LoadUnknownTypeGlyph
        {
            get => _LoadUnknownTypeGlyph;
            set => SetProperty(ref _LoadUnknownTypeGlyph, value);
        }

        private bool _IsDimmed;

        public bool IsDimmed
        {
            get => _IsDimmed;
            set => SetProperty(ref _IsDimmed, value);
        }

        private CloudDriveSyncStatusUI _SyncStatusUI;

        public CloudDriveSyncStatusUI SyncStatusUI
        {
            get => _SyncStatusUI;
            set => SetProperty(ref _SyncStatusUI, value);
        }

        private BitmapImage _FileImage;

        public BitmapImage FileImage
        {
            get => _FileImage;
            set
            {
                if (value != null)
                {
                    SetProperty(ref _FileImage, value);
                }
            }
        }

        private BitmapImage _IconOverlay;

        public BitmapImage IconOverlay
        {
            get => _IconOverlay;
            set
            {
                if (value != null)
                {
                    SetProperty(ref _IconOverlay, value);
                }
            }
        }

        private string _ItemPath;

        public string ItemPath
        {
            get => _ItemPath;
            set => SetProperty(ref _ItemPath, value);
        }

        private string _ItemName;

        public string ItemName
        {
            get => _ItemName;
            set => SetProperty(ref _ItemName, value);
        }

        private string _ItemType;

        public string ItemType
        {
            get => _ItemType;
            set
            {
                if (value != null)
                {
                    SetProperty(ref _ItemType, value);
                }
            }
        }

        public string FileExtension { get; set; }
        public string FileSize { get; set; }
        public long FileSizeBytes { get; set; }

        public string ItemDateModified { get; private set; }
        public string ItemDateCreated { get; private set; }
        public string ItemDateAccessed { get; private set; }

        public DateTimeOffset ItemDateModifiedReal
        {
            get => _itemDateModifiedReal;
            set
            {
                ItemDateModified = GetFriendlyDateFromFormat(value, DateReturnFormat);
                _itemDateModifiedReal = value;
            }
        }

        private DateTimeOffset _itemDateModifiedReal;

        public DateTimeOffset ItemDateCreatedReal
        {
            get => _itemDateCreatedReal;
            set
            {
                ItemDateCreated = GetFriendlyDateFromFormat(value, DateReturnFormat);
                _itemDateCreatedReal = value;
            }
        }

        private DateTimeOffset _itemDateCreatedReal;

        public DateTimeOffset ItemDateAccessedReal
        {
            get => _itemDateAccessedReal;
            set
            {
                ItemDateAccessed = GetFriendlyDateFromFormat(value, DateReturnFormat);
                _itemDateAccessedReal = value;
            }
        }

        private DateTimeOffset _itemDateAccessedReal;

        public bool IsImage()
        {
            if (FileExtension != null)
            {
                string lower = FileExtension.ToLower();
                return lower.Contains("png") || lower.Contains("jpg") || lower.Contains("gif") || lower.Contains("jpeg");
            }
            return false;
        }

        /// <summary>
        /// Create an item object, optionally with an explicitly-specified dateReturnFormat.
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
                string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";
                DateReturnFormat = returnformat;
            }
        }

        private string DateReturnFormat { get; }

        public static string GetFriendlyDateFromFormat(DateTimeOffset d, string returnFormat)
        {
            var elapsed = DateTimeOffset.Now - d;

            if (elapsed.TotalDays > 7 || returnFormat == "g")
            {
                return d.ToString(returnFormat);
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
            if (IsRecycleBinItem) suffix = "RecycleBinItemAutomation".GetLocalized();
            else if (IsShortcutItem) suffix = "ShortcutItemAutomation".GetLocalized();
            else
            {
                if (PrimaryItemAttribute == StorageItemTypes.File) suffix = "FileItemAutomation".GetLocalized();
                else suffix = "FolderItemAutomation".GetLocalized();
            }
            return $"{ItemName}, {ItemPath}, {suffix}";
        }

        public bool IsRecycleBinItem => this is RecycleBinItem;
        public bool IsShortcutItem => this is ShortcutItem;
        public bool IsLinkItem => IsShortcutItem && ((ShortcutItem)this).IsUrl;
    }

    public class RecycleBinItem : ListedItem
    {
        public RecycleBinItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        // For recycle bin elements (path + name)
        public string ItemOriginalPath { get; set; }
    }

    public class ShortcutItem : ListedItem
    {
        public ShortcutItem(string folderRelativeId, string returnFormat) : base(folderRelativeId, returnFormat)
        {
        }

        // For shortcut elements (.lnk and .url)
        public string TargetPath { get; set; }

        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool IsUrl { get; set; }
    }
}