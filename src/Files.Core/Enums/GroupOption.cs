namespace Files.Core.Enums
{
	public enum GroupOption : byte
	{
		None,
		Name,
		DateModified,
		DateCreated,
		Size,
		FileType,
		SyncStatus, // Cloud drive
		FileTag,
		OriginalFolder, // Recycle bin
		DateDeleted, // Recycle bin
		FolderPath, // Libraries
	}
}