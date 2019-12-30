using Files.Enums;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem
    {
        public string FolderTooltipText { get; set; }
        public string FolderRelativeId { get; }
        public Visibility FolderImg { get; set; }
        public Visibility FileIconVis { get; set; }
        public Visibility EmptyImgVis { get; set; }
        public BitmapImage FileImg { get; set; }
        public string FileName { get; set; }
        public string FileDate { get; private set; }
        public string FileType { get; set; }
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
