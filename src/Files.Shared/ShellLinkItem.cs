namespace Files.Shared
{
    public class ShellLinkItem : ShellFileItem
    {
        public string TargetPath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool InvalidTarget { get; set; }

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
