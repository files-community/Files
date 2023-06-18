using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;
using Files.Sdk.Storage.MutableStorage;

namespace Files.App.ServicesImplementation
{
	public class GitRepositoryService : IGitRepositoryService
	{
		public IWatcher CreateWatcher(ILocatableFolder folder)
		{
			return new GitRepositoryWatcher(folder);
		}
	}
}
