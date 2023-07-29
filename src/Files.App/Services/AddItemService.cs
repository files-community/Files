// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	/// <inheritdoc cref="IAddItemService"/>
	internal sealed class AddItemService : IAddItemService
	{
		private List<ShellNewEntry> _cached;

		public async Task InitializeAsync()
		{
			_cached = await ShellNewEntryExtensions.GetNewContextMenuEntries();
		}

		public List<ShellNewEntry> GetEntries()
		{
			return _cached;
		}
	}
}
