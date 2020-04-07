using Files.Enums;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem : INotifyPropertyChanged
    {
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
                if(_LoadFileIcon != value)
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
                if(_FileImage != value && value != null)
                {
                    _FileImage = value;
                    NotifyPropertyChanged("FileImage");
                }
            }
        }
        public string FileName { get; set; }
        public string FileDate { get; private set; }
        private string _FileType;
        public string FileType
        {
            get
            {
                return _FileType;
            }
            set
            {
                if(_FileType != value && value != null)
                {
                    _FileType = value;
                    NotifyPropertyChanged("FileType");
                }
            }
        }
        public string DotFileExtension { get; set; }
        public string FilePath { get; set; }
        public string FileSize { get; set; }
        public ulong FileSizeBytes { get; set; }

        public DateTimeOffset FileDateReal
        {
            get { return _fileDataReal; }
            set
            {
                FileDate = GetFriendlyDate(value);
                _fileDataReal = value;
            }
        }

        private DateTimeOffset _fileDataReal;

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
