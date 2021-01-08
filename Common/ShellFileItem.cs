using System;

namespace Files.Common
{
    public class ShellFileItem
    {
        public bool IsFolder;
        public string RecyclePath;
        public string FileName;
        public string FilePath;
        public long RecycleDate;
        public string FileSize;
        public ulong FileSizeBytes;
        public string FileType;

        public DateTime RecycleDateDT => DateTime.FromFileTimeUtc(RecycleDate).ToLocalTime();

        public ShellFileItem()
        {
        }

        public ShellFileItem(
            bool isFolder, string recyclePath, string fileName, string filePath,
            long recycleDate, string fileSize, ulong fileSizeBytes, string fileType)
        {
            this.IsFolder = isFolder;
            this.RecyclePath = recyclePath;
            this.FileName = fileName;
            this.FilePath = filePath;
            this.RecycleDate = recycleDate;
            this.FileSize = fileSize;
            this.FileSizeBytes = fileSizeBytes;
            this.FileType = fileType;
        }
    }
}