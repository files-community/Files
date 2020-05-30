using Files.Enums;
using System;
using System.ComponentModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem : INotifyPropertyChanged
    {
        public StorageItemTypes PrimaryItemAttribute { get; set; }
        public bool ItemPropertiesInitialized { get; set; } = false;
        public string FolderTooltipText { get; set; }
        public string FolderRelativeId { get; set; }
        public bool LoadFolderGlyph { get; set; }
        private bool _LoadFileIcon;

        public bool LoadFileIcon
        {
            get
            {
                return _LoadFileIcon;
            }
            set
            {
                if (_LoadFileIcon != value)
                {
                    _LoadFileIcon = value;
                    NotifyPropertyChanged("LoadFileIcon");
                }
            }
        }

        private bool _LoadUnknownTypeGlyph;

        public bool LoadUnknownTypeGlyph
        {
            get
            {
                return _LoadUnknownTypeGlyph;
            }
            set
            {
                if (_LoadUnknownTypeGlyph != value)
                {
                    _LoadUnknownTypeGlyph = value;
                    NotifyPropertyChanged("LoadUnknownTypeGlyph");
                }
            }
        }

        private BitmapImage _FileImage;

        public BitmapImage FileImage
        {
            get
            {
                return _FileImage;
            }
            set
            {
                if (_FileImage != value && value != null)
                {
                    _FileImage = value;
                    NotifyPropertyChanged("FileImage");
                }
            }
        }

        public string ItemName { get; set; }
        public string ItemDateModified { get; private set; }
        private string _ItemType;

        public string ItemType
        {
            get
            {
                return _ItemType;
            }
            set
            {
                if (_ItemType != value && value != null)
                {
                    _ItemType = value;
                    NotifyPropertyChanged("ItemType");
                }
            }
        }

        public string FileExtension { get; set; }
        public string ItemPath { get; set; }
        public string FileSize { get; set; }
        public ulong FileSizeBytes { get; set; }
        // For recycle bin elements (path + name)
        public string ItemOriginalPath { get; set; }

        public DateTimeOffset ItemDateModifiedReal
        {
            get { return _itemDateModifiedReal; }
            set
            {
                ItemDateModified = GetFriendlyDate(value);
                _itemDateModifiedReal = value;
            }
        }

        private DateTimeOffset _itemDateModifiedReal;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        public ListedItem(string folderRelativeId)
        {
            FolderRelativeId = folderRelativeId;
        }

        public static string GetFriendlyDate(DateTimeOffset d)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            var elapsed = DateTimeOffset.Now - d;

            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values[LocalSettings.DateTimeFormat].ToString()) == TimeStyle.Application ? "D" : "g";

            if (elapsed.TotalDays > 7)
            {
                return d.ToString(returnformat);
            }
            else if (elapsed.TotalDays > 1)
            {
                return $"{elapsed.Days} days ago";
            }
            else if (elapsed.TotalHours > 1)
            {
                return $"{elapsed.Hours} hours ago";
            }
            else if (elapsed.TotalMinutes > 1)
            {
                return $"{elapsed.Minutes} minutes ago";
            }
            else
            {
                return $"{elapsed.Seconds} seconds ago";
            }
        }
    }
}