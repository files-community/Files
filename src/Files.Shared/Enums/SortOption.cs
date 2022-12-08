namespace Files.Shared.Enums
{
	public enum SortOption : byte
	{
		Name,
		DateModified,
		DateCreated,
		Size,
		FileType,
		SyncStatus, // Cloud drive
		FileTag,
		OriginalFolder, // Recycle bin
		DateDeleted // Recycle bin
	}
}