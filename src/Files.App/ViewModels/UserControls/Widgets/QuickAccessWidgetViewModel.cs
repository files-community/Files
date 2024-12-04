// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="QuickAccessWidget"/>.
	/// </summary>
	public sealed class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Fields

		private readonly SemaphoreSlim _refreshSemaphore;
		private CancellationTokenSource _refreshCTS;

		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items { get; } = [];

		public string WidgetName => nameof(QuickAccessWidget);
		public string AutomationProperties => "QuickAccess".GetLocalizedResource();
		public string WidgetHeader => "QuickAccess".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			_refreshSemaphore = new SemaphoreSlim(1, 1);
			_refreshCTS = new CancellationTokenSource();

			_ = RefreshWidgetAsync();

			QuickAccessService.PinnedFoldersChanged += QuickAccessService_CollectionChanged;

			OpenPropertiesCommand = new RelayCommand<WidgetFolderCardItem>(ExecuteOpenPropertiesCommand);
			PinToSidebarCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteUnpinFromSidebarCommand);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return QuickAccessService.UpdatePinnedFoldersAsync();
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane
				}.Build(),
				new()
				{
					Text = "PinFolderToSidebar".GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.FavoritePin" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = "UnpinFolderFromSidebar".GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.FavoritePinRemove" },
					Command = UnpinFromSidebarCommand,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.Properties" },
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = CommandManager.OpenTerminalFromHome.IsExecutable
				},
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenTerminalFromHome).Build(),
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

		private async Task UpdateCollectionAsync(NotifyCollectionChangedEventArgs e)
		{
			try
			{
				await _refreshSemaphore.WaitAsync(_refreshCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// Drop other waiting instances
				_refreshCTS.Cancel();
				_refreshCTS = new CancellationTokenSource();

				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
						{
							if (e.NewItems is not null)
							{
								var item = e.NewItems.Cast<LocationItem>().Single();
								var cardItem = new WidgetFolderCardItem(item, SystemIO.Path.GetFileName(item.Text), item.IsPinned) { Path = item.Path };
								AddItemToCollection(cardItem);
							}
						}
						break;
					case NotifyCollectionChangedAction.Move:
						{
							if (e.OldItems is not null)
							{
								Items.RemoveAt(e.OldStartingIndex);

								var item = e.NewItems.Cast<LocationItem>().Single();
								var cardItem = new WidgetFolderCardItem(item, SystemIO.Path.GetFileName(item.Text), item.IsPinned) { Path = item.Path };
								AddItemToCollection(cardItem);
							}
						}
						break;
					case NotifyCollectionChangedAction.Remove:
						{
							if (e.OldItems is not null)
								Items.RemoveAt(e.OldStartingIndex);
						}
						break;
					// case NotifyCollectionChangedAction.Reset:
					default:
						{
							Items.Clear();
							foreach (var item in QuickAccessService.QuickAccessFolders.ToList())
							{
								if (item is not LocationItem locationItem)
									continue;

								var cardItem = new WidgetFolderCardItem(locationItem, SystemIO.Path.GetFileName(locationItem.Text), locationItem.IsPinned) { Path = locationItem.Path };
								AddItemToCollection(cardItem);
							}
						}
						break;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, "Could not populate pinned folders.");
			}
			finally
			{
				_refreshSemaphore.Release();
			}

			bool AddItemToCollection(WidgetFolderCardItem? item, int index = -1)
			{
				if (item is null || Items.Any(x => x.Equals(item)))
					return false;

				Items.Insert(index < 0 ? Items.Count : Math.Min(index, Items.Count), item);
				_ = item.LoadCardThumbnailAsync()
					.ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);

				return true;
			}
		}

		public async Task NavigateToPath(string path)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path, false);
				return;
			}

			ContentPageContext.ShellPage?.NavigateWithArguments(
				ContentPageContext.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new() { NavPathParam = path });
		}

		// Event methods

		private async void QuickAccessService_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				await UpdateCollectionAsync(e);
			});
		}

		// Command methods

		public override async Task ExecutePinToSidebarCommand(WidgetCardItem? item)
		{
			if (item is null || item.Path is null)
				return;

			await QuickAccessService.PinFolderAsync([item.Path]);
		}

		public override async Task ExecuteUnpinFromSidebarCommand(WidgetCardItem? item)
		{
			if (item is null || item.Path is null)
				return;

			await QuickAccessService.UnpinFolderAsync([item.Path]);
		}

		private void ExecuteOpenPropertiesCommand(WidgetFolderCardItem? item)
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
					BaseStorageFolder matchingStorageFolder = await ContentPageContext.ShellPage!.ShellViewModel.GetFolderFromPathAsync(item.Item.Path);
					if (matchingStorageFolder is not null)
					{
						var syncStatus = await ContentPageContext.ShellPage!.ShellViewModel.CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
						listedItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
					}
				}

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		// Disposer

		public void Dispose()
		{
			QuickAccessService.PinnedFoldersChanged -= QuickAccessService_CollectionChanged;
		}
	}
}
