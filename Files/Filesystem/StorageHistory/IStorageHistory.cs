using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public interface IStorageHistory
    {
        // TODO: Is this better?:
        // Tuple<FileOperationType, IEnumerable<object>, IEnumerable<object>> History { get; } 

        /// <summary>
        /// Type of operation that took place
        /// </summary>
        FileOperationType OperationType { get; }

        /// <summary>
        /// Source file/folder
        /// <br/>
        /// <br/>
        /// Attention!
        /// <br/>
        /// May contain more that one item
        /// </summary>
        IEnumerable<object> Source { get; }

        /// <summary>
        /// Destination file/folder
        /// <br/>
        /// <br/>
        /// Attention!
        /// <br/>
        /// May contain more that one item
        /// </summary>
        IEnumerable<object> Destination { get; }
    }
}
