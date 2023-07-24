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

		private bool IsInPointerPressed = false;

		private readonly DispatcherQueueTimer dragOverSectionTimer, dragOverItemTimer;

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


		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async void SideBar_ItemInvoked(object sender, object item)
		{
			if (item is not INavigationControlItem navigationControlItem) return;
			var navigationPath = item as string;

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if ((IsInPointerPressed || ctrlPressed) && navigationPath is not null)
			{
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
				return;
			}
			SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(navigationControlItem));
		}

		private void SideBarHost_Loaded(object sender, RoutedEventArgs e)
		{
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

		private void SideBarHost_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (args.NewSize.Width < 650)
			{
				DisplayMode = SideBarDisplayMode.Minimal;
			}
			else if (args.NewSize.Width < 1300)
			{
				DisplayMode = SideBarDisplayMode.Compact;
			}
			else
			{
				DisplayMode = SideBarDisplayMode.Expanded;
			}
		}

		private void TogglePaneButton_Click(object sender, RoutedEventArgs e)
		{
			if(DisplayMode == SideBarDisplayMode.Minimal)
			{
				IsPaneOpen = !IsPaneOpen;
			}
        }

        private async void SideBar_ItemDropped(object sender, ItemDroppedEventArgs e)
		{
			if (e.DropTarget is not LocationItem locationItem) return;

			if (FilesystemHelpers.HasDraggedStorageItems(e.DroppedItem))
			{
				var deferral = e.RawEvent.GetDeferral();
				if (string.IsNullOrEmpty(locationItem.Path) && isDropOnProcess && SectionType.Favorites.Equals(locationItem.Section)) // Pin to Favorites section
				{
					var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DroppedItem);
					foreach (var item in storageItems)
					{
						if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
							QuickAccessService.PinToSidebar(item.Path);
					}
				}
				else
				{
					var signal = new AsyncManualResetEvent();
					SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs()
					{
						Package = e.DroppedItem,
						ItemPath = locationItem.Path,
						AcceptedOperation = e.RawEvent.AcceptedOperation,
						SignalEvent = signal
					});
					await signal.WaitAsync();
				}
				deferral.Complete();
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
		public INavigationControlItem InvokedItem { get; set; }

		public SidebarItemInvokedEventArgs(INavigationControlItem item)
		{
			InvokedItem = item;
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
