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
using Windows.Foundation.Metadata;
using Windows.System;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of recent folders with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class RecentFilesWidget : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;
		public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event RecentFileInvokedEventHandler RecentFileInvoked;
		public event PropertyChangedEventHandler PropertyChanged;

		private ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();

		private SemaphoreSlim refreshRecentsSemaphore;

		private CancellationTokenSource refreshRecentsCTS;

		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public string WidgetName => nameof(RecentFilesWidget);
		public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
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

		public RecentFilesWidget()
		{
			InitializeComponent();

			refreshRecentsSemaphore = new SemaphoreSlim(1, 1);
			refreshRecentsCTS = new CancellationTokenSource();

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
			// Ensure values are not null
			if (e.OriginalSource is not FrameworkElement element ||
				element.DataContext is not RecentItem item)
				return;

			// Create a new Flyout
			var itemContextMenuFlyout = new CommandBarFlyout()
			{
				Placement = FlyoutPlacementMode.Full
			};

			// Hook events
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			itemContextMenuFlyout.Closed += (sender, e) => OnRightClickedItemChanged(null, null);

			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, itemContextMenuFlyout);

			// Get items for the flyout
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(element, new() { Position = e.GetPosition(element) });

			// Load shell menu items
			_ = ShellContextFlyoutFactory.LoadShellMenuItemsAsync(_flyoutItemPath, itemContextMenuFlyout);
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
					ShowItem = userSettingsService.GeneralSettingsService.ShowSendToMenu
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
			await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				// e.Action can only be Reset right now; naively refresh everything for simplicity
				await UpdateRecentsListAsync(e);
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

		private void OpenProperties(RecentItem item)
		{
			var flyout = HomePageContext.ItemContextFlyoutMenu;

			if (item is null || flyout is null)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = async (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;

				BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path));
				if (file is null)
				{
					ContentDialog dialog = new()
					{
						Title = "CannotAccessPropertiesTitle".GetLocalizedResource(),
						Content = "CannotAccessPropertiesContent".GetLocalizedResource(),
						PrimaryButtonText = "Ok".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.TryShowAsync();
				}
				else
				{
					var listedItem = await UniversalStorageEnumerator.AddFileAsync(file, null, default);
					FilePropertiesHelpers.OpenPropertiesWindow(listedItem, associatedInstance);
				}
			};
			flyout!.Closed += flyoutClosed;
		}

		private async Task UpdateRecentsListAsync(NotifyCollectionChangedEventArgs e)
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
				App.Logger.LogInformation(ex, "Could not populate recent files");
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
				_ = recentItem.LoadRecentItemIconAsync()
					.ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);
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

		private async Task RemoveRecentItemAsync(RecentItem item)
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

		private async Task ClearRecentItemsAsync()
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
