using System.Threading.Tasks;


namespace Files.Filesystem.FilesystemHistory
{
    public interface IStorageHistoryOperations
    {
        /// <summary>
        /// Redo an actuon with given <paramref name="history"/>
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        Task Undo(IStorageHistory history);

        /// <summary>
        /// Redo an actuon with given <paramref name="history"/>
        /// </summary>
        /// <param name="history"></param>
        /// <returns></returns>
        Task Redo(IStorageHistory history);
    }
}
