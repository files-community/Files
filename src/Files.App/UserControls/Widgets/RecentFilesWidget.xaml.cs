using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

		private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
		{
			var flyoutItem = sender as MenuFlyoutItem;
			var clickedOnItem = flyoutItem.DataContext as RecentItem;
			if (clickedOnItem.IsFile)
			{
				var targetPath = clickedOnItem.RecentPath;
				RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
				{
					ItemPath = Directory.GetParent(targetPath).FullName,    // parent directory
					ItemName = Path.GetFileName(targetPath),                // file name w extension
				});
			}
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

		private async void RemoveRecentItem_Click(object sender, RoutedEventArgs e)
		{
			await refreshRecentsSemaphore.WaitAsync();

			try
			{
				// Get the sender FrameworkElement and grab its DataContext ViewModel
				if (sender is MenuFlyoutItem fe && fe.DataContext is RecentItem vm)
				{
					// evict it from the recent items shortcut list
					// this operation invokes RecentFilesChanged which we handle to update the visible collection
					await App.RecentItemsManager.UnpinFromRecentFiles(vm);
				}
			}
			finally
			{
				refreshRecentsSemaphore.Release();
			}
		}

		private async void ClearRecentItems_Click(object sender, RoutedEventArgs e)
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