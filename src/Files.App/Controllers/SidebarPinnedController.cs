using Files.App.DataModels;
using Files.App.Filesystem;
using System;
using System.Collections.Specialized;
using System.IO;
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

		public async void LoadAsync(object? sender, FileSystemEventArgs e)
			=> await LoadAsync();
		public async Task LoadAsync()
			=> await Model.UpdateItemsWithExplorer();
	}
}