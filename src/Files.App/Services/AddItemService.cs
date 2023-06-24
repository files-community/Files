﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Services
{
	/// <inheritdoc cref="IAddItemService"/>
	internal sealed class AddItemService : IAddItemService
	{
		private List<ShellNewEntry> _cached;

		public async Task<List<ShellNewEntry>> GetNewEntriesAsync()
		{
			if (_cached is null || _cached.Count == 0)
				_cached = await ShellNewEntryExtensions.GetNewContextMenuEntries();

			return _cached;
		}
	}
}
