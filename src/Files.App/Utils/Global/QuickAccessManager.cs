// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Utils
{
	public sealed class QuickAccessManager
	{
		public FileSystemWatcher? PinnedItemsWatcher;

		public event FileSystemEventHandler? PinnedItemsModified;

		public EventHandler<ModifyQuickAccessEventArgs>? UpdateQuickAccessWidget;

		public IQuickAccessService QuickAccessService;

		public PinnedFoldersManager Model;
		public QuickAccessManager()
		{
			QuickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();
			Model = new();
			Initialize();
		}

		public void Initialize()
		{
			var automaticDestinationsPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations");
			
			// Only initialize FileSystemWatcher if the directory exists
			// This handles cases where AppData is redirected to network locations that don't contain Windows system directories
			if (Directory.Exists(automaticDestinationsPath))
			{
				PinnedItemsWatcher = new()
				{
					Path = automaticDestinationsPath,
					Filter = "f01b4d95cf55d32a.automaticDestinations-ms",
					NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName
				};

				PinnedItemsWatcher.Changed += PinnedItemsWatcher_Changed;
			}
			else
			{
				// If the directory doesn't exist (e.g., redirected AppData), skip FileSystemWatcher initialization
				// The app will still function, but won't receive automatic updates when pinned items change externally
				PinnedItemsWatcher = null;
			}
		}

		private void PinnedItemsWatcher_Changed(object sender, FileSystemEventArgs e)
			=> PinnedItemsModified?.Invoke(this, e);

		public async Task InitializeAsync()
		{
			PinnedItemsModified += Model.LoadAsync;
			await Model.LoadAsync();

			if (!Model.PinnedFolders.Contains(Constants.UserEnvironmentPaths.RecycleBinPath) && AppLifecycleHelper.IsFirstRun)
				await QuickAccessService.PinToSidebarAsync(Constants.UserEnvironmentPaths.RecycleBinPath);
		}
	}
}
