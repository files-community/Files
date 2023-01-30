using Files.App.DataModels;
using System;
using System.Collections.Specialized;
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
		}
		
		public async Task LoadAsync()
			=> await Model.UpdateItemsWithExplorer();
	}
}