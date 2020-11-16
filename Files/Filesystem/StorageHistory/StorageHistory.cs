using Files.Extensions;
using System.Collections.Generic;

namespace Files.Filesystem.FilesystemHistory
{
    public class StorageHistory : IStorageHistory
    {
        #region Public Properties

        public FileOperationType OperationType { get; private set; }

        public IEnumerable<string> Source { get; private set; }

        public IEnumerable<string> Destination { get; private set; }

        #endregion

        #region Constructor

        public StorageHistory(FileOperationType operationType, IEnumerable<string> source, IEnumerable<string> destination)
        {
            this.OperationType = operationType;
            this.Source = source;
            this.Destination = destination;
        }

        public StorageHistory(FileOperationType operationType, string source, string destination)
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

        public void Modify(FileOperationType operationType, IEnumerable<string> source, IEnumerable<string> destination)
        {
            this.OperationType = operationType;
            this.Source = source;
            this.Destination = destination;
        }

        public void Modify(FileOperationType operationType, string source, string destination)
        {
            this.OperationType = operationType;
            this.Source = source.CreateEnumerable();
            this.Destination = destination.CreateEnumerable();
        }

        #endregion
    }
}
