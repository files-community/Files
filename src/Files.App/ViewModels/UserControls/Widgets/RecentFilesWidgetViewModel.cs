// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Fields

		private readonly SemaphoreSlim _refreshRecentItemsSemaphore;
		private CancellationTokenSource _refreshRecentItemsCTS;

		// Properties

		public ObservableCollection<RecentItem> Items { get; } = [];

		public string WidgetName => nameof(RecentFilesWidgetViewModel);
		public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem { get; } = null;

		private bool _IsEmptyRecentItemsTextVisible;
		public bool IsEmptyRecentItemsTextVisible
		{
			get => _IsEmptyRecentItemsTextVisible;
			set
			{
				if (_IsEmptyRecentItemsTextVisible != value)
				{
					_IsEmptyRecentItemsTextVisible = value;
					NotifyPropertyChanged(nameof(IsEmptyRecentItemsTextVisible));
				}
			}
		}

		private bool _IsRecentFilesDisabledInWindows;
		public bool IsRecentFilesDisabledInWindows
		{
			get => _IsRecentFilesDisabledInWindows;
			internal set
			{
				if (_IsRecentFilesDisabledInWindows != value)
				{
					_IsRecentFilesDisabledInWindows = value;
					NotifyPropertyChanged(nameof(IsRecentFilesDisabledInWindows));
				}
			}
		}

		// Events

		public event PropertyChangedEventHandler? PropertyChanged;

		// Constructor

		public RecentFilesWidgetViewModel()
		{
			_refreshRecentItemsSemaphore = new SemaphoreSlim(1, 1);
			_refreshRecentItemsCTS = new CancellationTokenSource();

			_ = RefreshWidgetAsync();

			App.RecentItemsManager.RecentFilesChanged += Manager_RecentFilesChanged;

			RemoveRecentItemCommand = new AsyncRelayCommand<RecentItem>(RemoveRecentItemAsync);
			ClearAllItemsCommand = new AsyncRelayCommand(ClearRecentItemsAsync);
			OpenFileLocationCommand = new AsyncRelayCommand<RecentItem>(OpenFileLocation);
			OpenPropertiesCommand = new RelayCommand<RecentItem>(OpenProperties);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			IsRecentFilesDisabledInWindows = App.RecentItemsManager.CheckIsRecentFilesEnabled() is false;
			await App.RecentItemsManager.UpdateRecentFilesAsync();
		}

		public async Task OpenFileLocation(RecentItem? item)
		{
			if (item is null)
				return;

			try
			{
				if (item.IsFile)
				{
					var directoryName = SystemIO.Path.GetDirectoryName(item.RecentPath);

					await Win32Helpers.InvokeWin32ComponentAsync(
						item.RecentPath,
						ContentPageContext.ShellPage!,
						workingDirectory: directoryName ?? string.Empty);
				}
				else
				{
					ContentPageContext.ShellPage!.NavigateWithArguments(
						ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.GetLayoutType(item.RecentPath)!,
						new() { NavPathParam = item.RecentPath });
				}
			}
			catch (UnauthorizedAccessException)
			{
				var dialog = DynamicDialogFactory.GetFor_ConsentDialog();

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await dialog.TryShowAsync();
			}
			catch (COMException) { }
			catch (ArgumentException) { }
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "RecentItemRemove/Text".GetLocalizedResource(),
					Glyph = "\uE738",
					Command = RemoveRecentItemCommand,
					CommandParameter = item
				},
				new()
				{
					Text = "RecentItemClearAll/Text".GetLocalizedResource(),
					Glyph = "\uE74D",
					Command = ClearAllItemsCommand
				},
				new()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
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

		private async Task UpdateRecentItemsListAsync(NotifyCollectionChangedEventArgs e)
		{
			try
			{
				await _refreshRecentItemsSemaphore.WaitAsync(_refreshRecentItemsCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// drop other waiting instances
				_refreshRecentItemsCTS.Cancel();
				_refreshRecentItemsCTS = new();

				IsEmptyRecentItemsTextVisible = false;

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
					IsEmptyRecentItemsTextVisible = true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, "Could not populate recent files");
			}
			finally
			{
				_refreshRecentItemsSemaphore.Release();
			}
		}

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

		// Event methods

		private async void Manager_RecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				// e.Action can only be Reset right now; naively refresh everything for simplicity
				await UpdateRecentItemsListAsync(e);
			});
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Command methods

		private async Task ClearRecentItemsAsync()
		{
			await _refreshRecentItemsSemaphore.WaitAsync();
			try
			{
				Items.Clear();
				bool success = App.RecentItemsManager.ClearRecentItems();

				if (success)
				{
					IsEmptyRecentItemsTextVisible = true;
				}
			}
			finally
			{
				_refreshRecentItemsSemaphore.Release();
			}
		}

		private async Task RemoveRecentItemAsync(RecentItem? item)
		{
			if (item is null)
				return;

			await _refreshRecentItemsSemaphore.WaitAsync();

			try
			{
				await App.RecentItemsManager.UnpinFromRecentFiles(item);
			}
			finally
			{
				_refreshRecentItemsSemaphore.Release();
			}
		}

		private void OpenProperties(RecentItem? item)
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
					var listedItem = await UniversalStorageEnumerator.AddFileAsync(file, null!, default);
					FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
				}
			};
			flyout!.Closed += flyoutClosed;
		}

		// Disposer

		public void Dispose()
		{
			App.RecentItemsManager.RecentFilesChanged -= Manager_RecentFilesChanged;
		}
	}
}
