namespace Files.Shared.Enums
{
	public enum SortOption : byte
	{
		Name,
		DateModified,
		DateCreated,
		Size,
		FileType,
		FileTag,
		SyncStatus, // Cloud drive
		OriginalFolder, // Recycle bin
		DateDeleted // Recycle bin
	}
}