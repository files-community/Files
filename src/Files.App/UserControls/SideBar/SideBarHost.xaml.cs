// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.UserControls.SideBar
{
	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SideBarHost : UserControl, INotifyPropertyChanged
	{
		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();

		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public delegate void SidebarItemInvokedEventHandler(object sender, SidebarItemInvokedEventArgs e);

		public event SidebarItemInvokedEventHandler SidebarItemInvoked;

		public delegate void SidebarItemNewPaneInvokedEventHandler(object sender, SidebarItemNewPaneInvokedEventArgs e);

		public event SidebarItemNewPaneInvokedEventHandler SidebarItemNewPaneInvoked;

		public delegate void SidebarItemPropertiesInvokedEventHandler(object sender, SidebarItemPropertiesInvokedEventArgs e);

		public event SidebarItemPropertiesInvokedEventHandler SidebarItemPropertiesInvoked;

		public delegate void SidebarItemDroppedEventHandler(object sender, SidebarItemDroppedEventArgs e);

		public event SidebarItemDroppedEventHandler SidebarItemDropped;

		private INavigationControlItem rightClickedItem;

		private object? dragOverSection, dragOverItem = null;

		private bool isDropOnProcess = false;

		private double originalSize = 0;

		private bool lockFlag = false;

		public SidebarPinnedModel SidebarPinnedModel => App.QuickAccessManager.Model;

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(SidebarViewModel), typeof(SidebarControl), new PropertyMetadata(null));

		public static readonly DependencyProperty SelectedSidebarItemProperty = DependencyProperty.Register(nameof(SelectedSidebarItem), typeof(INavigationControlItem), typeof(SidebarControl), new PropertyMetadata(null));

		public INavigationControlItem SelectedSidebarItem
		{
			get => (INavigationControlItem)GetValue(SelectedSidebarItemProperty);
			set
			{
				if (IsLoaded)
					SetValue(SelectedSidebarItemProperty, value);
			}
		}

		public readonly ICommand CreateLibraryCommand = new AsyncRelayCommand(LibraryManager.ShowCreateNewLibraryDialog);

		public readonly ICommand RestoreLibrariesCommand = new AsyncRelayCommand(LibraryManager.ShowRestoreDefaultLibrariesDialog);

		private ICommand HideSectionCommand { get; }

		private ICommand PinItemCommand { get; }

		private ICommand UnpinItemCommand { get; }

		private ICommand OpenInNewTabCommand { get; }

		private ICommand OpenInNewWindowCommand { get; }

		private ICommand OpenInNewPaneCommand { get; }

		private ICommand EjectDeviceCommand { get; }

		private ICommand FormatDriveCommand { get; }

		private ICommand OpenPropertiesCommand { get; }

		private ICommand ReorderItemsCommand { get; }

		private bool IsInPointerPressed = false;

		private readonly DispatcherQueueTimer dragOverSectionTimer, dragOverItemTimer;

		public SideBarHost()
		{
			InitializeComponent();

			dragOverSectionTimer = DispatcherQueue.CreateTimer();
			dragOverItemTimer = DispatcherQueue.CreateTimer();

			HideSectionCommand = new RelayCommand(HideSection);
			UnpinItemCommand = new RelayCommand(UnpinItem);
			PinItemCommand = new RelayCommand(PinItem);
			OpenInNewTabCommand = new AsyncRelayCommand(OpenInNewTab);
			OpenInNewWindowCommand = new AsyncRelayCommand(OpenInNewWindow);
			OpenInNewPaneCommand = new AsyncRelayCommand(OpenInNewPane);
			EjectDeviceCommand = new AsyncRelayCommand(EjectDevice);
			FormatDriveCommand = new RelayCommand(FormatDrive);
			OpenPropertiesCommand = new RelayCommand<CommandBarFlyout>(OpenProperties);
			ReorderItemsCommand = new AsyncRelayCommand(ReorderItems);
		}

		public SidebarViewModel ViewModel
		{
			get => (SidebarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		private bool canOpenInNewPane;

		public bool CanOpenInNewPane
		{
			get => canOpenInNewPane;
			set
			{
				if (value != canOpenInNewPane)
				{
					canOpenInNewPane = value;
					NotifyPropertyChanged(nameof(CanOpenInNewPane));
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async Task OpenInNewPane()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			SidebarItemNewPaneInvoked?.Invoke(this, new SidebarItemNewPaneInvokedEventArgs(rightClickedItem));
		}

		private async Task OpenInNewTab()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewTab(rightClickedItem.Path);
		}

		private async Task OpenInNewWindow()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewWindowAsync(rightClickedItem.Path);
		}

		private void PinItem()
		{
			if (rightClickedItem is DriveItem)
				_ = QuickAccessService.PinToSidebar(new[] { rightClickedItem.Path });
		}
		private void UnpinItem()
		{
			if (rightClickedItem.Section == SectionType.Favorites || rightClickedItem is DriveItem)
				_ = QuickAccessService.UnpinFromSidebar(rightClickedItem.Path);
		}

		private void HideSection()
		{
			switch (rightClickedItem.Section)
			{
				case SectionType.Favorites:
					userSettingsService.GeneralSettingsService.ShowFavoritesSection = false;
					break;
				case SectionType.Library:
					userSettingsService.GeneralSettingsService.ShowLibrarySection = false;
					break;
				case SectionType.CloudDrives:
					userSettingsService.GeneralSettingsService.ShowCloudDrivesSection = false;
					break;
				case SectionType.Drives:
					userSettingsService.GeneralSettingsService.ShowDrivesSection = false;
					break;
				case SectionType.Network:
					userSettingsService.GeneralSettingsService.ShowNetworkDrivesSection = false;
					break;
				case SectionType.WSL:
					userSettingsService.GeneralSettingsService.ShowWslSection = false;
					break;
				case SectionType.FileTag:
					userSettingsService.GeneralSettingsService.ShowFileTagsSection = false;
					break;
			}
		}

		private async Task ReorderItems()
		{
			var dialog = new ReorderSidebarItemsDialogViewModel();
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var result = await dialogService.ShowDialogAsync(dialog);
		}

		private void OpenProperties(CommandBarFlyout menu)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = (s, e) =>
			{
				menu.Closed -= flyoutClosed;
				SidebarItemPropertiesInvoked?.Invoke(this, new SidebarItemPropertiesInvokedEventArgs(rightClickedItem));
			};
			menu.Closed += flyoutClosed;
		}

		private async Task EjectDevice()
		{
			var result = await DriveHelpers.EjectDeviceAsync(rightClickedItem.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(result);
		}

		private void FormatDrive()
		{
			Win32API.OpenFormatDriveDialog(rightClickedItem.Path);
		}

		private async void SideBar_ItemInvoked(object sender, object item)
		{
			var navigationPath = item as string;

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if ((IsInPointerPressed || ctrlPressed) && navigationPath is not null)
			{
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
				return;
			}
		}

		private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
		{
		}

		private List<ContextMenuFlyoutItemViewModel> GetLocationItemMenuItems(INavigationControlItem item, CommandBarFlyout menu)
		{
			var options = item.MenuOptions;

			var favoriteModel = App.QuickAccessManager.Model;
			var favoriteIndex = favoriteModel.IndexOfItem(item);
			var favoriteCount = favoriteModel.FavoriteItems.Count;

			var isFavoriteItem = item.Section is SectionType.Favorites && favoriteIndex is not -1;
			var showMoveItemUp = isFavoriteItem && favoriteIndex > 0;
			var showMoveItemDown = isFavoriteItem && favoriteIndex < favoriteCount - 1;

			var isDriveItem = item is DriveItem;
			var isDriveItemPinned = isDriveItem && ((DriveItem)item).IsPinned;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarCreateNewLibrary/Text".GetLocalizedResource(),
					Glyph = "\uE710",
					Command = CreateLibraryCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarRestoreLibraries/Text".GetLocalizedResource(),
					Glyph = "\uE10E",
					Command = RestoreLibrariesCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModelBuilder(commands.EmptyRecycleBin)
				{
					IsVisible = options.ShowEmptyRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.RestoreAllRecycleBin)
				{
					IsVisible = options.ShowEmptyRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					ShowItem = options.IsLocationItem && userSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					ShowItem = options.IsLocationItem && userSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					ShowItem = options.IsLocationItem && userSettingsService.GeneralSettingsService.ShowOpenInNewPane
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinItemCommand,
					ShowItem = isDriveItem && !isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinItemCommand,
					ShowItem = options.ShowUnpinItem || isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ReorderSidebarItemsDialogText".GetLocalizedResource(),
					Glyph = "\uE8D8",
					Command = ReorderItemsCommand,
					ShowItem = isFavoriteItem || item.Section is SectionType.Favorites
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = string.Format("SideBarHideSectionFromSideBar/Text".GetLocalizedResource(), rightClickedItem.Text),
					Glyph = "\uE77A",
					Command = HideSectionCommand,
					ShowItem = options.ShowHideSection
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarEjectDevice/Text".GetLocalizedResource(),
					Command = EjectDeviceCommand,
					ShowItem = options.ShowEjectDevice
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "FormatDriveText".GetLocalizedResource(),
					Command = FormatDriveCommand,
					CommandParameter = item,
					ShowItem = options.ShowFormatDrive
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = menu,
					ShowItem = options.ShowProperties
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					IsHidden = !options.ShowShellItems,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
					IsHidden = !options.ShowShellItems,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		private void SideBar_ItemContextInvoked(object sender, ItemContextInvokedArgs args)
		{
			if (args.Item is not INavigationControlItem item || sender is not FrameworkElement sidebarItem)
			{
				return;
			}
			rightClickedItem = item;

			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			var menuItems = GetLocationItemMenuItems(item, itemContextMenuFlyout);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
								.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(sidebarItem, new FlyoutShowOptions { Position = args.Position });

			if (item.MenuOptions.ShowShellItems)
				_ = ShellContextmenuHelper.LoadShellMenuItems(rightClickedItem.Path, itemContextMenuFlyout, item.MenuOptions);
		}

		private void SideBar_DisplayModeChanged(object sender, SideBarDisplayMode e)
		{
			if (e == SideBarDisplayMode.Minimal)
			{
			}
		}

		private void SideBar_Loaded(object sender, RoutedEventArgs e)
		{
			(this.FindDescendant("TabContentBorder") as Border)!.Child = TabContent;
		}
	}

	public class SidebarItemDroppedEventArgs : EventArgs
	{
		public DataPackageView Package { get; set; }
		public string ItemPath { get; set; }
		public DataPackageOperation AcceptedOperation { get; set; }
		public AsyncManualResetEvent SignalEvent { get; set; }
	}

	public class SidebarItemInvokedEventArgs : EventArgs
	{
		public NavigationViewItemBase InvokedItemContainer { get; set; }

		public SidebarItemInvokedEventArgs(NavigationViewItemBase ItemContainer)
		{
			InvokedItemContainer = ItemContainer;
		}
	}

	public class SidebarItemPropertiesInvokedEventArgs : EventArgs
	{
		public object InvokedItemDataContext { get; set; }

		public SidebarItemPropertiesInvokedEventArgs(object invokedItemDataContext)
		{
			InvokedItemDataContext = invokedItemDataContext;
		}
	}

	public class SidebarItemNewPaneInvokedEventArgs : EventArgs
	{
		public object InvokedItemDataContext { get; set; }

		public SidebarItemNewPaneInvokedEventArgs(object invokedItemDataContext)
		{
			InvokedItemDataContext = invokedItemDataContext;
		}
	}

	public class NavItemDataTemplateSelector : DataTemplateSelector
	{
		public DataTemplate LocationNavItemTemplate { get; set; }
		public DataTemplate DriveNavItemTemplate { get; set; }
		public DataTemplate LinuxNavItemTemplate { get; set; }
		public DataTemplate FileTagNavItemTemplate { get; set; }
		public DataTemplate HeaderNavItemTemplate { get; set; }

		protected override DataTemplate? SelectTemplateCore(object item)
		{
			if (item is null || item is not INavigationControlItem navControlItem)
				return null;

			return navControlItem.ItemType switch
			{
				NavigationControlItemType.Location => LocationNavItemTemplate,
				NavigationControlItemType.Drive => DriveNavItemTemplate,
				NavigationControlItemType.CloudDrive => DriveNavItemTemplate,
				NavigationControlItemType.LinuxDistro => LinuxNavItemTemplate,
				NavigationControlItemType.FileTag => FileTagNavItemTemplate,
				_ => null
			};
		}
	}
}
