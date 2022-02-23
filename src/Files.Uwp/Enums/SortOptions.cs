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
        SyncStatus, // Cloud drive
        FileTag,
        OriginalFolder, // Recycle bin
        DateDeleted, // Recycle bin
        FolderPath, // Libraries
    }

    public enum SortDirection : byte // We cannot use Microsoft.Toolkit.Uwp.UI.SortDirection since it's UI-tied and we need Model-tied
    {
        Ascending = 0,
        Descending = 1
    }
}