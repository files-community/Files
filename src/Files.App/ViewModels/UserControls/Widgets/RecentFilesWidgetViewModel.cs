// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetRecentItem> Items { get; } = [];

		public string WidgetName => nameof(RecentFilesWidgetViewModel);
		public string AutomationProperties => "RecentFiles".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		private bool _IsEmptyRecentItemsTextVisible;
		public bool IsEmptyRecentItemsTextVisible
		{
			get => _IsEmptyRecentItemsTextVisible;
			set => SetProperty(ref _IsEmptyRecentItemsTextVisible, value);
		}

		private bool _IsRecentFilesDisabledInWindows;
		public bool IsRecentFilesDisabledInWindows
		{
			get => _IsRecentFilesDisabledInWindows;
			set => SetProperty(ref _IsRecentFilesDisabledInWindows, value);
		}

		// Constructor

		public RecentFilesWidgetViewModel()
		{
			_ = RefreshWidgetAsync();

			App.RecentItemsManager.RecentFilesChanged += async (s, e) => await RefreshWidgetAsync();
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				IsRecentFilesDisabledInWindows = !App.RecentItemsManager.CheckIsRecentFilesEnabled();

				try
				{
					if (Items.Count != 0)
						Items.Clear();

					IsEmptyRecentItemsTextVisible = false;

					// Already sorted, add all in order
					var recentFiles = App.RecentItemsManager.RecentFiles;
					if (!recentFiles.SequenceEqual(Items))
					{
						Items.Clear();
						foreach (var item in recentFiles)
						{
							Items.Insert(0, item);

							_ = item.LoadRecentItemIconAsync()
								.ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);
						}
					}

					if (Items.Count == 0 && !IsRecentFilesDisabledInWindows)
						IsEmptyRecentItemsTextVisible = true;
				}
				catch (Exception ex)
				{
					App.Logger.LogInformation(ex, "The app could not populate recent files");
				}
			});
		}

		public async Task OpenFileLocation(WidgetRecentItem? item)
		{
			if (item is null)
				return;

			try
			{
				if (item.IsFile)
				{
					var directoryName = SystemIO.Path.GetDirectoryName(item.RecentPath);

					await Win32Helpers.InvokeWin32ComponentAsync(
						item.RecentPath,
						ContentPageContext.ShellPage!,
						workingDirectory: directoryName ?? string.Empty);
				}
				else
				{
					ContentPageContext.ShellPage!.NavigateWithArguments(
						ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.GetLayoutType(item.RecentPath)!,
						new() { NavPathParam = item.RecentPath });
				}
			}
			catch (Exception) { }
		}

		// Disposer

		public void Dispose()
		{
			App.RecentItemsManager.RecentFilesChanged -= async (s, e) => await RefreshWidgetAsync();
		}
	}
}
