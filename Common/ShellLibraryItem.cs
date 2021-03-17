namespace Files.Common
{
    public class ShellLibraryItem
    {
        public string Path;
        public string Name;
        public bool IsPinned;
        public string DefaultSaveFolder;
        public string[] Folders;

        public ShellLibraryItem() { }

        public ShellLibraryItem(string path, string name, bool isPinned)
        {
            this.Path = path;
            this.Name = name;
            this.IsPinned = isPinned;
        }
    }
}