using Files.App.DataModels;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Filesystem
{
	public sealed class PinnedItemsManager
	{
		private static readonly Lazy<PinnedItemsManager> lazy = new(() => new PinnedItemsManager());
		public FileSystemWatcher? PinnedItemsWatcher;
		public event FileSystemEventHandler? PinnedItemsModified;

		public static PinnedItemsManager Default => lazy.Value;

		private PinnedItemsManager()
		{
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
	}
}
