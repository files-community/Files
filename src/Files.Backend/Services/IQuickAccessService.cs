using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	public interface IQuickAccessService
	{
		// Gets the list of quick access items from File Explorer
		Task<List<string>> GetPinnedFoldersAsync();

		// Pins a folder to the quick access list
		Task PinToSidebar(string folderPath);
		Task PinToSidebar(string[] folderPaths);

		// Unpins a folder from the quick access list
		Task UnpinFromSidebar(string folderPath);
		Task UnpinFromSidebar(string[] folderPaths);
	}
}