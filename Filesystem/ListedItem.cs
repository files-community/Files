using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem
{
    public class ListedItem
    {
        public Visibility FolderImg { get; set; }
        public Visibility FileIconVis { get; set; }
        public Visibility EmptyImgVis { get; set; }
        public BitmapImage FileImg { get; set; }
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string DotFileExtension { get; set; }
        public string FilePath { get; set; }
        public int ItemIndex { get; set; }
        public ListedItem()
        {

        }
    }
}
