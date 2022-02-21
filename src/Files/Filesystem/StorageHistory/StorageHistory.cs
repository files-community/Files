using Files.Enums;
using Files.Extensions;
using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistory : IStorageHistory
    {
        #region Public Properties

        public FileOperationType OperationType { get; private set; }

        public IEnumerable<IStorageItemWithPath> Source { get; private set; }

        public IEnumerable<IStorageItemWithPath> Destination { get; private set; }

        #endregion Public Properties

        #region Constructor

        public StorageHistory(FileOperationType operationType, IEnumerable<IStorageItemWithPath> source, IEnumerable<IStorageItemWithPath> destination)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
        }

        public StorageHistory(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
        {
            OperationType = operationType;
            Source = source.CreateEnumerable();
            Destination = destination.CreateEnumerable();
        }

        #endregion Constructor

        #region Modify

        public void Modify(IStorageHistory newHistory)
        {
            OperationType = newHistory.OperationType;
            Source = newHistory.Source;
            Destination = newHistory.Destination;
        }

        public void Modify(FileOperationType operationType, IEnumerable<IStorageItemWithPath> source, IEnumerable<IStorageItemWithPath> destination)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
        }

        public void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
        {
            OperationType = operationType;
            Source = source.CreateEnumerable();
            Destination = destination.CreateEnumerable();
        }

        #endregion Modify

        #region IDisposable

        public void Dispose()
        {
            Source = null;
            Destination = null;
        }

        #endregion IDisposable
    }
}