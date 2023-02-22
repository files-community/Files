using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using Files.App.DataModels;
using Files.App.ServicesImplementation;
using Files.App.UserControls.Widgets;
using Files.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using static Files.Core.Constants;

namespace Files.App.Filesystem
{
	public sealed class QuickAccessManager
	{
		public FileSystemWatcher? PinnedItemsWatcher;

		public event FileSystemEventHandler? PinnedItemsModified;
		
		public EventHandler<ModifyQuickAccessEventArgs>? UpdateQuickAccessWidget;

		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public SidebarPinnedModel Model;
		public QuickAccessManager()
		{
			Model = new();
			Initialize();
		}	
		
		public void Initialize()
		{
			PinnedItemsWatcher = new()
			{
				Path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations"),
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms",
				NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName,
				EnableRaisingEvents = true
			};

			PinnedItemsWatcher.Changed += PinnedItemsWatcher_Changed;
		}

		private void PinnedItemsWatcher_Changed(object sender, FileSystemEventArgs e)
			=> PinnedItemsModified?.Invoke(this, e);
		
		public static async Task<IEnumerable<string>?> ReadV2PinnedItemsFile()
		{
			return await SafetyExtensions.IgnoreExceptions(async () =>
			{
				var oldPinnedItemsFile = await ApplicationData.Current.LocalCacheFolder.GetFileAsync("PinnedItems.json");
				var model = JsonSerializer.Deserialize<SidebarPinnedModel>(await FileIO.ReadTextAsync(oldPinnedItemsFile));
				await oldPinnedItemsFile.DeleteAsync();
				return model?.FavoriteItems;
			});
		}

		public async Task InitializeAsync()
		{
			PinnedItemsModified += Model.LoadAsync;

			await Model.LoadAsync();
			if (!Model.FavoriteItems.Contains(CommonPaths.RecycleBinPath) && SystemInformation.Instance.IsFirstRun)
				await QuickAccessService.PinToSidebar(CommonPaths.RecycleBinPath);
			
			var fileItems = (await ReadV2PinnedItemsFile())?.ToList();

			if (fileItems is null)
				return;

			var itemsToLoad = fileItems.Except(Model.FavoriteItems).ToArray();
			await QuickAccessService.PinToSidebar(itemsToLoad);
			await Model.LoadAsync();
		}
	}
}
