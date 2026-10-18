// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Utils
{
	public sealed class QuickAccessManager
	{


		public EventHandler<ModifyQuickAccessEventArgs>? UpdateQuickAccessWidget;

		public IQuickAccessService QuickAccessService;

		public PinnedFoldersManager Model;
		public QuickAccessManager()
		{
			QuickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();
			Model = new();
			Initialize();
		}

		public void Initialize() => App.ServerChanged += OnServerChanged;

		private async void OnServerChanged(object? sender, string name)
		{
			if (name == "PinnedFolders")
				await SafetyExtensions.IgnoreExceptions(Model.LoadAsync, App.Logger);
		}

		public async Task InitializeAsync()
		{
			await Model.LoadAsync();

			if (!Model.PinnedFolders.Contains(Constants.UserEnvironmentPaths.RecycleBinPath) && AppLifecycleHelper.IsFirstRun)
				await QuickAccessService.PinToSidebarAsync(Constants.UserEnvironmentPaths.RecycleBinPath);
		}
	}
}
