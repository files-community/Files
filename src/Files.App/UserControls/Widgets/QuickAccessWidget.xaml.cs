// Copyright (c) 2023 Files Community
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

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents a Widget for quick accessing to storage items on Windows.
	/// </summary>
	public sealed partial class QuickAccessWidget : HomePageWidget, IWidgetItemModel, INotifyPropertyChanged
	{
		public IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static ObservableCollection<FolderCardItem> ItemsAdded = new();

		static QuickAccessWidget()
		{
			ItemsAdded.CollectionChanged += ItemsAdded_CollectionChanged;
		}

		public QuickAccessWidget()
		{
			InitializeComponent();

			Loaded += QuickAccessWidget_Loaded;
			Unloaded += QuickAccessWidget_Unloaded;

			OpenInNewTabCommand = new AsyncRelayCommand<FolderCardItem>(OpenInNewTab);
			OpenInNewWindowCommand = new AsyncRelayCommand<FolderCardItem>(OpenInNewWindow);
			OpenInNewPaneCommand = new RelayCommand<FolderCardItem>(OpenInNewPane);
			OpenPropertiesCommand = new RelayCommand<FolderCardItem>(OpenProperties);
			PinToFavoritesCommand = new AsyncRelayCommand<FolderCardItem>(PinToFavorites);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<FolderCardItem>(UnpinFromFavorites);
		}

		public delegate void QuickAccessCardInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);

		public delegate void QuickAccessCardNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);

		public delegate void QuickAccessCardPropertiesInvokedEventHandler(object sender, QuickAccessCardEventArgs e);

		public event QuickAccessCardInvokedEventHandler CardInvoked;

		public event QuickAccessCardNewPaneInvokedEventHandler CardNewPaneInvoked;

		public event QuickAccessCardPropertiesInvokedEventHandler CardPropertiesInvoked;

		public event EventHandler QuickAccessWidgetShowMultiPaneControlsInvoked;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;

		public bool ShowMenuFlyout => false;

		public MenuFlyoutItem? MenuFlyoutItem => null;

		public ICommand OpenPropertiesCommand;
		public ICommand OpenInNewPaneCommand;

		public string WidgetName => nameof(QuickAccessWidget);

		public string AutomationProperties => "QuickAccess".GetLocalizedResource();

		public string WidgetHeader => "QuickAccess".GetLocalizedResource();

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{				
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					CommandParameter = item,
					ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					CommandParameter = item,
					ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewWindow
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = userSettingsService.GeneralSettingsService.ShowOpenInNewPane
				},
				new ContextMenuFlyoutItemViewModel()
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
				new ContextMenuFlyoutItemViewModel()
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
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new ContextMenuFlyoutItemViewModel()
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

		private async void ModifyItem(object? sender, ModifyQuickAccessEventArgs? e)
		{
			if (e is null)
				return;

			await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				if (e.Reset)
				{
					// Find the intersection between the two lists and determine whether to remove or add
					var itemsToRemove = ItemsAdded.Where(x => !e.Paths.Contains(x.Path)).ToList();
					var itemsToAdd = e.Paths.Where(x => !ItemsAdded.Any(y => y.Path == x)).ToList();

					// Remove items
					foreach (var itemToRemove in itemsToRemove)
						ItemsAdded.Remove(itemToRemove);

					// Add items
					foreach (var itemToAdd in itemsToAdd)
					{
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = ItemsAdded.IndexOf(ItemsAdded.FirstOrDefault(x => !x.IsPinned));
						var isPinned = (bool?)e.Items.Where(x => x.FilePath == itemToAdd).FirstOrDefault()?.Properties["System.Home.IsPinned"] ?? false;
						if (ItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						ItemsAdded.Insert(isPinned && lastIndex >= 0 ? lastIndex : ItemsAdded.Count, new FolderCardItem(item, Path.GetFileName(item.Text), isPinned)
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
						var lastIndex = ItemsAdded.IndexOf(ItemsAdded.FirstOrDefault(x => !x.IsPinned));
						if (ItemsAdded.Any(x => x.Path == itemToAdd))
							continue;
						ItemsAdded.Insert(e.Pin && lastIndex >= 0 ? lastIndex : ItemsAdded.Count, new FolderCardItem(item, Path.GetFileName(item.Text), e.Pin) // Add just after the Recent Folders
						{
							Path = item.Path,
						});
					}
				}
				else
					foreach (var itemToRemove in ItemsAdded.Where(x => e.Paths.Contains(x.Path)).ToList())
						ItemsAdded.Remove(itemToRemove);
			});
		}

		private async void QuickAccessWidget_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= QuickAccessWidget_Loaded;

			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync();
			ModifyItem(this, new ModifyQuickAccessEventArgs(itemsToAdd.ToArray(), false)
			{
				Reset = true
			});

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItem;
		}

		private void QuickAccessWidget_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= QuickAccessWidget_Unloaded;
			App.QuickAccessManager.UpdateQuickAccessWidget -= ModifyItem;
		}

		private static async void ItemsAdded_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (FolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		private void MenuFlyout_Opening(object sender)
		{			
			var pinToFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "PinToFavorites");
			if (pinToFavoritesItem is not null)
				pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as FolderCardItem)?.IsPinned ?? false ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "UnpinFromFavorites");
			if (unpinFromFavoritesItem is not null)
				unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as FolderCardItem)?.IsPinned ?? false ? Visibility.Visible : Visibility.Collapsed;
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OpenInNewPane(FolderCardItem item)
		{
			CardNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs { Path = item.Path });
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed) // check middle click
			{
				string navigationPath = ((Button)sender).Tag.ToString()!;
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
			}
		}

		private void OpenProperties(FolderCardItem item)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				ItemContextMenuFlyout.Closed -= flyoutClosed;
				CardPropertiesInvoked?.Invoke(this, new QuickAccessCardEventArgs { Item = item.Item });
			};
			ItemContextMenuFlyout.Closed += flyoutClosed;
		}

		public override async Task PinToFavorites(WidgetCardItem item)
		{
			await QuickAccessService.PinToSidebar(item.Path);

			ModifyItem(this, new ModifyQuickAccessEventArgs(new[] { item.Path }, false));

			var items = (await QuickAccessService.GetPinnedFoldersAsync())
				.Where(link => !((bool?)link.Properties["System.Home.IsPinned"] ?? false));

			var recentItem = items.Where(x => !ItemsAdded.Select(y => y.Path).Contains(x.FilePath)).FirstOrDefault();
			if (recentItem is not null)
			{
				ModifyItem(this, new ModifyQuickAccessEventArgs(new[] { recentItem.FilePath }, true)
				{
					Pin = false
				});
			}
		}

		public override async Task UnpinFromFavorites(WidgetCardItem item)
		{
			await QuickAccessService.UnpinFromSidebar(item.Path);

			ModifyItem(this, new ModifyQuickAccessEventArgs(new[] { item.Path }, false));
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			string ClickedCard = (sender as Button).Tag.ToString();
			string NavigationPath = ClickedCard; // path to navigate

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

		public Task RefreshWidget()
		{
			return Task.CompletedTask;
		}

		public void Dispose() 
		{
		}
	}
}
