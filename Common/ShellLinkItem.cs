namespace Files.Common
{
    public class ShellLinkItem : ShellFileItem
    {
        public string TargetPath;
        public string Arguments;
        public string WorkingDirectory;
        public bool RunAsAdmin;

        public ShellLinkItem()
        {
        }

        public ShellLinkItem(ShellFileItem baseItem)
        {
            this.RecyclePath = baseItem.RecyclePath;
            this.FileName = baseItem.FileName;
            this.FilePath = baseItem.FilePath;
            this.RecycleDate = baseItem.RecycleDate;
            this.ModifiedDate = baseItem.ModifiedDate;
            this.CreatedDate = baseItem.CreatedDate;
            this.FileSize = baseItem.FileSize;
            this.FileSizeBytes = baseItem.FileSizeBytes;
            this.FileType = baseItem.FileType;
        }
    }
}
