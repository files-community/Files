// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IQuickAccessService
	{
		/// <summary>
		/// Gets the list of quick access items
		/// </summary>
		/// <returns></returns>
		Task<IEnumerable<ShellFileItem>> GetPinnedFoldersAsync();

		/// <summary>
		/// Pins a folder to the quick access list
		/// </summary>
		/// <param name="folderPath">The folder to pin</param>
		/// <returns></returns>
		Task PinToSidebarAsync(string folderPath);

		/// <summary>
		/// Pins folders to the quick access list
		/// </summary>
		/// <param name="folderPaths">The array of folders to pin</param>
		/// <returns></returns>
		Task PinToSidebarAsync(string[] folderPaths);

		/// <summary>
		/// Unpins a folder from the quick access list
		/// </summary>
		/// <param name="folderPath">The folder to unpin</param>
		/// <returns></returns>
		Task UnpinFromSidebarAsync(string folderPath);

		/// <summary>
		/// Unpins folders from the quick access list
		/// </summary>
		/// <param name="folderPaths">The array of folders to unpin</param>
		/// <returns></returns>
		Task UnpinFromSidebarAsync(string[] folderPaths);

		/// <summary>
		/// Checks if a folder is pinned to the quick access list
		/// </summary>
		/// <param name="folderPath">The path of the folder</param>
		/// <returns>true if the item is pinned</returns>
		bool IsItemPinned(string folderPath);

		/// <summary>
		/// Saves a state of pinned folder items in the sidebar
		/// </summary>
		/// <param name="items">The array of items to save</param>
		/// <returns></returns>
		Task SaveAsync(string[] items);

		/// <summary>
		/// Notifies listeners that the collection of pinned items has changed.
		/// </summary>
		/// <remarks>Call this method when the set of pinned items is modified to ensure that any UI components or
		/// services reflecting pinned items remain in sync. If doUpdateQuickAccessWidget is set to true, the quick access
		/// widget will be refreshed to display the latest pinned items.</remarks>
		/// <param name="doUpdateQuickAccessWidget">true to update the quick access widget after notifying the change; otherwise, false.</param>
		/// <returns>A task that represents the asynchronous notification operation.</returns>
		Task NotifyPinnedItemsChanged(bool doUpdateQuickAccessWidget);
	}
}
