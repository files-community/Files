// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;

namespace Files.App.Utils
{
	public sealed class QuickAccessManager
	{
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public FileSystemWatcher? PinnedItemsWatcher;

		public event FileSystemEventHandler? PinnedItemsModified;
		
		public EventHandler<ModifyQuickAccessEventArgs>? UpdateQuickAccessWidget;

		public PinnedFoldersManager Model = new();

		public QuickAccessManager()
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

		public async Task InitializeAsync()
		{
			PinnedItemsModified += Model.LoadAsync;

			//if (!Model.PinnedFolders.Contains(Constants.UserEnvironmentPaths.RecycleBinPath) && SystemInformation.Instance.IsFirstRun)
			//	await QuickAccessService.PinToSidebar(Constants.UserEnvironmentPaths.RecycleBinPath);

			await Model.LoadAsync();
		}
	}
}
