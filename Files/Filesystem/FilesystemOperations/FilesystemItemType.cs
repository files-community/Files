using System;

namespace Files.Filesystem
{
    public enum FilesystemItemType : byte
    {
        /// <summary>
        /// The item is a file
        /// </summary>
        File = 0,

        /// <summary>
        /// The item is a directory
        /// </summary>
        Directory = 1,

        /// <summary>
        /// The item is a symlink
        /// </summary>
        [Obsolete("The Symlink is never used")]
        Symlink = 2
    }
}
