namespace Files.Shared.Enums
{
	public enum GroupOption : byte
	{
		None,
		Name,
		DateModified,
		DateCreated,
		Size,
		FileType,
		FileTag,
		SyncStatus, // Cloud drive
		OriginalFolder, // Recycle bin
		DateDeleted, // Recycle bin
		FolderPath, // Libraries
	}
}