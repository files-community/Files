using Files.Enums;
using Files.Extensions;
using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistory : IStorageHistory
    {
        #region Public Properties

        public FileOperationType OperationType { get; private set; }

        public IEnumerable<PathWithType> Source { get; private set; }

        public IEnumerable<PathWithType> Destination { get; private set; }

        #endregion

        #region Constructor

        public StorageHistory(FileOperationType operationType, IEnumerable<PathWithType> source, IEnumerable<PathWithType> destination)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
        }

        public StorageHistory(FileOperationType operationType, PathWithType source, PathWithType destination)
        {
            OperationType = operationType;
            Source = source.CreateEnumerable();
            Destination = destination.CreateEnumerable();
        }

        #endregion

        #region Modify

        public void Modify(IStorageHistory newHistory)
        {
            OperationType = newHistory.OperationType;
            Source = newHistory.Source;
            Destination = newHistory.Destination;
        }

        public void Modify(FileOperationType operationType, IEnumerable<PathWithType> source, IEnumerable<PathWithType> destination)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
        }

        public void Modify(FileOperationType operationType, PathWithType source, PathWithType destination)
        {
            OperationType = operationType;
            Source = source.CreateEnumerable();
            Destination = destination.CreateEnumerable();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Source?.ForEach((item) => item?.Dispose());
            Destination?.ForEach((item) => item?.Dispose());

            Source = null;
            Destination = null;
        }

        #endregion
    }
}
