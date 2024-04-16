// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Data.Contracts
{
	public interface IQuickAccessService
	{
		event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		event SystemIO.FileSystemEventHandler? PinnedItemsModified;

		event EventHandler<ModifyQuickAccessEventArgs>? UpdateQuickAccessWidget;

		IReadOnlyList<INavigationControlItem> PinnedFolderItems { get; }

		Task InitializeAsync();

		/// <summary>
		/// Gets the list of quick access items
		/// </summary>
		/// <returns></returns>
		Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync();

		/// <summary>
		/// Pins folders to the quick access list
		/// </summary>
		/// <param name="folderPaths">The array of folders to pin</param>
		/// <returns></returns>
		Task PinToSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true);

		/// <summary>
		/// Unpins folders from the quick access list
		/// </summary>
		/// <param name="folderPaths">The array of folders to unpin</param>
		/// <returns></returns>
		Task UnpinFromSidebarAsync(string[] folderPaths, bool invokeQuickAccessChangedEvent = true);

		/// <summary>
		/// Checks if a folder is pinned to the quick access list
		/// </summary>
		/// <param name="folderPath">The path of the folder</param>
		/// <returns>true if the item is pinned</returns>
		bool IsPinnedToSidebar(string folderPath);

		/// <summary>
		/// Saves a state of pinned folder items in the sidebar
		/// </summary>
		/// <param name="items">The array of items to save</param>
		/// <returns></returns>
		Task NotifyPinnedItemsChangesAsync(string[] items);

		/// <summary>
		/// Updates items with the pinned items from the explorer sidebar
		/// </summary>
		Task UpdateItemsWithExplorerAsync();

		/// <summary>
		/// Returns the index of the location item in the navigation sidebar
		/// </summary>
		/// <param name="locationItem">The location item</param>
		/// <returns>Index of the item</returns>
		int IndexOfItem(INavigationControlItem locationItem);

		/// <summary>
		/// CreateLocationItemFromPathAsync
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task<LocationItem> CreateLocationItemFromPathAsync(string path);

		/// <summary>
		/// Adds the item (from a path) to the navigation sidebar
		/// </summary>
		/// <param name="path">The path which to save</param>
		/// <returns>Task</returns>
		Task AddItemToSidebarAsync(string path);

		/// <summary>
		/// Adds all items to the navigation sidebar
		/// </summary>
		Task AddAllItemsToSidebarAsync();

		/// <summary>
		/// Removes stale items in the navigation sidebar
		/// </summary>
		void RemoveStaleSidebarItems();
	}
}
