namespace Files
{
    /// <summary>
    /// Type of operation on Abstract Explorer filesystem that took place
    /// </summary>
    public enum FileOperationType
    {
        /// <summary>
        /// A file/folder has been created
        /// </summary>
        CreateNew = 0,

        /// <summary>
        /// A file/folder has been renamed
        /// </summary>
        Rename = 1,

        /// <summary>
        /// A file/folder has been copied to destination
        /// </summary>
        Copy = 3,

        /// <summary>
        /// A file/folder has been moved to destination
        /// </summary>
        Move = 4,

        /// <summary>
        /// A file has been extracted
        /// </summary>
        Extract = 5,

        /// <summary>
        /// A file/folder has been recycled
        /// </summary>
        Recycle = 6,

        /// <summary>
        /// A file/folder has been restored from recycle bin
        /// </summary>
        Restore = 7,

        /// <summary>
        /// A file/folder has been deleted
        /// </summary>
        Delete = 8
    }
}
