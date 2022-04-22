using Files.Shared.Enums;
using Files.Shared.Extensions;
using System.Collections.Generic;

namespace Files.Uwp.Filesystem.FilesystemHistory
{
    public class StorageHistory : IStorageHistory
    {
        public FileOperationType OperationType { get; private set; }

        public IList<IStorageItemWithPath> Source { get; private set; }
        public IList<IStorageItemWithPath> Destination { get; private set; }

        public StorageHistory(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
            => (OperationType, Source, Destination) = (operationType, source.CreateList(), destination.CreateList());
        public StorageHistory(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
            => (OperationType, Source, Destination) = (operationType, source, destination);

        public void Modify(IStorageHistory newHistory)
            => (OperationType, Source, Destination) = (newHistory.OperationType, newHistory.Source, newHistory.Destination);
        public void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
            => (OperationType, Source, Destination) = (operationType, source.CreateList(), destination.CreateList());
        public void Modify(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
            => (OperationType, Source, Destination) = (operationType, source, destination);

        public void Dispose() => (Source, Destination) = (null, null);
    }
}