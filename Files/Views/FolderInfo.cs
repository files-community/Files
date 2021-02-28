using Files.Filesystem;

namespace Files.Views.Pages
{
    public class FolderInfo
    {
        public int RootBladeNumber { get; set; }
        public ListedItem Folder { get; set; }
        public string Path { get; set; }
    }
}