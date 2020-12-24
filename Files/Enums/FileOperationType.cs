using System;

namespace Files.Enums
{
    /// <summary>
    /// Type of operation on Files filesystem that took place
    /// </summary>
    [Flags]
    public enum FileOperationType : byte
    {
        /// <summary>
        /// An item has been created
        /// </summary>
        CreateNew = 0,

        /// <summary>
        /// An item has been renamed
        /// </summary>
        Rename = 1,

        /// <summary>
        /// An item has been copied to destination
        /// </summary>
        Copy = 3,

        /// <summary>
        /// An item has been moved to destination
        /// </summary>
        Move = 4,

        /// <summary>
        /// An item has been extracted
        /// </summary>
        Extract = 5,

        /// <summary>
        /// An item has been recycled
        /// </summary>
        Recycle = 6,

        /// <summary>
        /// An item has been restored from Recycle Bin
        /// </summary>
        Restore = 7,

        /// <summary>
        /// A item has been deleted
        /// </summary>
        Delete = 8
    }
}