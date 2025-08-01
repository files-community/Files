// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Search
{
	public interface IEverythingSearchService
	{
		bool IsEverythingAvailable();
		Task<List<ListedItem>> SearchAsync(string query, string searchPath = null, CancellationToken cancellationToken = default);
		Task<List<ListedItem>> FilterItemsAsync(IEnumerable<ListedItem> items, string query, CancellationToken cancellationToken = default);
	}
}