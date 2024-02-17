// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Fields

		private readonly SemaphoreSlim _refreshItemsSemaphore;
		private CancellationTokenSource _refreshItemsCTS;

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
			set
			{
				if (_IsEmptyRecentItemsTextVisible != value)
				{
					_IsEmptyRecentItemsTextVisible = value;
					OnPropertyChanged(nameof(IsEmptyRecentItemsTextVisible));
				}
			}
		}

		private bool _IsRecentFilesDisabledInWindows;
		public bool IsRecentFilesDisabledInWindows
		{
			get => _IsRecentFilesDisabledInWindows;
			internal set
			{
				if (_IsRecentFilesDisabledInWindows != value)
				{
					_IsRecentFilesDisabledInWindows = value;
					OnPropertyChanged(nameof(IsRecentFilesDisabledInWindows));
				}
			}
		}

		// Constructor

		public RecentFilesWidgetViewModel()
		{
			_refreshItemsSemaphore = new(1, 1);
			_refreshItemsCTS = new();

			_ = RefreshWidgetAsync();

			App.RecentItemsManager.RecentFilesChanged += async (s, e) => await RefreshWidgetAsync();
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				try
				{
					await _refreshItemsSemaphore.WaitAsync(_refreshItemsCTS.Token);
				}
				catch (OperationCanceledException)
				{
					return;
				}

				IsRecentFilesDisabledInWindows = !App.RecentItemsManager.CheckIsRecentFilesEnabled();

				try
				{
					// Drop other waiting instances
					_refreshItemsCTS.Cancel();
					_refreshItemsCTS.TryReset();

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
					App.Logger.LogInformation(ex, "Could not populate recent files");
				}
				finally
				{
					_refreshItemsSemaphore.Release();
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
			catch (UnauthorizedAccessException)
			{
				var dialog = DynamicDialogFactory.GetFor_ConsentDialog();

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await dialog.TryShowAsync();
			}
			catch (COMException) { }
			catch (ArgumentException) { }
		}

		// Disposer

		public void Dispose()
		{
			App.RecentItemsManager.RecentFilesChanged -= async (s, e) => await RefreshWidgetAsync();
		}
	}
}
