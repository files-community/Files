// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Data.Contracts
{
	public interface IWindowsQuickAccessService
	{
		/// <summary>
		/// Gets invoked when the pinned folder collection has been changed.
		/// </summary>
		event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		/// <summary>
		/// Gets invoked when pinned items have changed.
		/// </summary>
		event EventHandler<ModifyQuickAccessEventArgs>? PinnedItemsChanged;

		/// <summary>
		/// Gets all pinned folder paths.
		/// </summary>
		List<string> PinnedFolderPaths { get; }

		/// <summary>
		/// Gets all pinned folder items.
		/// </summary>
		IReadOnlyList<INavigationControlItem> PinnedFolderItems { get; }

		/// <summary>
		/// Initializes Quick Access item list.
		/// </summary>
		/// <returns></returns>
		Task InitializeAsync();

		/// <summary>
		/// Gets the list of Quick Access items.
		/// </summary>
		/// <returns></returns>
		Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync();

		/// <summary>
		/// Pins folders to the Quick Access list.
		/// </summary>
		/// <param name="folderPaths">The array of folders to pin.</param>
		/// <returns></returns>
		Task PinFolderToSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true);

		/// <summary>
		/// Unpins folders from the Quick Access list.
		/// </summary>
		/// <param name="folderPaths">The array of folders to unpin.</param>
		/// <returns></returns>
		Task UnpinFolderFromSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true);

		/// <summary>
		/// Checks if a folder is pinned to the Quick Access list.
		/// </summary>
		/// <param name="folderPath">The path of the folder.</param>
		/// <returns>true if the item is pinned.</returns>
		bool IsPinnedFolder(string folderPath);

		/// <summary>
		/// Refreshes Quick Access pinned items.
		/// </summary>
		/// <param name="items">The array of items to pin.</param>
		/// <returns></returns>
		Task RefreshPinnedFolders(string[] items);

		/// <summary>
		/// Fetches items from File Explorer.
		/// </summary>
		Task UpdatePinnedFolders();

		/// <summary>
		/// Syncs all pinned items with File Explorer.
		/// </summary>
		Task SyncPinnedItemsAsync();

		/// <summary>
		/// Returns the index of the location item in Sidebar.
		/// </summary>
		/// <param name="path">The path to look up.</param>
		/// <returns>Index of the item.</returns>
		int IndexOf(string path);
	}
}
