using System;

namespace Files.Common
{
    public class ShellFileItem
    {
        public bool IsFolder;
        public string RecyclePath;
        public string FileName;
        public string FilePath;
        public DateTime RecycleDate;
        public DateTime ModifiedDate;
        public string FileSize;
        public ulong FileSizeBytes;
        public string FileType;

        public ShellFileItem()
        {
        }

        public ShellFileItem(
            bool isFolder, string recyclePath, string fileName, string filePath,
            DateTime recycleDate, DateTime modifiedDate, string fileSize, ulong fileSizeBytes, string fileType)
        {
            this.IsFolder = isFolder;
            this.RecyclePath = recyclePath;
            this.FileName = fileName;
            this.FilePath = filePath;
            this.RecycleDate = recycleDate;
            this.ModifiedDate = modifiedDate;
            this.FileSize = fileSize;
            this.FileSizeBytes = fileSizeBytes;
            this.FileType = fileType;
        }
    }
}