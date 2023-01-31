using Files.App.DataModels;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Controllers
{
	public class SidebarPinnedController
	{
		public SidebarPinnedModel Model { get; set; }
		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;
		public SidebarPinnedController()
		{
			Model = new SidebarPinnedModel();
			Model.SetController(this);

			PinnedItemsManager.Default.PinnedItemsModified += LoadAsync;
		}

		public async Task InitializeAsync()
		{
			await LoadAsync();
			if (!Model.FavoriteItems.Contains(CommonPaths.RecycleBinPath))
				await PinnedItemsService.PinToSidebar(CommonPaths.RecycleBinPath);

			var fileItems = (await PinnedItemsManager.ReadV2PinnedItemsFile())?.ToList();

			if (fileItems is null)
				return;

			var itemsToLoad = fileItems.Except(Model.FavoriteItems).ToArray();
			await PinnedItemsService.PinToSidebar(itemsToLoad);
			await LoadAsync();
		}

		public async void LoadAsync(object? sender, FileSystemEventArgs e)
			=> await LoadAsync();
		public async Task LoadAsync()
			=> await Model.UpdateItemsWithExplorer();
	}
}