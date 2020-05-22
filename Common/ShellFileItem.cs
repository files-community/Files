using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Files.Common
{
    public class ShellFileItem
    {
        public bool isFolder;
        public string recyclePath;
        public string fileName;
        public string filePath;
        public DateTime recycleDate;
        public string fileSize;
        public int fileSizeBytes;
        public string fileType;

        public ShellFileItem()
        {

        }

        public ShellFileItem(bool isFolder, string recyclePath, string fileName, string filePath, DateTime recycleDate, string fileSize, int fileSizeBytes, string fileType)
        {
            this.isFolder = isFolder;
            this.recyclePath = recyclePath;
            this.fileName = fileName;
            this.filePath = filePath;
            this.recycleDate = recycleDate;
            this.fileSize = fileSize;
            this.fileSizeBytes = fileSizeBytes;
            this.fileType = fileType;
        }
    }
}
