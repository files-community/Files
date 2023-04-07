using Files.App.Extensions;
using Files.Backend.Services;
using Files.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	/// <inheritdoc cref="IAddItemService"/>
	internal sealed class AddItemService : IAddItemService
	{
		private List<ShellNewEntry> _cached;

		public Task<List<ShellNewEntry>> GetNewEntriesAsync()
		{
			if (_cached is null || _cached.Count == 0)
				return ShellNewEntryExtensions.GetNewContextMenuEntries().ContinueWith(t => _cached = t.Result);

			return Task.FromResult(_cached);
		}
	}
}
