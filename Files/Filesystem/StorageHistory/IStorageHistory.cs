using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public interface IStorageHistory
    {
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
        IEnumerable<string> Source { get; }

        /// <summary>
        /// Destination file/folder
        /// <br/>
        /// <br/>
        /// Attention!
        /// <br/>
        /// May contain more that one item
        /// </summary>
        IEnumerable<string> Destination { get; }

        #region Modify

        void Modify(IStorageHistory newHistory);

        void Modify(FileOperationType operationType, IEnumerable<string> source, IEnumerable<string> destination);

        void Modify(FileOperationType operationType, string source, string destination);

        #endregion
    }
}
