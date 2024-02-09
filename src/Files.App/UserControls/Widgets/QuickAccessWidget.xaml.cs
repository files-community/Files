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
	/// Represents group of control displays a list of quick access folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class QuickAccessWidget : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		public IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public static ObservableCollection<WidgetFolderCardItem> ItemsAdded = new();

		static QuickAccessWidget()
		{
			ItemsAdded.CollectionChanged += ItemsAdded_CollectionChanged;
		}

		public QuickAccessWidget()
		{
			InitializeComponent();

			Loaded += QuickAccessWidget_Loaded;
			Unloaded += QuickAccessWidget_Unloaded;

			OpenInNewTabCommand = new AsyncRelayCommand<WidgetFolderCardItem>(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetFolderCardItem>(OpenInNewWindowAsync);
			OpenInNewPaneCommand = new RelayCommand<WidgetFolderCardItem>(OpenInNewPane);
			OpenPropertiesCommand = new RelayCommand<WidgetFolderCardItem>(OpenProperties);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetFolderCardItem>(PinToFavoritesAsync);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetFolderCardItem>(UnpinFromFavoritesAsync);
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

		private async void ModifyItemAsync(object? sender, ModifyQuickAccessEventArgs? e)
		{
			if (e is null)
				return;

			await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				if (e.Reset)
				{
					// Find the intersection between the two lists and determine whether to remove or add
					var originalItemsAdded = ItemsAdded.ToList();
					var itemsToRemove = originalItemsAdded.Where(x => !e.Paths.Contains(x.Path));
					var itemsToAdd = e.Paths.Where(x => !originalItemsAdded.Any(y => y.Path == x));

					// Remove items
					foreach (var itemToRemove in itemsToRemove)
						ItemsAdded.Remove(itemToRemove);

					// Add items
					foreach (var itemToAdd in itemsToAdd)
					{
						var interimItemsAdded = ItemsAdded.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = ItemsAdded.IndexOf(interimItemsAdded.FirstOrDefault(x => !x.IsPinned));
						var isPinned = (bool?)e.Items.Where(x => x.FilePath == itemToAdd).FirstOrDefault()?.Properties["System.Home.IsPinned"] ?? false;
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						ItemsAdded.Insert(isPinned && lastIndex >= 0 ? Math.Min(lastIndex, ItemsAdded.Count) : ItemsAdded.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), isPinned)
						{
							Path = item.Path,
						});
					}

					return;
				}
				if (e.Reorder)
				{
					// Remove pinned items
					foreach (var itemToRemove in ItemsAdded.ToList().Where(x => x.IsPinned))
						ItemsAdded.Remove(itemToRemove);

					// Add pinned items in the new order
					foreach (var itemToAdd in e.Paths)
					{
						var interimItemsAdded = ItemsAdded.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = ItemsAdded.IndexOf(interimItemsAdded.FirstOrDefault(x => !x.IsPinned));
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;

						ItemsAdded.Insert(lastIndex >= 0 ? Math.Min(lastIndex, ItemsAdded.Count) : ItemsAdded.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), true)
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
						var interimItemsAdded = ItemsAdded.ToList();
						var item = await App.QuickAccessManager.Model.CreateLocationItemFromPathAsync(itemToAdd);
						var lastIndex = ItemsAdded.IndexOf(interimItemsAdded.FirstOrDefault(x => !x.IsPinned));
						if (interimItemsAdded.Any(x => x.Path == itemToAdd))
							continue;
						ItemsAdded.Insert(e.Pin && lastIndex >= 0 ? Math.Min(lastIndex, ItemsAdded.Count) : ItemsAdded.Count, new WidgetFolderCardItem(item, Path.GetFileName(item.Text), e.Pin) // Add just after the Recent Folders
						{
							Path = item.Path,
						});
					}
				}
				else
					foreach (var itemToRemove in ItemsAdded.ToList().Where(x => e.Paths.Contains(x.Path)))
						ItemsAdded.Remove(itemToRemove);
			});
		}

		private async void QuickAccessWidget_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= QuickAccessWidget_Loaded;

			var itemsToAdd = await QuickAccessService.GetPinnedFoldersAsync();
			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(itemsToAdd.ToArray(), false)
			{
				Reset = true
			});

			App.QuickAccessManager.UpdateQuickAccessWidget += ModifyItemAsync;
		}

		private void QuickAccessWidget_Unloaded(object sender, RoutedEventArgs e)
		{
			Unloaded -= QuickAccessWidget_Unloaded;
			App.QuickAccessManager.UpdateQuickAccessWidget -= ModifyItemAsync;
		}

		private static async void ItemsAdded_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetFolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		private void MenuFlyout_Opening(object sender)
		{
			var pinToFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "PinToFavorites");
			if (pinToFavoritesItem is not null)
				pinToFavoritesItem.Visibility = (pinToFavoritesItem.DataContext as WidgetFolderCardItem)?.IsPinned ?? false ? Visibility.Collapsed : Visibility.Visible;

			var unpinFromFavoritesItem = (sender as MenuFlyout)?.Items.SingleOrDefault(x => x.Name == "UnpinFromFavorites");
			if (unpinFromFavoritesItem is not null)
				unpinFromFavoritesItem.Visibility = (unpinFromFavoritesItem.DataContext as WidgetFolderCardItem)?.IsPinned ?? false ? Visibility.Visible : Visibility.Collapsed;
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void OpenInNewPane(WidgetFolderCardItem item)
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

		private void OpenProperties(WidgetFolderCardItem item)
		{
			if (!HomePageContext.IsAnyItemRightClicked)
				return;

			var flyout = HomePageContext.ItemContextFlyoutMenu;
			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;
				CardPropertiesInvoked?.Invoke(this, new QuickAccessCardEventArgs { Item = item.Item });
			};

			flyout!.Closed += flyoutClosed;
		}

		public override async Task PinToFavoritesAsync(WidgetCardItem item)
		{
			await QuickAccessService.PinToSidebarAsync(item.Path);

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { item.Path }, false));

			var items = (await QuickAccessService.GetPinnedFoldersAsync())
				.Where(link => !((bool?)link.Properties["System.Home.IsPinned"] ?? false));

			var recentItem = items.Where(x => !ItemsAdded.ToList().Select(y => y.Path).Contains(x.FilePath)).FirstOrDefault();
			if (recentItem is not null)
			{
				ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { recentItem.FilePath }, true)
				{
					Pin = false
				});
			}
		}

		public override async Task UnpinFromFavoritesAsync(WidgetCardItem item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item.Path);

			ModifyItemAsync(this, new ModifyQuickAccessEventArgs(new[] { item.Path }, false));
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

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		public void Dispose()
		{
		}
	}
}
