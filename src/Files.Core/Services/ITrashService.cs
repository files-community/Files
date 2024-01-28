// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Services
{
	public interface ITrashService
	{
		ITrashWatcher Watcher { get; }

		Task<List<ShellFileItem>> GetAllItemsAsync();

		ulong GetTrashSize();

		bool HasItems();

		bool IsTrashed(string path);

		Task EmptyTrashAsync();

		Task RestoreTrashAsync();

		Task<bool> CanBeTrashed(string path);
	}
}
