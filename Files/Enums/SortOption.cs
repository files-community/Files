namespace Files.Enums
{
    public enum SortOption : byte
    {
        Name,
        DateModified,
        DateCreated,
        Size,
        FileType,
        SyncStatus,
        FileTag,
        OriginalFolder,
        DateDeleted
    }

    public enum GroupOption : byte
    {
        None,
        Name,
        DateModified,
        DateCreated,
        Size,
        FileType,
        SyncStatus,
        FileTag,
        OriginalFolder,
        DateDeleted
    }
}