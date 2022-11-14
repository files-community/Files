using System;

namespace Files.Shared
{
	public class ShellFileItem
	{
		public bool IsFolder { get; set; }
		public string RecyclePath { get; set; }
		public string FileName { get; set; }
		public string FilePath { get; set; }
		public DateTime RecycleDate { get; set; }
		public DateTime ModifiedDate { get; set; }
		public DateTime CreatedDate { get; set; }
		public string FileSize { get; set; }
		public ulong FileSizeBytes { get; set; }
		public string FileType { get; set; }

		public ShellFileItem()
		{
		}

		public ShellFileItem(
			bool isFolder, string recyclePath, string fileName, string filePath,
			DateTime recycleDate, DateTime modifiedDate, DateTime createdDate, string fileSize, ulong fileSizeBytes, string fileType)
		{
			this.IsFolder = isFolder;
			this.RecyclePath = recyclePath;
			this.FileName = fileName;
			this.FilePath = filePath;
			this.RecycleDate = recycleDate;
			this.ModifiedDate = modifiedDate;
			this.CreatedDate = createdDate;
			this.FileSize = fileSize;
			this.FileSizeBytes = fileSizeBytes;
			this.FileType = fileType;
		}
	}
}