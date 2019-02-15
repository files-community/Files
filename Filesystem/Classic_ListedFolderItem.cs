using System.Collections.ObjectModel;

namespace Files.Filesystem
{
    public class Classic_ListedFolderItem
    {
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public ObservableCollection<Classic_ListedFolderItem> Children { get; set; } = new ObservableCollection<Classic_ListedFolderItem>();
    }
}