using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class RecentFilesWidget : UserControl, IWidgetItemModel, INotifyPropertyChanged
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);

		public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;

		public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);

		public event RecentFileInvokedEventHandler RecentFileInvoked;

		public event PropertyChangedEventHandler PropertyChanged;

		private ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();

		private SemaphoreSlim refreshRecentsSemaphore;

		private CancellationTokenSource refreshRecentsCTS;

		public ICommand RemoveRecentItemCommand;
		public ICommand ClearAllItemsCommand;
		public ICommand OpenFileLocationCommand;

		public string WidgetName => nameof(RecentFilesWidget);

		public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalizedResource();

		public string WidgetHeader => "RecentFiles".GetLocalizedResource();

		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowRecentFilesWidget;

		public bool ShowMenuFlyout => false;

		public MenuFlyoutItem? MenuFlyoutItem => null;

		private bool isEmptyRecentsTextVisible = false;
		public bool IsEmptyRecentsTextVisible
		{
			get => isEmptyRecentsTextVisible;
			internal set
			{
				if (isEmptyRecentsTextVisible != value)
				{
					isEmptyRecentsTextVisible = value;
					NotifyPropertyChanged(nameof(IsEmptyRecentsTextVisible));
				}
			}
		}

		private bool isRecentFilesDisabledInWindows = false;
		public bool IsRecentFilesDisabledInWindows
		{
			get => isRecentFilesDisabledInWindows;
			internal set
			{
				if (isRecentFilesDisabledInWindows != value)
				{
					isRecentFilesDisabledInWindows = value;
					NotifyPropertyChanged(nameof(IsRecentFilesDisabledInWindows));
				}
			}
		}

		public RecentFilesWidget()
		{
			InitializeComponent();

			refreshRecentsSemaphore = new SemaphoreSlim(1, 1);
			refreshRecentsCTS = new CancellationTokenSource();

			// recent files could have changed while widget wasn't loaded
			_ = RefreshWidget();

			App.RecentItemsManager.RecentFilesChanged += Manager_RecentFilesChanged;

			RemoveRecentItemCommand = new RelayCommand<RecentItem>(RemoveRecentItem);
			ClearAllItemsCommand = new RelayCommand(ClearRecentItems);
			OpenFileLocationCommand = new RelayCommand<RecentItem>(OpenFileLocation);
		}

		private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			if (sender is not Grid recentItemsGrid || recentItemsGrid.DataContext is not RecentItem item)
				return;

			var menuItems = GetRecentItemMenuItems(item);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			if (!UserSettingsService.AppearanceSettingsService.MoveShellExtensionsToSubMenu)
				secondaryElements.OfType<FrameworkElement>()
								 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width if the overflow menu setting is disabled

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(recentItemsGrid, new FlyoutShowOptions { Position = e.GetPosition(recentItemsGrid) });

			LoadShellMenuItems(item, itemContextMenuFlyout);

			e.Handled = true;
		}

		private async void LoadShellMenuItems(RecentItem item, CommandBarFlyout itemContextMenuFlyout)
		{
			try
			{
				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
				var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: null,
					new List<ListedItem>() { new ListedItem(null!) { ItemPath = item.RecentPath } }, shiftPressed: shiftPressed, showOpenMenu: false, default);
				if (!UserSettingsService.AppearanceSettingsService.MoveShellExtensionsToSubMenu)
				{
					var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(shellMenuItems);
					if (!secondaryElements.Any())
						return;

					var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);
					var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

					var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
					if (itemsControl is not null)
					{
						var maxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin;
						secondaryElements.OfType<FrameworkElement>()
										 .ForEach(x => x.MaxWidth = maxWidth); // Set items max width to current menu width (#5555)
					}

					itemContextMenuFlyout.SecondaryCommands.Add(new AppBarSeparator());
					secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
				}
				else
				{
					var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(shellMenuItems);
					if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") is not AppBarButton overflowItem)
						return;

					var flyoutItems = (overflowItem.Flyout as MenuFlyout)?.Items;
					if (flyoutItems is not null)
						overflowItems.ForEach(i => flyoutItems.Add(i));
					overflowItem.Visibility = overflowItems.Any() ? Visibility.Visible : Visibility.Collapsed;
				}
			}
			catch { }
		}

		private List<ContextMenuFlyoutItemViewModel> GetRecentItemMenuItems(RecentItem item)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemRemove/Text".GetLocalizedResource(),
					Glyph = "\uF117",
					GlyphFontFamilyName = "CustomGlyph",
					Command = RemoveRecentItemCommand,
					CommandParameter = item,
					ShowItem = true
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemClearAll/Text".GetLocalizedResource(),
					Glyph = "\uF113",
					GlyphFontFamilyName = "CustomGlyph",
					Command = ClearAllItemsCommand,
					ShowItem = true
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemOpenFileLocation/Text".GetLocalizedResource(),
					Glyph = "\uE737",
					Command = OpenFileLocationCommand,
					CommandParameter = item,
					ShowItem = true
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ShowMoreOptions".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsHidden = true,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public async Task RefreshWidget()
		{
			IsRecentFilesDisabledInWindows = App.RecentItemsManager.CheckIsRecentFilesEnabled() is false;
			await App.RecentItemsManager.UpdateRecentFilesAsync();
		}

		private async void Manager_RecentFilesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await DispatcherQueue.EnqueueAsync(async () =>
			{
				// e.Action can only be Reset right now; naively refresh everything for simplicity
				await UpdateRecentsList(e);
			});
		}

		private void OpenFileLocation(RecentItem item)
			=>	RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
				{
					ItemPath = Directory.GetParent(item.RecentPath).FullName,    // parent directory
					ItemName = Path.GetFileName(item.RecentPath),                // file name w extension
				});
			
		private async Task UpdateRecentsList(NotifyCollectionChangedEventArgs e)
		{
			try
			{
				await refreshRecentsSemaphore.WaitAsync(refreshRecentsCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// drop other waiting instances
				refreshRecentsCTS.Cancel();
				refreshRecentsCTS = new CancellationTokenSource();

				IsEmptyRecentsTextVisible = false;

				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
						if (e.NewItems is not null)
						{
							var addedItem = e.NewItems.Cast<RecentItem>().Single();
							AddItemToRecentList(addedItem, 0);
						}
						break;

					case NotifyCollectionChangedAction.Move:
						if (e.OldItems is not null)
						{
							var movedItem = e.OldItems.Cast<RecentItem>().Single();
							recentItemsCollection.RemoveAt(e.OldStartingIndex);
							AddItemToRecentList(movedItem, 0);
						}
						break;

					case NotifyCollectionChangedAction.Remove:
						if (e.OldItems is not null)
						{
							var removedItem = e.OldItems.Cast<RecentItem>().Single();
							recentItemsCollection.RemoveAt(e.OldStartingIndex);
						}
						break;

					// case NotifyCollectionChangedAction.Reset:
					default:
						var recentFiles = App.RecentItemsManager.RecentFiles; // already sorted, add all in order
						if (!recentFiles.SequenceEqual(recentItemsCollection))
						{
							recentItemsCollection.Clear();
							foreach (var item in recentFiles)
							{
								AddItemToRecentList(item);
							}
						}
						break;
				}

				// update chevron if there aren't any items
				if (recentItemsCollection.Count == 0 && !IsRecentFilesDisabledInWindows)
				{
					IsEmptyRecentsTextVisible = true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.Info(ex, "Could not populate recent files");
			}
			finally
			{
				refreshRecentsSemaphore.Release();
			}
		}

		/// <summary>
		/// Add the RecentItem to the ObservableCollection for the UI to render.
		/// </summary>
		/// <param name="recentItem">The recent item to be added</param>
		private bool AddItemToRecentList(RecentItem recentItem, int index = -1)
		{
			if (!recentItemsCollection.Any(x => x.Equals(recentItem)))
			{
				recentItemsCollection.Insert(index < 0 ? recentItemsCollection.Count : Math.Min(index, recentItemsCollection.Count), recentItem);
				_ = recentItem.LoadRecentItemIcon()
					.ContinueWith(t => App.Logger.Warn(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
				return true;
			}
			return false;
		}

		private void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var recentItem = e.ClickedItem as RecentItem;
			RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = recentItem.RecentPath,
				IsFile = recentItem.IsFile
			});
		}

		private async void RemoveRecentItem(RecentItem item)
		{
			await refreshRecentsSemaphore.WaitAsync();

			try
			{
				await App.RecentItemsManager.UnpinFromRecentFiles(item);
			}
			finally
			{
				refreshRecentsSemaphore.Release();
			}
		}

		private async void ClearRecentItems()
		{
			await refreshRecentsSemaphore.WaitAsync();
			try
			{
				recentItemsCollection.Clear();
				bool success = App.RecentItemsManager.ClearRecentItems();

				if (success)
				{
					IsEmptyRecentsTextVisible = true;
				}
			}
			finally
			{
				refreshRecentsSemaphore.Release();
			}
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Dispose()
		{
			App.RecentItemsManager.RecentFilesChanged -= Manager_RecentFilesChanged;
		}
	}
}