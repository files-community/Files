using System.Collections.Generic;


namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistory : IStorageHistory
    {
        public FileOperationType OperationType { get; private set; }

        public IEnumerable<object> Source { get; private set; }

        public IEnumerable<object> Destination { get; private set; }

        public StorageHistory(FileOperationType operationType, IEnumerable<object> source, IEnumerable<object> destination)
        {
            this.OperationType = operationType;
            this.Source = source;
            this.Destination = destination;
        }

        public void Extend(FileOperationType operationType, IEnumerable<object> source, IEnumerable<object> destination)
        {
            this.OperationType = operationType;
            this.Source = source;
            this.Destination = destination;
        }
    }
}
