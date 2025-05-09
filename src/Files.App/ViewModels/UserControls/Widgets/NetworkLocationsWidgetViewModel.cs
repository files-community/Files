// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="NetworkLocationsWidget"/>.
	/// </summary>
	public sealed partial class NetworkLocationsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Properties

		public ObservableCollection<WidgetDriveCardItem> Items { get; } = [];

		public string WidgetName => nameof(NetworkLocationsWidget);
		public string AutomationProperties => Strings.NetworkLocations.GetLocalizedResource();
		public string WidgetHeader => Strings.NetworkLocations.GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget;
		public bool ShowMenuFlyout => true;
		public MenuFlyoutItem? MenuFlyoutItem => new()
		{
			Icon = new FontIcon() { Glyph = "\uE710" },
			Text = Strings.DrivesWidgetOptionsFlyoutMapNetDriveMenuItem_Text.GetLocalizedResource(),
			Command = MapNetworkDriveCommand
		};

		private bool _IsNoNetworkLocations;
		public bool IsNoNetworkLocations
		{
			get => _IsNoNetworkLocations;
			private set => SetProperty(ref _IsNoNetworkLocations, value);
		}

		// Commands

		private ICommand MapNetworkDriveCommand { get; } = null!;
		private ICommand DisconnectNetworkDriveCommand { get; } = null!;

		// Constructor

		public NetworkLocationsWidgetViewModel()
		{
			Items.CollectionChanged += Items_CollectionChanged;

			PinToSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToSidebarCommand);
			UnpinFromSidebarCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromSidebarCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteOpenPropertiesCommand);
			DisconnectNetworkDriveCommand = new RelayCommand<WidgetDriveCardItem>(ExecuteDisconnectNetworkDriveCommand);
			MapNetworkDriveCommand = new AsyncRelayCommand(ExecuteMapNetworkDriveCommand);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				foreach (var item in Items)
					item.Dispose();

				Items.Clear();

				await foreach (IWindowsFolder folder in HomePageContext.HomeFolder.GetNetworkLocationsAsync(default))
				{
					folder.TryGetShellLink(out ComPtr<IShellLinkW> pShellLink);
					string linkTargetPath = string.Empty;

					if (pShellLink.IsNull)
					{
						folder.GetPropertyValue("System.Link.TargetParsingPath", out linkTargetPath);
					}
					else
					{
						unsafe
						{
							char* pszTargetPath = (char*)NativeMemory.Alloc(1024);
							pShellLink.Get()->GetPath(pszTargetPath, 1024, null, (uint)SLGP_FLAGS.SLGP_RAWPATH);
							linkTargetPath = new(pszTargetPath);

							NativeMemory.Free(pszTargetPath);
						}
					}

					Items.Insert(
						Items.Count,
						new()
						{
							Item = folder,
							Text = folder.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI),
							Path = linkTargetPath,
							DriveType = SystemIO.DriveType.Network
						});
				}

				IsNoNetworkLocations = !Items.Any();
			});
		}

		public async Task NavigateToPath(string path)
		{
			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed)
			{
				await NavigationHelpers.OpenPathInNewTab(path);
				return;
			}

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage!.InstanceViewModel.FolderSettings.GetLayoutType(path),
				new() { NavPathParam = path });
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewTabFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab && CommandManager.OpenInNewTabFromHomeAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewWindowFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow && CommandManager.OpenInNewWindowFromHomeAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.OpenInNewPaneFromHomeAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && CommandManager.OpenInNewPaneFromHomeAction.IsExecutable
				}.Build(),
				new()
				{
					Text = Strings.PinFolderToSidebar.GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel() { ThemedIconStyle = "App.ThemedIcons.FavoritePin" },
					Command = PinToSidebarCommand,
					CommandParameter = item,
					ShowItem = !isPinned
				},
				new()
				{
					Text = Strings.UnpinFolderFromSidebar.GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel() { ThemedIconStyle = "App.ThemedIcons.FavoritePinRemove" },
					Command = UnpinFromSidebarCommand,
					CommandParameter = item,
					ShowItem = isPinned
				},
				new ContextMenuFlyoutItemViewModelBuilder(CommandManager.FormatDriveFromHome).Build(),
				new()
				{
					Text = Strings.Properties.GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel() { ThemedIconStyle = "App.ThemedIcons.Properties" },
					Command = OpenPropertiesCommand,
					CommandParameter = item
				},
				new()
				{
					Text = Strings.TurnOnBitLocker.GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsEnabled = false
				},
				new()
				{
					Text = Strings.ManageBitLocker.GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsEnabled = false
				},
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

		public void DisableWidget()
		{
			UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget = false;
		}

		// Command methods

		private Task ExecuteMapNetworkDriveCommand()
		{
			return NetworkService.OpenMapNetworkDriveDialogAsync();
		}

		private void ExecuteOpenPropertiesCommand(WidgetDriveCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked || item is null)
				return;

			var flyout = HomePageContext.ItemContextFlyoutMenu;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;
				FilePropertiesHelpers.OpenPropertiesWindow(item.Item, ContentPageContext.ShellPage!);
			};

			flyout!.Closed += flyoutClosed;
		}

		private void ExecuteDisconnectNetworkDriveCommand(WidgetDriveCardItem? item)
		{
			if (item is null)
				return;

			item.Item.TryDisconnectNetworkDrive();
		}

		// Event methods

		private async void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action is NotifyCollectionChangedAction.Add)
			{
				foreach (WidgetDriveCardItem cardItem in e.NewItems!)
					await cardItem.LoadCardThumbnailAsync();
			}
		}

		// Disposer

		public void Dispose()
		{
			foreach (var item in Items)
				item.Dispose();
		}
	}
}
