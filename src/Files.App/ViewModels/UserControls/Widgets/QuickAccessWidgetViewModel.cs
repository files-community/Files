﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items { get; } = [];

		public string WidgetName => nameof(QuickAccessWidget);
		public string AutomationProperties => "QuickAccess".GetLocalizedResource();
		public string WidgetHeader => "QuickAccess".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Commands

		public ICommand OpenInNewPaneCommand { get; set; } = null!;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			_ = InitializeWidget();

			Items.CollectionChanged += Items_CollectionChanged;

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteOpenInNewWindowCommand);
			OpenInNewPaneCommand = new RelayCommand<WidgetFolderCardItem>(ExecuteOpenInNewPaneCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetFolderCardItem>(ExecuteOpenPropertiesCommand);
			PinToSidebarCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteUnpinFromSidebarCommand);
		}

		// Methods

		private async Task InitializeWidget()
		{
			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync();
			ModifyItemAsync(this, new(itemsToAdd.ToArray(), false) { Reset = true });

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItemAsync;
		}

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHomeAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHomeAction).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHomeAction).Build(),
				new()
				{
					Text = "PinFolderToSidebar".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "Icons.Pin.16x16" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = "UnpinFolderFromSidebar".GetLocalizedResource(),
					OpacityIcon = new() { OpacityIconStyle = "Icons.Unpin.16x16" },
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
					OpacityIcon = new() { OpacityIconStyle = "ColorIconProperties" },
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

		private async void ModifyItemAsync(object? sender, ModifyQuickAccessEventArgs? e)
		{
			if (e is null)
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				if (e.Reset)
				{
					// Find the intersection between the two lists and determine whether to remove or add
					var originalItems = Items.ToList();
					var itemsToRemove = originalItems.Where(x => !e.Paths.Contains(x.Path));
					var itemsToAdd = e.Paths.Where(x => !originalItems.Any(y => y.Path == x));

					// Remove items
					foreach (var itemToRemove in itemsToRemove)
						Items.Remove(itemToRemove);

					// Add items
					foreach (var itemToAdd in itemsToAdd)
					{
						var interimItems = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = Items.IndexOf(interimItems.FirstOrDefault(x => !x.IsPinned));
						var isPinned = (bool?)e.Items.Where(x => x.FilePath == itemToAdd).FirstOrDefault()?.Properties["System.Home.IsPinned"] ?? false;
						if (interimItems.Any(x => x.Path == itemToAdd))
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
						var interimItems = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = Items.IndexOf(interimItems.FirstOrDefault(x => !x.IsPinned));
						if (interimItems.Any(x => x.Path == itemToAdd))
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
						var interimItems = Items.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = Items.IndexOf(interimItems.FirstOrDefault(x => !x.IsPinned));
						if (interimItems.Any(x => x.Path == itemToAdd))
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

		private async void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetFolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		// Command methods

		public override async Task ExecutePinToSidebarCommand(WidgetCardItem? item)
		{
			if (item is null || item.Path is null)
				return;

			await QuickAccessService.PinToSidebarAsync(item.Path);

			ModifyItemAsync(this, new(new[] { item.Path }, false));

			var items = (await QuickAccessService.GetPinnedFoldersAsync())
				.Where(link => !((bool?)link.Properties["System.Home.IsPinned"] ?? false));

			var recentItem = items.Where(x => !Items.ToList().Select(y => y.Path).Contains(x.FilePath)).FirstOrDefault();
			if (recentItem is not null)
			{
				ModifyItemAsync(this, new(new[] { recentItem.FilePath }, true) { Pin = false });
			}
		}

		public override async Task ExecuteUnpinFromSidebarCommand(WidgetCardItem? item)
		{
			if (item is null || item.Path is null)
				return;

			await QuickAccessService.UnpinFromSidebarAsync(item.Path);

			ModifyItemAsync(this, new(new[] { item.Path }, false));
		}

		private void ExecuteOpenInNewPaneCommand(WidgetFolderCardItem? item)
		{
			if (item is null || item.Path is null)
				return;

			ContentPageContext.ShellPage!.PaneHolder?.OpenSecondaryPane(item.Path);
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

		// Disposer

		public void Dispose()
		{
			App.QuickAccessManager.UpdateQuickAccessWidget -= ModifyItemAsync;
		}
	}
}
