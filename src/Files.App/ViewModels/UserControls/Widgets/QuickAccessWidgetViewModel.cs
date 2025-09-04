// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.WinRT;
using Windows.Win32.UI.Shell;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="QuickAccessWidget"/>.
	/// </summary>
	public sealed partial class QuickAccessWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetFolderCardItem> Items { get; } = [];

		public string WidgetName => nameof(QuickAccessWidget);
		public string AutomationProperties => Strings.QuickAccess.GetLocalizedResource();
		public string WidgetHeader => Strings.QuickAccess.GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Fields

		// TODO: Replace with IMutableFolder.GetWatcherAsync() once it gets implemented in IWindowsStorable
		private readonly SystemIO.FileSystemWatcher _quickAccessFolderWatcher;

		// Constructor

		public QuickAccessWidgetViewModel()
		{
			Items.CollectionChanged += Items_CollectionChanged;

			OpenPropertiesCommand = new RelayCommand<WidgetFolderCardItem>(ExecuteOpenPropertiesCommand);
			PinToSidebarCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetFolderCardItem>(ExecuteUnpinFromSidebarCommand);

			_quickAccessFolderWatcher = new()
			{
				Path = SystemIO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations"),
				Filter = "f01b4d95cf55d32a.automaticDestinations-ms",
				NotifyFilter = SystemIO.NotifyFilters.LastAccess | SystemIO.NotifyFilters.LastWrite | SystemIO.NotifyFilters.FileName
			};

			_quickAccessFolderWatcher.Changed += async (s, e) =>
			{
				await RefreshWidgetAsync();
			};

			_quickAccessFolderWatcher.EnableRaisingEvents = true;
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (var item in Items)
					item.Dispose();

				Items.Clear();

				await foreach (IWindowsStorable folder in HomePageContext.HomeFolder.GetQuickAccessFolderAsync(default))
				{
					folder.GetPropertyValue<bool>("System.Home.IsPinned", out var isPinned);
					folder.TryGetShellTooltip(out var tooltip);

					Items.Insert(
						Items.Count,
						new WidgetFolderCardItem(
							folder,
							folder.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI),
							isPinned,
							tooltip ?? string.Empty));
				}
			});
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHome)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab && CommandManager.OpenInNewTabFromHome.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHome)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow && CommandManager.OpenInNewWindowFromHome.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHome)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && CommandManager.OpenInNewPaneFromHome.IsExecutable
				}.Build(),
				new()
				{
					Text = Strings.PinFolderToSidebar.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.FavoritePin" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = Strings.UnpinFolderFromSidebar.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.FavoritePinRemove" },
					Command = UnpinFromSidebarCommand,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new()
				{
					Text = Strings.SendTo.GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = Strings.Properties.GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.Properties" },
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenTerminal && CommandManager.OpenTerminalFromHome.IsExecutable
				},
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenTerminalFromHome)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenTerminal && CommandManager.OpenTerminalFromHome.IsExecutable
				}.Build(),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = Strings.Loading.GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public async Task NavigateToPath(string path)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			ContentPageContext.ShellPage?.NavigateWithArguments(
				ContentPageContext.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new() { NavPathParam = path });
		}

		// Event methods

		private async void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetFolderCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		// Command methods

		public override async Task ExecutePinToSidebarCommand(WidgetCardItem? item)
		{
			if (item is not WidgetFolderCardItem folderCardItem || folderCardItem.Path is null)
				return;

			var lastPinnedItemIndex = Items.LastOrDefault(x => x.IsPinned) is { } lastPinnedItem ? Items.IndexOf(lastPinnedItem) : 0;
			var currentPinnedItemIndex = Items.IndexOf(folderCardItem);
			if (currentPinnedItemIndex is -1)
				return;

			HRESULT hr = default;
			using ComPtr<IAgileReference> pAgileReference = default;

			unsafe
			{
				hr = PInvoke.RoGetAgileReference(AgileReferenceOptions.AGILEREFERENCE_DEFAULT, IID.IID_IShellItem, (IUnknown*)folderCardItem.Item.ThisPtr, pAgileReference.GetAddressOf());
			}

			// Pin to Quick Access on Windows
			hr = await STATask.Run(() =>
			{
				unsafe
				{
					IShellItem* pShellItem = null;
					hr = pAgileReference.Get()->Resolve(IID.IID_IShellItem, (void**)&pShellItem);
					using var windowsFile = new WindowsFile(pShellItem);

					// NOTE: "pintohome" is an undocumented verb, which calls an undocumented COM class, windows.storage.dll!CPinToFrequentExecute : public IExecuteCommand, ...
					return windowsFile.TryInvokeContextMenuVerb("pintohome");
				}
			});

			if (hr.ThrowIfFailedOnDebug().Failed)
				return;

			// Add this to right before the last pinned item
			// NOTE: To be honest, this is not needed as the file watcher will take care of this
			if (lastPinnedItemIndex + 1 != currentPinnedItemIndex)
				Items.Move(currentPinnedItemIndex, lastPinnedItemIndex + 1);
		}

		public override async Task ExecuteUnpinFromSidebarCommand(WidgetCardItem? item)
		{
			if (item is not WidgetFolderCardItem folderCardItem || folderCardItem.Path is null)
				return;

			HRESULT hr = default;
			using ComPtr<IAgileReference> pAgileReference = default;

			unsafe
			{
				hr = PInvoke.RoGetAgileReference(AgileReferenceOptions.AGILEREFERENCE_DEFAULT, IID.IID_IShellItem, (IUnknown*)folderCardItem.Item.ThisPtr, pAgileReference.GetAddressOf());
			}

			// Unpin from Quick Access on Windows
			hr = await STATask.Run(() =>
			{
				unsafe
				{
					IShellItem* pShellItem = null;
					hr = pAgileReference.Get()->Resolve(IID.IID_IShellItem, (void**)&pShellItem);
					using var windowsFile = new WindowsFile(pShellItem);

					// NOTE: "unpinfromhome" is an undocumented verb, which calls an undocumented COM class, windows.storage.dll!CRemoveFromFrequentPlacesExecute : public IExecuteCommand, ...
					// NOTE: "remove" is for some shell folders where the "unpinfromhome" may not work
					return windowsFile.TryInvokeContextMenuVerbs(["unpinfromhome", "remove"], true);
				}
			});

			if (hr.ThrowIfFailedOnDebug().Failed)
				return;

			Items.Remove(folderCardItem);
		}

		private void ExecuteOpenPropertiesCommand(WidgetFolderCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked || item is null || item.Item is null)
				return;

			var flyout = HomePageContext.ItemContextFlyoutMenu;
			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = async (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;

				ListedItem listedItem = new(null!)
				{
					ItemPath = item.Path,
					ItemNameRaw = item.Text,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = Strings.Folder.GetLocalizedResource(),
				};

				if (!string.Equals(item.Path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				{
					BaseStorageFolder matchingStorageFolder = await ContentPageContext.ShellPage!.ShellViewModel.GetFolderFromPathAsync(item.Path);
					if (matchingStorageFolder is not null)
					{
						var syncStatus = await ContentPageContext.ShellPage!.ShellViewModel.CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
						listedItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
					}
				}

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		// Disposer

		public void Dispose()
		{
			foreach (var item in Items)
				item.Dispose();
		}
	}
}
