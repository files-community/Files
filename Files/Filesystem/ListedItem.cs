using Files.Enums;
using System;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem : INotifyPropertyChanged
    {
        public string FolderTooltipText { get; set; }
        public string FolderRelativeId { get; set; }
        public Visibility FolderImg { get; set; }
        private Visibility _FileIconVis;
        public Visibility FileIconVis
        {
            get
            {
                return _FileIconVis;
            }
            set
            {
                if(_FileIconVis != value)
                {
                    _FileIconVis = value;
                    NotifyPropertyChanged("FileIconVis");
                }
            }
        }
        private Visibility _EmptyImgVis;
        public Visibility EmptyImgVis
        {
            get
            {
                return _EmptyImgVis;
            }
            set
            {
                if (_EmptyImgVis != value)
                {
                    _EmptyImgVis = value;
                    NotifyPropertyChanged("EmptyImgVis");
                }
            }
        }
        private BitmapImage _FileImg;
        public BitmapImage FileImg
        {
            get
            {
                return _FileImg;
            }
            set
            {
                if(_FileImg != value && value != null)
                {
                    _FileImg = value;
                    NotifyPropertyChanged("FileImg");
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
            string returnformat = Enum.Parse<TimeStyle>(localSettings.Values["datetimeformat"].ToString()) == TimeStyle.Application ? "D" : "g";

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
