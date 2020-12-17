using Files.Enums;
using System;
using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public interface IStorageHistory : IDisposable
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
        IEnumerable<PathWithType> Source { get; }

        /// <summary>
        /// Destination file/folder
        /// <br/>
        /// <br/>
        /// Attention!
        /// <br/>
        /// May contain more that one item
        /// </summary>
        IEnumerable<PathWithType> Destination { get; }

        #region Modify

        void Modify(IStorageHistory newHistory);

        void Modify(FileOperationType operationType, IEnumerable<PathWithType> source, IEnumerable<PathWithType> destination);

        void Modify(FileOperationType operationType, PathWithType source, PathWithType destination);

        #endregion Modify
    }
}