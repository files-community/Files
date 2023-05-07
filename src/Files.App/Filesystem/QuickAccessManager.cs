// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Helpers;
using Files.App.Data.Models;
using Files.App.Helpers;
using Files.App.ServicesImplementation;
using Files.App.UserControls.Widgets;
using System;
using System.IO;
using System.Threading.Tasks;

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

		public async Task InitializeAsync()
		{
			PinnedItemsModified += Model.LoadAsync;

			if (!Model.FavoriteItems.Contains(Constants.UserEnvironmentPaths.RecycleBinPath) && SystemInformation.Instance.IsFirstRun)
				await QuickAccessService.PinToSidebar(Constants.UserEnvironmentPaths.RecycleBinPath);

			await Model.LoadAsync();
		}
	}
}
