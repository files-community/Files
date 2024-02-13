// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items = [];

		public string WidgetName => nameof(QuickAccessWidgetViewModel);
		public string AutomationProperties => "QuickAccess".GetLocalizedResource();
		public string WidgetHeader => "QuickAccess".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		// Commands

		public ICommand OpenInNewPaneCommand;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			_ = InitializeWidget();

			Items.CollectionChanged += ItemsAdded_CollectionChanged;

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetFolderCardItem>(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetFolderCardItem>(OpenInNewWindowAsync);
			OpenInNewPaneCommand = new RelayCommand<WidgetFolderCardItem>(OpenInNewPane);
			OpenPropertiesCommand = new RelayCommand<WidgetFolderCardItem>(OpenProperties);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetFolderCardItem>(PinToFavoritesAsync);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetFolderCardItem>(UnpinFromFavoritesAsync);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
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

		private async Task InitializeWidget()
		{
			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync();

			ModifyItemAsync(this, new(itemsToAdd.ToArray(), false) { Reset = true });

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItemAsync;
		}

		private async void ModifyItemAsync(object? sender, ModifyQuickAccessEventArgs? e)
		{
			if (e is null || e.Paths is null || e.Items is null)
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				if (e.Reset)
				{
					// Find the intersection between the two lists and determine whether to remove or add
					var originalItemsAdded = Items.ToList();
					var itemsToRemove = originalItemsAdded.Where(x => !e.Paths.Contains(x.Path));
					var itemsToAdd = e.Paths.Where(x => !originalItemsAdded.Any(y => y.Path == x));

					// Remove items
					foreach (var itemToRemove in itemsToRemove)
						Items.Remove(itemToRemove);

					// Add items
					foreach (var itemToAdd in itemsToAdd)
					{
						var interimItemsAdded = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);

						if (interimItemsAdded.FirstOrDefault(x => !x.IsPinned) is not WidgetFolderCardItem cardItem)
							continue;

						var lastIndex = Items.IndexOf(cardItem);
						var isPinned = (bool?)e.Items.Where(x => x.FilePath == itemToAdd).FirstOrDefault()?.Properties["System.Home.IsPinned"] ?? false;
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						Items.Insert(isPinned && lastIndex >= 0 ? Math.Min(lastIndex, Items.Count) : Items.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), isPinned)
						{
							Path = item.Path,
						});
					}

					return;
				}
				if (e.Reorder)
				{
					// Remove pinned items
					foreach (var itemToRemove in Items.ToList().Where(x => x.IsPinned))
						Items.Remove(itemToRemove);

					// Add pinned items in the new order
					foreach (var itemToAdd in e.Paths)
					{
						var interimItemsAdded = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);

						if (interimItemsAdded.FirstOrDefault(x => !x.IsPinned) is not WidgetFolderCardItem cardItem)
							continue;

						var lastIndex = Items.IndexOf(cardItem);
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						Items.Insert(lastIndex >= 0 ? Math.Min(lastIndex, Items.Count) : Items.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), true)
						{
							Path = item.Path,
						});
					}

					return;
				}
				if (e.Add)
				{
					foreach (var itemToAdd in e.Paths)
					{
						var interimItemsAdded = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);

						if (interimItemsAdded.FirstOrDefault(x => !x.IsPinned) is not WidgetFolderCardItem cardItem)
							continue;

						var lastIndex = Items.IndexOf(cardItem);
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;
						Items.Insert(e.Pin && lastIndex >= 0 ? Math.Min(lastIndex, Items.Count) : Items.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), e.Pin) // Add just after the Recent Folders
						{
							Path = item.Path,
						});
					}
				}
				else
					foreach (var itemToRemove in Items.ToList().Where(x => e.Paths.Contains(x.Path)))
						Items.Remove(itemToRemove);
			});
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow
				},
				new()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane
				},
				new()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinToFavoritesCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinFromFavoritesCommand,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		// Event methods

		private static async void ItemsAdded_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetFolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		// Command methods

		private void OpenInNewPane(WidgetFolderCardItem? item)
		{
			if (item is null || string.IsNullOrEmpty(item.Path))
				return;

			ContentPageContext.ShellPage!.PaneHolder?.OpenPathInNewPane(item.Path);
		}

		private void OpenProperties(WidgetFolderCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked || item is null || item.Item is null)
				return;

			var flyout = HomePageContext.ItemContextFlyoutMenu;
			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = async (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;

				ListedItem listedItem = new(null!)
				{
					ItemPath = item.Item.Path,
					ItemNameRaw = item.Item.Text,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};

				if (!string.Equals(item.Item.Path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				{
					BaseStorageFolder matchingStorageFolder = await ContentPageContext.ShellPage!.FilesystemViewModel.GetFolderFromPathAsync(item.Item.Path);

					if (matchingStorageFolder is not null)
					{
						var syncStatus = await ContentPageContext.ShellPage!.FilesystemViewModel.CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
						listedItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
					}
				}

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		public override async Task PinToFavoritesAsync(WidgetCardItem? item)
		{
			if (item is null)
				return;

			await QuickAccessService.PinToSidebarAsync(item.Path ?? string.Empty);

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { item.Path ?? string.Empty }, false));

			var items = (await QuickAccessService.GetPinnedFoldersAsync())
				.Where(link => !((bool?)link.Properties["System.Home.IsPinned"] ?? false));

			var recentItem = items.Where(x => !Items.ToList().Select(y => y.Path).Contains(x.FilePath)).FirstOrDefault();
			if (recentItem is not null)
			{
				ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { recentItem.FilePath }, true)
				{
					Pin = false
				});
			}
		}

		public override async Task UnpinFromFavoritesAsync(WidgetCardItem? item)
		{
			if (item is null)
				return;

			await QuickAccessService.UnpinFromSidebarAsync(item.Path ?? string.Empty);

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { item.Path ?? string.Empty }, false));
		}

		public void Dispose()
		{
			App.QuickAccessManager.UpdateQuickAccessWidget -= ModifyItemAsync;
		}
	}
}
