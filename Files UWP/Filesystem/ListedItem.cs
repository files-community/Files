using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem
    {
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

        public DateTimeOffset FileDateReal
        {
            get { return _fileDataReal; }
            set
            {
                FileDate = GetFriendlyDate(value.LocalDateTime);
                _fileDataReal = value;
            }
        }

        private DateTimeOffset _fileDataReal;

        public ListedItem(string folderRelativeId)
        {
            FolderRelativeId = folderRelativeId;
        }

        public static string GetFriendlyDate(DateTime d)
        {
            if (d.Year == DateTime.Now.Year)              // If item is accessed in the same year as stored
            {
                if (d.Month == DateTime.Now.Month)        // If item is accessed in the same month as stored
                {
                    if ((DateTime.Now.Day - d.Day) < 7) // If item is accessed on the same week
                    {
                        if (d.DayOfWeek == DateTime.Now.DayOfWeek)   // If item is accessed on the same day as stored
                        {
                            if ((DateTime.Now.Hour - d.Hour) > 1)
                            {
                                return DateTime.Now.Hour - d.Hour + " hours ago";
                            }
                            else
                            {
                                return DateTime.Now.Minute - d.Minute + " hour ago";
                            }
                        }
                        else                                                        // If item is from a previous day of the same week
                        {
                            return d.DayOfWeek + " at " + d.ToShortTimeString();
                        }
                    }
                    else                                                          // If item is from a previous week of the same month
                    {
                        string monthAsString = "Month";
                        switch (d.Month)
                        {
                            case 1:
                                monthAsString = "January";
                                break;
                            case 2:
                                monthAsString = "February";
                                break;
                            case 3:
                                monthAsString = "March";
                                break;
                            case 4:
                                monthAsString = "April";
                                break;
                            case 5:
                                monthAsString = "May";
                                break;
                            case 6:
                                monthAsString = "June";
                                break;
                            case 7:
                                monthAsString = "July";
                                break;
                            case 8:
                                monthAsString = "August";
                                break;
                            case 9:
                                monthAsString = "September";
                                break;
                            case 10:
                                monthAsString = "October";
                                break;
                            case 11:
                                monthAsString = "November";
                                break;
                            case 12:
                                monthAsString = "December";
                                break;
                        }
                        return monthAsString + " " + d.Day;
                    }

                }
                else                                                            // If item is from a past month of the same year
                {
                    string monthAsString = "Month";
                    switch (d.Month)
                    {
                        case 1:
                            monthAsString = "January";
                            break;
                        case 2:
                            monthAsString = "February";
                            break;
                        case 3:
                            monthAsString = "March";
                            break;
                        case 4:
                            monthAsString = "April";
                            break;
                        case 5:
                            monthAsString = "May";
                            break;
                        case 6:
                            monthAsString = "June";
                            break;
                        case 7:
                            monthAsString = "July";
                            break;
                        case 8:
                            monthAsString = "August";
                            break;
                        case 9:
                            monthAsString = "September";
                            break;
                        case 10:
                            monthAsString = "October";
                            break;
                        case 11:
                            monthAsString = "November";
                            break;
                        case 12:
                            monthAsString = "December";
                            break;
                    }
                    return monthAsString + " " + d.Day;
                }
            }
            else                                                                // If item is from a past year
            {
                string monthAsString = "Month";
                switch (d.Month)
                {
                    case 1:
                        monthAsString = "January";
                        break;
                    case 2:
                        monthAsString = "February";
                        break;
                    case 3:
                        monthAsString = "March";
                        break;
                    case 4:
                        monthAsString = "April";
                        break;
                    case 5:
                        monthAsString = "May";
                        break;
                    case 6:
                        monthAsString = "June";
                        break;
                    case 7:
                        monthAsString = "July";
                        break;
                    case 8:
                        monthAsString = "August";
                        break;
                    case 9:
                        monthAsString = "September";
                        break;
                    case 10:
                        monthAsString = "October";
                        break;
                    case 11:
                        monthAsString = "November";
                        break;
                    case 12:
                        monthAsString = "December";
                        break;
                }
                return monthAsString + " " + d.Day + ", " + d.Year;
            }
        }
    }
}
