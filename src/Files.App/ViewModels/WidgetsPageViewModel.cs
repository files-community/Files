using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.EventArguments.Bundles;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Widgets;
using Files.App.ViewModels.Widgets.Bundles;
using Files.Backend.Services;
using Files.Sdk.Storage.LocatableStorage;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Files.App.ViewModels.Pages
{
	public class WidgetsPageViewModel : ObservableObject, IDisposable
	{
		private BundlesViewModel bundlesViewModel;
		private SemaphoreSlim refreshRecentsSemaphore;
		private CancellationTokenSource refreshRecentsCTS;

		private readonly IRecentItemsService recentItemsService = Ioc.Default.GetRequiredService<IRecentItemsService>();
		private readonly WidgetsListControlViewModel widgetsViewModel;
		private readonly JsonElement defaultJson = JsonSerializer.SerializeToElement("{}");

		private IShellPage associatedInstance;
		public event EventHandler<RoutedEventArgs> YourHomeLoadedInvoked;

		public ICommand YourHomeLoadedCommand { get; private set; }
		public ICommand LoadBundlesCommand { get; private set; }
		public ObservableCollection<RecentItem> RecentItemsCollection = new ObservableCollection<RecentItem>();
		// recent files
		private readonly List<ILocatableStorable> recentFiles = new();
		public IReadOnlyList<RecentItem> RecentFiles    // already sorted
		{
			get
			{
				lock (recentFiles)
				{
					return recentFiles.Cast<RecentItem>().ToList().AsReadOnly();
				}
			}
		}

		// recent folders
		private readonly List<ILocatableStorable> recentFolders = new();
		public IReadOnlyList<RecentItem> RecentFolders  // already sorted
		{
			get
			{
				lock (recentFolders)
				{
					return recentFolders.Cast<RecentItem>().ToList().AsReadOnly();
				}
			}
		}

		private bool isEmptyRecentsTextVisible = false;
		public bool IsEmptyRecentsTextVisible
		{
			get => isEmptyRecentsTextVisible;
			set => SetProperty(ref isEmptyRecentsTextVisible, value);
		}

		private bool isRecentFilesDisabledInWindows = false;
		public bool IsRecentFilesDisabledInWindows
		{
			get => isRecentFilesDisabledInWindows;
			internal set => SetProperty(ref isRecentFilesDisabledInWindows, value);
		}

		public WidgetsPageViewModel(WidgetsListControlViewModel widgetsViewModel, IShellPage associatedInstance)
		{
			this.widgetsViewModel = widgetsViewModel;
			this.associatedInstance = associatedInstance;

			refreshRecentsSemaphore = new SemaphoreSlim(1, 1);
			refreshRecentsCTS = new CancellationTokenSource();
			// Create commands
			
			YourHomeLoadedCommand = new RelayCommand<RoutedEventArgs>(YourHomeLoaded);
			LoadBundlesCommand = new RelayCommand<BundlesViewModel>(LoadBundles);

			IsRecentFilesDisabledInWindows = !recentItemsService.IsSupported();
			RecentItemsManager.Default.RecentItemsChanged += Default_RecentItemsChanged;
		}

		private async void Default_RecentItemsChanged(object? sender, EventArgs e)
		{
			await OnRecentItemsChangedAsync();
		}

		public async Task OnRecentItemsChangedAsync()
		{
			var args = await recentItemsService.UpdateRecentFilesAsync(recentFiles);
			await App.Window.DispatcherQueue.EnqueueAsync(async () => await UpdateRecentsListAsync(args));
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
							RecentItemsCollection.RemoveAt(e.OldStartingIndex);
							AddItemToRecentList(movedItem, 0);
						}
						break;

					case NotifyCollectionChangedAction.Remove:
						if (e.OldItems is not null)
						{
							var removedItem = e.OldItems.Cast<RecentItem>().Single();
							RecentItemsCollection.RemoveAt(e.OldStartingIndex);
						}
						break;

					// case NotifyCollectionChangedAction.Reset:
					default:
						var recentFiles = RecentFiles; // already sorted, add all in order
						if (!recentFiles.SequenceEqual(RecentItemsCollection))
						{
							RecentItemsCollection.Clear();
							foreach (var item in recentFiles)
							{
								AddItemToRecentList(item);
							}
						}
						break;
				}

				// update chevron if there aren't any items
				if (RecentItemsCollection.Count == 0 && !IsRecentFilesDisabledInWindows)
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
			if (!RecentItemsCollection.Any(x => x.Equals(recentItem)))
			{
				RecentItemsCollection.Insert(index < 0 ? RecentItemsCollection.Count : Math.Min(index, RecentItemsCollection.Count), recentItem);
				_ = recentItem.LoadRecentItemIcon()
					.ContinueWith(t => App.Logger.LogWarning(t.Exception, string.Empty), TaskContinuationOptions.OnlyOnFaulted);
				return true;
			}
			return false;
		}

		public async void RemoveRecentItem(RecentItem item)
		{
			await refreshRecentsSemaphore.WaitAsync();

			try
			{
				await recentItemsService.UnpinFromRecentFilesAsync(item);
			}
			finally
			{
				refreshRecentsSemaphore.Release();
			}
		}

		public async void ClearRecentItems()
		{
			await refreshRecentsSemaphore.WaitAsync();
			try
			{
				RecentItemsCollection.Clear();
				bool success = recentItemsService.ClearRecentItems();

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

		public void ChangeAppInstance(IShellPage associatedInstance)
		{
			this.associatedInstance = associatedInstance;
		}

		private void YourHomeLoaded(RoutedEventArgs e)
		{
			YourHomeLoadedInvoked?.Invoke(this, e);
		}

		private async void LoadBundles(BundlesViewModel viewModel)
		{
			bundlesViewModel = viewModel;

			bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
			bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
			bundlesViewModel.OpenPathEvent += BundlesViewModel_OpenPathEvent;
			bundlesViewModel.OpenPathInNewPaneEvent += BundlesViewModel_OpenPathInNewPaneEvent;

			await bundlesViewModel.Initialize();
		}

		private void BundlesViewModel_OpenPathInNewPaneEvent(object sender, string e)
		{
			associatedInstance.PaneHolder.OpenPathInNewPane(e);
		}

		private async void BundlesViewModel_OpenPathEvent(object sender, BundlesOpenPathEventArgs e)
		{
			await NavigationHelpers.OpenPath(e.path, associatedInstance, e.itemType, e.openSilent, e.openViaApplicationPicker, e.selectItems);
		}

		#region IDisposable

		public void Dispose()
		{
			if (bundlesViewModel is not null)
			{
				bundlesViewModel.OpenPathEvent -= BundlesViewModel_OpenPathEvent;
				bundlesViewModel.OpenPathInNewPaneEvent -= BundlesViewModel_OpenPathInNewPaneEvent;
			}
			RecentItemsManager.Default.RecentItemsChanged -= Default_RecentItemsChanged;

			widgetsViewModel?.Dispose();
		}

		#endregion IDisposable
	}
}
