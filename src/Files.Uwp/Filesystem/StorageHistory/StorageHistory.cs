using Files.Shared.Enums;
using Files.Shared.Extensions;
using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistory : IStorageHistory
    {
        #region Public Properties

        public FileOperationType OperationType { get; private set; }

        public IList<IStorageItemWithPath> Source { get; private set; }

        public IList<IStorageItemWithPath> Destination { get; private set; }

        #endregion Public Properties

        #region Constructor

        public StorageHistory(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
        }

        public StorageHistory(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
        {
            OperationType = operationType;
            Source = source.CreateList();
            Destination = destination.CreateList();
        }

        #endregion Constructor

        #region Modify

        public void Modify(IStorageHistory newHistory)
        {
            OperationType = newHistory.OperationType;
            Source = newHistory.Source;
            Destination = newHistory.Destination;
        }

        public void Modify(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
        {
            OperationType = operationType;
            Source = source;
            Destination = destination;
        }

        public void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
        {
            OperationType = operationType;
            Source = source.CreateList();
            Destination = destination.CreateList();
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