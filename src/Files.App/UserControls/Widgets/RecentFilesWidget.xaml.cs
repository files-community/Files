using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
using Files.App.ViewModels.Widgets;
using Files.Shared.Extensions;
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
using Windows.System;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class RecentFilesWidget : HomePageWidget, IWidgetItemModel, INotifyPropertyChanged
	{
		public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);

		public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;

		public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);

		public event RecentFileInvokedEventHandler RecentFileInvoked;

		public event PropertyChangedEventHandler PropertyChanged;

		private ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();

		private SemaphoreSlim refreshRecentsSemaphore;

		private CancellationTokenSource refreshRecentsCTS;

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
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (sender is not Grid recentItemsGrid || recentItemsGrid.DataContext is not RecentItem item)
				return;

			var menuItems = GetItemMenuItems(item, false);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
							 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(recentItemsGrid, new FlyoutShowOptions { Position = e.GetPosition(recentItemsGrid) });

			_ = ShellContextmenuHelper.LoadShellMenuItems(item.Path, itemContextMenuFlyout, showOpenWithMenu: true, showSendToMenu: true);

			e.Handled = true;
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenItemsWithCaptionText".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemRemove/Text".GetLocalizedResource(),
					Glyph = "\uE738",
					Command = RemoveRecentItemCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RecentItemClearAll/Text".GetLocalizedResource(),
					Glyph = "\uE74D",
					Command = ClearAllItemsCommand
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			};
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
		{
			RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item.RecentPath).FullName,    // parent directory
				ItemName = Path.GetFileName(item.RecentPath),                // file name w extension
			});
		}

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