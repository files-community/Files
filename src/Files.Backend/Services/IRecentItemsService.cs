using Files.Sdk.Storage.LocatableStorage;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Files.Backend.Services
{
	public interface IRecentItemsService
	{
		Task<NotifyCollectionChangedEventArgs> UpdateRecentFilesAsync(List<ILocatableStorable> itemsCollection);
		Task<NotifyCollectionChangedEventArgs> UpdateRecentFoldersAsync(List<ILocatableStorable> itemsCollection);
		Task<IList<ILocatableStorable>> ListRecentFilesAsync();
		Task<IList<ILocatableStorable>> ListRecentFoldersAsync();
		bool AddToRecentItems(string path);
		bool ClearRecentItems();
		Task<bool> UnpinFromRecentFilesAsync(ILocatableStorable item);
		bool IsSupported();

	}
}
