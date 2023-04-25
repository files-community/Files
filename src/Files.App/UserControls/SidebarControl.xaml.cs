// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Files.App.Commands;
using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ServicesImplementation;
using Files.App.Shell;
using Files.App.ViewModels;
using Files.App.ViewModels.Dialogs;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Shared.Extensions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using UWPToWinAppSDKUpgradeHelpers;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.System;
using Windows.UI.Core;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.UserControls
{
	public sealed partial class SidebarControl : NavigationView, INotifyPropertyChanged
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

		/// <summary>
		/// true if the user is currently resizing the sidebar
		/// </summary>
		private bool dragging;

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

		public static readonly DependencyProperty TabContentProperty = DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SidebarControl), new PropertyMetadata(null));

		public UIElement TabContent
		{
			get => (UIElement)GetValue(TabContentProperty);
			set => SetValue(TabContentProperty, value);
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

		public SidebarControl()
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
					ItemType = ItemType.Separator,
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
			_ = await dialogService.ShowDialogAsync(dialog);
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

		private async void Sidebar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (IsInPointerPressed || args.InvokedItem is null || args.InvokedItemContainer is null)
			{
				IsInPointerPressed = false;
				return;
			}

			var navigationPath = args.InvokedItemContainer.Tag?.ToString();

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			if (ctrlPressed && navigationPath is not null)
			{
				await NavigationHelpers.OpenPathInNewTab(navigationPath);
				return;
			}

			SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(args.InvokedItemContainer));
		}

		private async void Sidebar_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;
			var context = (sender as NavigationViewItem)?.DataContext;

			if (!properties.IsMiddleButtonPressed || context is not INavigationControlItem item || await DriveHelpers.CheckEmptyDrive(item?.Path))
				return;

			IsInPointerPressed = true;
			e.Handled = true;
			await NavigationHelpers.OpenPathInNewTab(item?.Path);
		}

		private void PaneRoot_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var contextMenu = FlyoutBase.GetAttachedFlyout(this);
			contextMenu.ShowAt(this, new FlyoutShowOptions() { Position = e.GetPosition(this) });

			e.Handled = true;
		}

		private void NavigationViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var itemContextMenuFlyout = new CommandBarFlyout { Placement = FlyoutPlacementMode.Full };
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			if (sender is not NavigationViewItem sidebarItem ||
				sidebarItem.DataContext is not INavigationControlItem item)
				return;

			rightClickedItem = item;

			var menuItems = GetLocationItemMenuItems(item, itemContextMenuFlyout);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements.OfType<FrameworkElement>()
								.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(sidebarItem, new FlyoutShowOptions { Position = e.GetPosition(sidebarItem) });

			if (item.MenuOptions.ShowShellItems)
				_ = ShellContextmenuHelper.LoadShellMenuItems(rightClickedItem.Path, itemContextMenuFlyout, item.MenuOptions);

			e.Handled = true;
		}

		private void NavigationViewItem_DragEnter(object sender, DragEventArgs e)
		{
			var navView = sender as NavigationViewItem;
			VisualStateManager.GoToState(navView, "DragEnter", false);

			if (navView?.DataContext is not INavigationControlItem iNavItem)
				return;

			if (string.IsNullOrEmpty(iNavItem.Path))
				HandleDragOverSection(sender);
			else
				HandleDragOverItem(sender);
		}

		private void HandleDragOverItem(object sender)
		{
			dragOverItem = sender;
			dragOverItemTimer.Stop();
			dragOverItemTimer.Debounce(() =>
			{
				if (dragOverItem is null)
					return;

				dragOverItemTimer.Stop();
				SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(dragOverItem as NavigationViewItemBase));
				dragOverItem = null;
			}, TimeSpan.FromMilliseconds(1000), false);
		}

		private void HandleDragOverSection(object sender)
		{
			dragOverSection = sender;
			dragOverSectionTimer.Stop();
			dragOverSectionTimer.Debounce(() =>
			{
				if (dragOverSection is null)
					return;

				dragOverSectionTimer.Stop();
				if ((dragOverSection as NavigationViewItem)?.DataContext is LocationItem section)
					section.IsExpanded = true;

				dragOverSection = null;
			}, TimeSpan.FromMilliseconds(1000), false);
		}

		private void NavigationViewItem_DragLeave(object sender, DragEventArgs e)
		{
			var navView = sender as NavigationViewItem;
			VisualStateManager.GoToState(navView, "DragLeave", false);

			isDropOnProcess = false;

			if (navView?.DataContext is not INavigationControlItem)
				return;

			if (sender == dragOverItem)
				dragOverItem = null; // Reset dragged over item

			if (sender == dragOverSection)
				dragOverSection = null; // Reset dragged over item
		}

		private async void NavigationViewLocationItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as NavigationViewItem)?.DataContext is not LocationItem locationItem)
				return;

			var deferral = e.GetDeferral();

			if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.Handled = true;
				isDropOnProcess = true;

				var isPathNull = string.IsNullOrEmpty(locationItem.Path);
				var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);
				var hasStorageItems = storageItems.Any();

				if (isPathNull && hasStorageItems && SectionType.Favorites.Equals(locationItem.Section))
				{
					var haveFoldersToPin = storageItems.Any(item => item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path));

					if (!haveFoldersToPin)
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						var captionText = "PinToFavorites".GetLocalizedResource();
						CompleteDragEventArgs(e, captionText, DataPackageOperation.Move);
					}
				}
				else if (isPathNull ||
					(hasStorageItems && storageItems.AreItemsAlreadyInFolder(locationItem.Path)) ||
					locationItem.Path.StartsWith("Home", StringComparison.OrdinalIgnoreCase))
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else if (hasStorageItems is false)
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else
				{
					string captionText;
					DataPackageOperation operationType;
					if (locationItem.Path.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Move;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
					{
						captionText = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Link;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Move;
					}
					else if (storageItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
						|| ZipStorageFolder.IsZipPath(locationItem.Path))
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					else if (locationItem.IsDefaultLocation || storageItems.AreItemsInSameDrive(locationItem.Path))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Move;
					}
					else
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					CompleteDragEventArgs(e, captionText, operationType);
				}
			}			

			deferral.Complete();
		}

		private DragEventArgs CompleteDragEventArgs(DragEventArgs e, string captionText, DataPackageOperation operationType)
		{
			e.DragUIOverride.IsCaptionVisible = true;
			e.DragUIOverride.Caption = captionText;
			e.AcceptedOperation = operationType;
			return e;
		}

		private async void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
				return;

			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if (sender is not NavigationViewItem navView || navView.DataContext is not LocationItem locationItem)
				return;

			// If the dropped item is a folder or file from a file system
			if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				VisualStateManager.GoToState(navView, "Drop", false);

				var deferral = e.GetDeferral();

				if (string.IsNullOrEmpty(locationItem.Path) && isDropOnProcess && SectionType.Favorites.Equals(locationItem.Section)) // Pin to Favorites section
				{
					var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);
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
						Package = e.DataView,
						ItemPath = locationItem.Path,
						AcceptedOperation = e.AcceptedOperation,
						SignalEvent = signal
					});
					await signal.WaitAsync();
				}

				isDropOnProcess = false;
				deferral.Complete();
			}

			await Task.Yield();
			lockFlag = false;
		}

		private async void NavigationViewDriveItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as NavigationViewItem)?.DataContext is not DriveItem driveItem ||
				!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			var deferral = e.GetDeferral();
			e.Handled = true;

			var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);
			var hasStorageItems = storageItems.Any();

			if ("Unknown".GetLocalizedResource().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
				(hasStorageItems && storageItems.AreItemsAlreadyInFolder(driveItem.Path)))
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else if (!hasStorageItems)
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else
			{
				string captionText;
				DataPackageOperation operationType;
				if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
				{
					captionText = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Link;
				}
				else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
				{
					captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Copy;
				}
				else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
				{
					captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Move;
				}
				else if (storageItems.AreItemsInSameDrive(driveItem.Path))
				{
					captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Move;
				}
				else
				{
					captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Copy;
				}
				CompleteDragEventArgs(e, captionText, operationType);
			}

			deferral.Complete();
		}

		private async void NavigationViewDriveItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
				return;

			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if (sender is not NavigationViewItem navView || navView.DataContext is not DriveItem driveItem)
				return;

			VisualStateManager.GoToState(navView, "Drop", false);

			var deferral = e.GetDeferral();

			var signal = new AsyncManualResetEvent();
			SidebarItemDropped?.Invoke(this, new SidebarItemDroppedEventArgs()
			{
				Package = e.DataView,
				ItemPath = driveItem.Path,
				AcceptedOperation = e.AcceptedOperation,
				SignalEvent = signal
			});
			await signal.WaitAsync();

			deferral.Complete();
			await Task.Yield();
			lockFlag = false;
		}

		private async void NavigationViewFileTagItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as NavigationViewItem)?.DataContext is not FileTagItem fileTagItem ||
				!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			var deferral = e.GetDeferral();
			e.Handled = true;

			var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (!storageItems.Any())
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), fileTagItem.Text);
				e.AcceptedOperation = DataPackageOperation.Link;
			}

			deferral.Complete();
		}

		private async void NavigationViewFileTag_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
				return;

			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if (sender is not NavigationViewItem navItem || navItem.DataContext is not FileTagItem fileTagItem)
				return;

			VisualStateManager.GoToState(navItem, "Drop", false);

			var deferral = e.GetDeferral();

			var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);
			foreach (var item in storageItems.Where(x => !string.IsNullOrEmpty(x.Path)))
			{
				var listedItem = new ListedItem(null)
				{
					ItemPath = item.Path,
					FileFRN = await FileTagsHelper.GetFileFRN(item.Item),
					FileTags = new[] { fileTagItem.FileTag.Uid }
				};
			}

			deferral.Complete();
			await Task.Yield();
			lockFlag = false;
		}

		private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
		{
			(this.FindDescendant("TabContentBorder") as Border)!.Child = TabContent;
		}

		private void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
		{
			IsPaneToggleButtonVisible = args.DisplayMode == NavigationViewDisplayMode.Minimal;
		}

		private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
			var step = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;
			originalSize = IsPaneOpen ? userSettingsService.AppearanceSettingsService.SidebarWidth : CompactPaneLength;

			if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
			{
				IsPaneOpen = !IsPaneOpen;
				return;
			}

			if (IsPaneOpen)
			{
				if (e.Key == VirtualKey.Left)
				{
					SetSize(-step, true);
					e.Handled = true;
				}
				else if (e.Key == VirtualKey.Right)
				{
					SetSize(step, true);
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Right)
			{
				IsPaneOpen = !IsPaneOpen;
				return;
			}

			userSettingsService.AppearanceSettingsService.SidebarWidth = OpenPaneLength;
		}

		private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (DisplayMode == NavigationViewDisplayMode.Expanded)
				SetSize(e.Cumulative.Translation.X);
		}

		private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (dragging)
				return; // keep showing pressed event if currently resizing the sidebar

			var border = (Border)sender;
			border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
		}

		private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (DisplayMode != NavigationViewDisplayMode.Expanded)
				return;

			var border = (Border)sender;
			border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPointerOver", true);
		}

		private void SetSize(double val, bool closeImmediatelyOnOversize = false)
		{
			if (IsPaneOpen)
			{
				var newSize = originalSize + val;
				var isNewSizeGreaterThanMinimum = newSize >= Constants.UI.MinimumSidebarWidth;
				if (newSize <= Constants.UI.MaximumSidebarWidth && isNewSizeGreaterThanMinimum)
					OpenPaneLength = newSize; // passing a negative value will cause an exception

				// if the new size is below the minimum, check whether to toggle the pane collapse the sidebar
				IsPaneOpen = !(!isNewSizeGreaterThanMinimum && (Constants.UI.MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatelyOnOversize));
			}
			else
			{
				if (val < Constants.UI.MinimumSidebarWidth - CompactPaneLength &&
					!closeImmediatelyOnOversize)
					return;

				OpenPaneLength = val + CompactPaneLength; // set open sidebar length to minimum value to keep it smooth
				IsPaneOpen = true;
			}
		}

		private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			var border = (Border)sender;
			border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
			userSettingsService.AppearanceSettingsService.SidebarWidth = OpenPaneLength;
			dragging = false;
		}

		private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			IsPaneOpen = !IsPaneOpen;
		}

		private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			if (DisplayMode != NavigationViewDisplayMode.Expanded)
				return;

			originalSize = IsPaneOpen ? userSettingsService.AppearanceSettingsService.SidebarWidth : CompactPaneLength;
			var border = (Border)sender;
			border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPressed", true);
			dragging = true;
		}

		public static GridLength GetSidebarCompactSize()
		{
			return App.Current.Resources.TryGetValue("NavigationViewCompactPaneLength", out object paneLength) && paneLength is double paneLengthDouble
				? new GridLength(paneLengthDouble)
				: new GridLength(200);
		}

		#region Sidebar sections expanded state management

		private async void NavigationView_Expanding(NavigationView sender, NavigationViewItemExpandingEventArgs args)
		{
			if (args.ExpandingItem is not LocationItem loc || loc.ChildItems is null)
				return;

			await SetNavigationViewCollapse(sender, loc, true);
		}

		private async void NavigationView_Collapsed(NavigationView sender, NavigationViewItemCollapsedEventArgs args)
		{
			if (args.CollapsedItem is not LocationItem loc || loc.ChildItems is null)
				return;

			await SetNavigationViewCollapse(sender, loc, false);
		}

		private static async Task SetNavigationViewCollapse(NavigationView sender, LocationItem loc, bool isCollapsed)
		{
			await Task.Delay(50); // Wait a little so IsPaneOpen tells the truth when in minimal mode
			if (sender.IsPaneOpen) // Don't store expanded state if sidebar pane is closed
				Ioc.Default.GetRequiredService<SettingsViewModel>().Set(isCollapsed, $"section:{loc.Text.Replace('\\', '_')}");
		}

		private void NavigationView_PaneOpened(NavigationView sender, object args)
		{
			// Restore expanded state when pane is opened
			foreach (var loc in ViewModel.SideBarItems.OfType<LocationItem>().Where(x => x.ChildItems is not null))
				loc.IsExpanded = Ioc.Default.GetRequiredService<SettingsViewModel>().Get(loc.Text == "SidebarFavorites".GetLocalizedResource(), $"section:{loc.Text.Replace('\\', '_')}");
		}

		private void NavigationView_PaneClosed(NavigationView sender, object args)
		{
			// Collapse all sections but do not store the state when pane is closed
			foreach (var loc in ViewModel.SideBarItems.OfType<LocationItem>().Where(x => x.ChildItems is not null))
				loc.IsExpanded = false;
		}

		#endregion
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
