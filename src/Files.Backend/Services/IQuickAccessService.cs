using Files.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
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
		Task PinToSidebar(string folderPath);

		/// <summary>
		/// Pins folders to the quick access list
		/// </summary>
		/// <param name="folderPaths">The array of folders to pin</param>
		/// <returns></returns>
		Task PinToSidebar(string[] folderPaths);

		/// <summary>
		/// Unpins a folder from the quick access list
		/// </summary>
		/// <param name="folderPath">The folder to unpin</param>
		/// <param name="syncItems">Whether to sync the items with explorer</param>
		/// <returns></returns>
		Task UnpinFromSidebar(string folderPath, bool syncItems = true);

		/// <summary>
		/// Unpins folders from the quick access list
		/// </summary>
		/// <param name="folderPaths">The array of folders to unpin</param>
		/// <param name="syncItems">Whether to sync the items with explorer</param>
		/// <returns></returns>
		Task UnpinFromSidebar(string[] folderPaths, bool syncItems = true);

		/// <summary>
		/// Checks if a folder is pinned to the quick access list
		/// </summary>
		/// <param name="folderPath">The path of the folder</param>
		/// <returns>true if the item is pinned</returns>
		bool IsItemPinned(string folderPath);

		/// <summary>
		/// Moves a folder to a new location in the sidebar and in the quick access widget
		/// </summary>
		/// <param name="toMove">The path of the folder to be moved in the sidebar</param>
		/// <param name="destination">The path of the folder over which to place the moved folder in the sidebar</param>
		/// <returns></returns>
		Task MoveTo(string toMove, string destination);
	}
}