﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents ViewModel for <see cref="QuickAccessWidget"/>.
	/// </summary>
	public class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items { get; } = new();

		public string WidgetName => nameof(QuickAccessWidgetViewModel);
		public string AutomationProperties => "QuickAccess".GetLocalizedResource();
		public string WidgetHeader => "QuickAccess".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Events

		public delegate void QuickAccessCardInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);
		public delegate void QuickAccessCardNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);
		public delegate void QuickAccessCardPropertiesInvokedEventHandler(object sender, QuickAccessCardEventArgs e);
		public event QuickAccessCardInvokedEventHandler? CardInvoked;
		public event QuickAccessCardNewPaneInvokedEventHandler? CardNewPaneInvoked;
		public event QuickAccessCardPropertiesInvokedEventHandler? CardPropertiesInvoked;
		public event EventHandler? QuickAccessWidgetShowMultiPaneControlsInvoked;

		// Commands

		public ICommand OpenInNewPaneCommand;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			Items.CollectionChanged += ItemsAdded_CollectionChanged;

			_ = LoadPinnedFoldersAsync();

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItemAsync;

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteOpenInNewWindowCommand);
			OpenInNewPaneCommand = new RelayCommand<WidgetFolderCardItem>(ExecuteOpenInNewPaneCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetFolderCardItem>(ExecuteOpenPropertiesCommand);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecutePinToFavoritesCommand);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteUnpinFromFavoritesCommand);
		}

		// Methods

		private async Task LoadPinnedFoldersAsync()
		{
			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync();

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(itemsToAdd.ToArray(), false) { Reset = true });
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
					var itemsToRemove = Items.Where(x => !e.Paths.Contains(x.Path)).ToList();
					var itemsToAdd = e.Paths.Where(x => !Items.Any(y => y.Path == x)).ToList();

					// Remove items
					foreach (var itemToRemove in itemsToRemove)
						Items.Remove(itemToRemove);

					// Add items
					foreach (var itemToAdd in itemsToAdd)
					{
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = Items.IndexOf(Items.FirstOrDefault(x => !x.IsPinned)!);
						var isPinned = (bool?)e.Items.Where(x => x.FilePath == itemToAdd).FirstOrDefault()?.Properties["System.Home.IsPinned"] ?? false;
						if (Items.Any(x => x.Path == itemToAdd))
							continue;

						Items.Insert(isPinned && lastIndex >= 0 ? lastIndex : Items.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), isPinned)
						{
							Path = item.Path,
						});
					}

					return;
				}
				if (e.Reorder)
				{
					// Remove pinned items
					foreach (var itemToRemove in Items.Where(x => x.IsPinned).ToList())
						Items.Remove(itemToRemove);

					// Add pinned items in the new order
					foreach (var itemToAdd in e.Paths)
					{
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = Items.IndexOf(Items.FirstOrDefault(x => !x.IsPinned)!);
						if (Items.Any(x => x.Path == itemToAdd))
							continue;

						Items.Insert(lastIndex >= 0 ? lastIndex : Items.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), true)
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
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = Items.IndexOf(Items.FirstOrDefault(x => !x.IsPinned)!);
						if (Items.Any(x => x.Path == itemToAdd))
							continue;
						Items.Insert(e.Pin && lastIndex >= 0 ? lastIndex : Items.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), e.Pin) // Add just after the Recent Folders
						{
							Path = item.Path,
						});
					}
				}
				else
					foreach (var itemToRemove in Items.Where(x => e.Paths.Contains(x.Path)).ToList())
						Items.Remove(itemToRemove);
			});
		}

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
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
					Command = OpenInNewTabCommand!,
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
					Command = OpenInNewWindowCommand!,
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
					Command = PinToFavoritesCommand!,
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
					Command = UnpinFromFavoritesCommand!,
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
					Command = OpenPropertiesCommand!,
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
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		// Event methods

		private void MenuFlyout_Opening(object sender)
		{
			var pinToFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "PinToFavorites");
			if (pinToFavoritesItem is not null)
				pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as WidgetFolderCardItem)?.IsPinned ?? false ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "UnpinFromFavorites");
			if (unpinFromFavoritesItem is not null)
				unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as WidgetFolderCardItem)?.IsPinned ?? false ? Visibility.Visible : Visibility.Collapsed;
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			string NavigationPath = (sender as Button)?.Tag.ToString()!;

			if (string.IsNullOrEmpty(NavigationPath))
				return;

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(NavigationPath);
				return;
			}

			CardInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs { Path = NavigationPath });
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
			{
				string navigationPath = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
			}
		}

		private async void ItemsAdded_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetFolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		// Command methods

		private void ExecuteOpenInNewPaneCommand(WidgetFolderCardItem? item)
		{
			CardNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs { Path = item!.Path ?? string.Empty });
		}

		private void ExecuteOpenPropertiesCommand(WidgetFolderCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked)
				return;

			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = (s, e) =>
			{
				HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;
				CardPropertiesInvoked?.Invoke(this, new QuickAccessCardEventArgs { Item = item!.Item });
			};

			HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;
		}

		protected override async Task ExecutePinToFavoritesCommand(WidgetCardItem? item)
		{
			await QuickAccessService.PinToSidebarAsync(item!.Path ?? string.Empty);

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { item.Path ?? string.Empty }, false));

			var items = (await QuickAccessService.GetPinnedFoldersAsync())
				.Where(link => !((bool?)link.Properties["System.Home.IsPinned"] ?? false));

			var recentItem = items.Where(x => !Items.Select(y => y.Path).Contains(x.FilePath)).FirstOrDefault();
			if (recentItem is not null)
			{
				ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { recentItem.FilePath }, true) { Pin = false });
			}
		}

		protected override async Task ExecuteUnpinFromFavoritesCommand(WidgetCardItem? item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item!.Path ?? string.Empty);

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { item.Path ?? string.Empty }, false));
		}

		// Disposer

		public void Dispose()
		{
			App.QuickAccessManager.UpdateQuickAccessWidget -= ModifyItemAsync;
		}
	}
}
