// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public class ShellFileItem
	{
		public bool IsFolder { get; set; }

		public string RecyclePath { get; set; } = string.Empty;

		public string FileName { get; set; } = string.Empty;

		public string FilePath { get; set; } = string.Empty;

		public DateTime RecycleDate { get; set; }

		public DateTime ModifiedDate { get; set; }

		public DateTime CreatedDate { get; set; }

		public string FileSize { get; set; } = string.Empty;

		public ulong FileSizeBytes { get; set; }

		public string FileType { get; set; } = string.Empty;

		public byte[] PIDL { get; set; } = []; // Low level shell item identifier

		public Dictionary<string, object?> Properties { get; set; }

		public ShellFileItem()
		{
			Properties = [];
		}

		public ShellFileItem(bool isFolder, string? recyclePath, string? fileName, string? filePath, DateTime recycleDate, DateTime modifiedDate, DateTime createdDate, string? fileSize, ulong fileSizeBytes, string? fileType, byte[] pidl) : this()
		{
			IsFolder = isFolder;
			RecyclePath = recyclePath ?? string.Empty;
			FileName = fileName ?? string.Empty;
			FilePath = filePath ?? string.Empty;
			RecycleDate = recycleDate;
			ModifiedDate = modifiedDate;
			CreatedDate = createdDate;
			FileSize = fileSize ?? string.Empty;
			FileSizeBytes = fileSizeBytes;
			FileType = fileType ?? string.Empty;
			PIDL = pidl;
		}
	}
}
