// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Dependency injections

		public string WidgetName => "RecentFiles";
		public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
		public bool ShowMenuFlyout => false;

		public MenuFlyoutItem? MenuFlyoutItem
			=> null;

		private SemaphoreSlim _refreshRecentFilesSemaphore;

		private CancellationTokenSource _refreshRecentFilesCTS;

		public ObservableCollection<RecentItem> Items = new();

		private bool isEmptyRecentFilesTextVisible = false;
		public bool IsEmptyRecentFilesTextVisible
		{
			get => isEmptyRecentFilesTextVisible;
			internal set
			{
				if (isEmptyRecentFilesTextVisible != value)
				{
					isEmptyRecentFilesTextVisible = value;
					NotifyPropertyChanged(nameof(IsEmptyRecentFilesTextVisible));
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

		private IShellPage associatedInstance;
		public IShellPage AppInstance
		{
			get => associatedInstance;
			set
			{
				if (value != associatedInstance)
				{
					associatedInstance = value;
					NotifyPropertyChanged(nameof(AppInstance));
				}
			}
		}

		// Events

		public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;
		public event RecentFileInvokedEventHandler RecentFileInvoked;
		public event PropertyChangedEventHandler PropertyChanged;

		public RecentFilesWidgetViewModel()
		{
			_refreshRecentFilesSemaphore = new SemaphoreSlim(1, 1);
			_refreshRecentFilesCTS = new CancellationTokenSource();

			// recent files could have changed while widget wasn't loaded
			_ = RefreshWidgetAsync();

			App.RecentItemsManager.RecentFilesChanged += Manager_RecentFilesChanged;

			RemoveRecentItemCommand = new AsyncRelayCommand<RecentItem>(RemoveRecentItemAsync);
			ClearAllItemsCommand = new AsyncRelayCommand(ClearRecentItemsAsync);
			OpenFileLocationCommand = new RelayCommand<RecentItem>(OpenFileLocation);
			OpenPropertiesCommand = new RelayCommand<RecentItem>(OpenProperties);
		}

		private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ItemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			ItemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (e.OriginalSource is not FrameworkElement element || element.DataContext is not RecentItem item)
				return;

			var menuItems = GetItemMenuItems(item, false);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
							 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => ItemContextMenuFlyout.SecondaryCommands.Add(i));
			FlyoutItemPath = item.Path;
			ItemContextMenuFlyout.Opened += ItemContextMenuFlyout_Opened;
			ItemContextMenuFlyout.ShowAt(element, new FlyoutShowOptions { Position = e.GetPosition(element) });
		}

		private async void ItemContextMenuFlyout_Opened(object? sender, object e)
		{
			ItemContextMenuFlyout.Opened -= ItemContextMenuFlyout_Opened;
			await ShellContextmenuHelper.LoadShellMenuItemsAsync(FlyoutItemPath, ItemContextMenuFlyout, showOpenWithMenu: true, showSendToMenu: true);
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
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

		public async Task RefreshWidgetAsync()
		{
			IsRecentFilesDisabledInWindows = App.RecentItemsManager.CheckIsRecentFilesEnabled() is false;
			await App.RecentItemsManager.UpdateRecentFilesAsync();
		}

		private async void Manager_RecentFilesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				// e.Action can only be Reset right now; naively refresh everything for simplicity
				await UpdateRecentFilesListAsync(e);
			});
		}

		private void OpenFileLocation(RecentItem item)
		{
			RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				// Parent directory
				ItemPath = Directory.GetParent(item.RecentPath).FullName,

				// File name with extension
				ItemName = Path.GetFileName(item.RecentPath),
			});
		}

		private void OpenProperties(RecentItem item)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = async (s, e) =>
			{
				ItemContextMenuFlyout.Closed -= flyoutClosed;
				var listedItem = await UniversalStorageEnumerator.AddFileAsync(await BaseStorageFile.GetFileFromPathAsync(item.Path), null, default);
				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, associatedInstance);
			};
			ItemContextMenuFlyout.Closed += flyoutClosed;
		}

		private async Task UpdateRecentFilesListAsync(NotifyCollectionChangedEventArgs e)
		{
			try
			{
				await _refreshRecentFilesSemaphore.WaitAsync(_refreshRecentFilesCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// Drop other waiting instances
				_refreshRecentFilesCTS.Cancel();
				_refreshRecentFilesCTS = new CancellationTokenSource();

				IsEmptyRecentFilesTextVisible = false;

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
							Items.RemoveAt(e.OldStartingIndex);
							AddItemToRecentList(movedItem, 0);
						}
						break;

					case NotifyCollectionChangedAction.Remove:
						if (e.OldItems is not null)
						{
							var removedItem = e.OldItems.Cast<RecentItem>().Single();
							Items.RemoveAt(e.OldStartingIndex);
						}
						break;

					// case NotifyCollectionChangedAction.Reset:
					default:
						var recentFiles = App.RecentItemsManager.RecentFiles; // already sorted, add all in order
						if (!recentFiles.SequenceEqual(Items))
						{
							Items.Clear();
							foreach (var item in recentFiles)
							{
								AddItemToRecentList(item);
							}
						}
						break;
				}

				// update chevron if there aren't any items
				if (Items.Count == 0 && !IsRecentFilesDisabledInWindows)
				{
					IsEmptyRecentFilesTextVisible = true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, "Could not populate recent files");
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
			}
		}

		/// <summary>
		/// Add the RecentItem to the ObservableCollection for the UI to render.
		/// </summary>
		/// <param name="recentItem">The recent item to be added</param>
		private bool AddItemToRecentList(RecentItem recentItem, int index = -1)
		{
			if (!Items.Any(x => x.Equals(recentItem)))
			{
				Items.Insert(index < 0 ? Items.Count : Math.Min(index, Items.Count), recentItem);
				_ = recentItem.LoadRecentItemIconAsync()
					.ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);
				return true;
			}
			return false;
		}

		public void RecentFilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var recentItem = e.ClickedItem as RecentItem;
			RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = recentItem.RecentPath,
				IsFile = recentItem.IsFile
			});
		}

		private async Task RemoveRecentItemAsync(RecentItem item)
		{
			await _refreshRecentFilesSemaphore.WaitAsync();

			try
			{
				await App.RecentItemsManager.UnpinFromRecentFiles(item);
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
			}
		}

		private async Task ClearRecentItemsAsync()
		{
			await _refreshRecentFilesSemaphore.WaitAsync();
			try
			{
				Items.Clear();
				bool success = App.RecentItemsManager.ClearRecentItems();

				if (success)
				{
					IsEmptyRecentFilesTextVisible = true;
				}
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
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
