using System.Threading.Tasks;


namespace Files.Filesystem.FilesystemHistory
{
    public interface IStorageHistoryOperations
    {
        Task Undo(IStorageHistory history);

        Task Redo(IStorageHistory history);
    }
}
