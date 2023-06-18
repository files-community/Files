using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;

namespace Files.Backend.Services
{
	public interface IGitRepositoryService
	{
		/// <summary>
		/// Creates a watcher for git repositories
		/// </summary>
		/// <returns>The created git repository watcher</returns>
		IWatcher CreateWatcher(ILocatableFolder folder);
	}
}
