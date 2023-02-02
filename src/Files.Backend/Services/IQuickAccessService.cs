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
		Task<List<string>> GetPinnedFoldersAsync();

		/// <summary>
		/// Pins a folder to the quick access list
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="loadExplorerItems"></param>
		/// <returns></returns>
		Task PinToSidebar(string folderPath, bool loadExplorerItems = true);

		/// <summary>
		/// Pins folders to the quick access list
		/// </summary>
		/// <param name="folderPaths"></param>
		/// <param name="loadExplorerItems"></param>
		/// <returns></returns>
		Task PinToSidebar(string[] folderPaths, bool loadExplorerItems = true);

		/// <summary>
		/// Unpins a folder from the quick access list
		/// </summary>
		/// <param name="folderPath"></param>
		/// <param name="loadExplorerItems"></param>
		/// <returns></returns>
		Task UnpinFromSidebar(string folderPath, bool loadExplorerItems = true);

		/// <summary>
		/// Unpins folders from the quick access list
		/// </summary>
		/// <param name="folderPaths"></param>
		/// <param name="loadExplorerItems"></param>
		/// <returns></returns>
		Task UnpinFromSidebar(string[] folderPaths, bool loadExplorerItems = true);
	}
}