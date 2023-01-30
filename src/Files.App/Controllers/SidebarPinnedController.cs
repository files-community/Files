using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.DataModels;
using Files.App.Filesystem;
using Files.App.Serialization.Implementation;
using Files.App.ServicesImplementation;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;

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