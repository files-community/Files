using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	public interface IQuickAccessService
	{
		// Gets the list of quick access items from File Explorer
		Task<List<string>> GetPinnedFoldersAsync();

		// Pins a folder to the quick access list
		Task PinToSidebar(string folderPath, bool loadExplorerItems = true);
		Task PinToSidebar(string[] folderPaths, bool loadExplorerItems = true);

		// Unpins a folder from the quick access list
		Task UnpinFromSidebar(string folderPath, bool loadExplorerItems = true);
		Task UnpinFromSidebar(string[] folderPaths, bool loadExplorerItems = true);
	}
}