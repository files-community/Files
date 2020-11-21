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
            this.OperationType = operationType;
            this.Source = source;
            this.Destination = destination;
        }

        public StorageHistory(FileOperationType operationType, PathWithType source, PathWithType destination)
        {
            this.OperationType = operationType;
            this.Source = source.CreateEnumerable();
            this.Destination = destination.CreateEnumerable();
        }

        #endregion

        #region Modify

        public void Modify(IStorageHistory newHistory)
        {
            this.OperationType = newHistory.OperationType;
            this.Source = newHistory.Source;
            this.Destination = newHistory.Destination;
        }

        public void Modify(FileOperationType operationType, IEnumerable<PathWithType> source, IEnumerable<PathWithType> destination)
        {
            this.OperationType = operationType;
            this.Source = source;
            this.Destination = destination;
        }

        public void Modify(FileOperationType operationType, PathWithType source, PathWithType destination)
        {
            this.OperationType = operationType;
            this.Source = source.CreateEnumerable();
            this.Destination = destination.CreateEnumerable();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            this.Source?.ForEach((item) => item?.Dispose());
            this.Destination?.ForEach((item) => item?.Dispose());

            this.Source = null;
            this.Destination = null;
        }

        #endregion
    }
}
