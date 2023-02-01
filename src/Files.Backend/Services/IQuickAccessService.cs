using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.App.ServicesImplementation
{
	public interface IQuickAccessService
	{
		static string guid;

		Task<List<string>> GetPinnedFoldersAsync();

		Task PinToSidebar(string folderPath, bool loadExplorerItems = true);
		Task PinToSidebar(string[] folderPaths, bool loadExplorerItems = true);

		Task UnpinFromSidebar(string folderPath, bool loadExplorerItems = true);
		Task UnpinFromSidebar(string[] folderPaths, bool loadExplorerItems = true);
	}
}