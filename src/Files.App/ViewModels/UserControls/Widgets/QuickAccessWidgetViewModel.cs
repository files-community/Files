// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Fields

		private readonly SemaphoreSlim _refreshItemsSemaphore;
		private CancellationTokenSource _refreshItemsCTS;

		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items { get; } = [];

		public string WidgetName => nameof(QuickAccessWidgetViewModel);
		public string AutomationProperties => "QuickAccess".GetLocalizedResource();
		public string WidgetHeader => "QuickAccess".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			_refreshItemsSemaphore = new(1, 1);
			_refreshItemsCTS = new();

			_ = RefreshWidgetAsync();

			App.QuickAccessManager.UpdateQuickAccessWidget += async (s, e) => await RefreshWidgetAsync();
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

				try
				{
					// Drop other waiting instances
					_refreshItemsCTS.Cancel();
					_refreshItemsCTS.TryReset();

					var quickAccessPinnedItems = await QuickAccessService.GetPinnedFoldersAsync();

					foreach (var item in quickAccessPinnedItems)
					{
						var locationItem = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(item.FilePath);
						var isPinned = (bool?)item.Properties["System.Home.IsPinned"] ?? false;

						Items.Add(new WidgetFolderCardItem(locationItem, SystemIO.Path.GetFileName(locationItem.Text), isPinned) { Path = item.FilePath });
					}

					foreach (WidgetFolderCardItem cardItem in Items)
						await cardItem.LoadCardThumbnailAsync();
				}
				catch (Exception ex)
				{
					App.Logger.LogInformation(ex, "Could not populate file tags containers.");
				}
				finally
				{
					_refreshItemsSemaphore.Release();
				}
			});
		}

		public async Task OpenFileLocation(string path)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.GetLayoutType(path)!,
				new() { NavPathParam = path });
		}

		// Disposer

		public void Dispose()
		{
			App.QuickAccessManager.UpdateQuickAccessWidget -= async (s, e) => await RefreshWidgetAsync();
		}
	}
}
