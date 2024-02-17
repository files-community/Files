// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
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
			_ = RefreshWidgetAsync();

			App.QuickAccessManager.UpdateQuickAccessWidget += async (s, e) => await RefreshWidgetAsync();
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				var quickAccessPinnedItems = await QuickAccessService.GetPinnedFoldersAsync();

				if (Items.Count != 0)
					Items.Clear();

				foreach (var item in quickAccessPinnedItems)
				{
					var locationItem = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(item.FilePath);
					var isPinned = (bool?)item.Properties["System.Home.IsPinned"] ?? false;

					Items.Add(new WidgetFolderCardItem(locationItem, SystemIO.Path.GetFileName(locationItem.Text), isPinned) { Path = item.FilePath });
				}

				foreach (WidgetFolderCardItem cardItem in Items)
					await cardItem.LoadCardThumbnailAsync();
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
