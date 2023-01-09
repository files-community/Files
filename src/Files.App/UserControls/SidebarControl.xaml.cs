using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels;
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
		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public delegate void SidebarItemInvokedEventHandler(object sender, SidebarItemInvokedEventArgs e);

		public event SidebarItemInvokedEventHandler SidebarItemInvoked;

		public delegate void SidebarItemNewPaneInvokedEventHandler(object sender, SidebarItemNewPaneInvokedEventArgs e);

		public event SidebarItemNewPaneInvokedEventHandler SidebarItemNewPaneInvoked;

		public delegate void SidebarItemPropertiesInvokedEventHandler(object sender, SidebarItemPropertiesInvokedEventArgs e);

		public event SidebarItemPropertiesInvokedEventHandler SidebarItemPropertiesInvoked;

		public delegate void SidebarItemDroppedEventHandler(object sender, SidebarItemDroppedEventArgs e);

		public event SidebarItemDroppedEventHandler SidebarItemDropped;

		private INavigationControlItem rightClickedItem;

		private object dragOverSection, dragOverItem = null;

		private bool isDropOnProcess = false;

		/// <summary>
		/// true if the user is currently resizing the sidebar
		/// </summary>
		private bool dragging;

		private double originalSize = 0;

		private bool lockFlag = false;

		public SidebarPinnedModel SidebarPinnedModel => App.SidebarPinnedController.Model;

		public static readonly DependencyProperty EmptyRecycleBinCommandProperty = DependencyProperty.Register(nameof(EmptyRecycleBinCommand), typeof(ICommand), typeof(SidebarControl), new PropertyMetadata(null));

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(SidebarViewModel), typeof(SidebarControl), new PropertyMetadata(null));

		public static readonly DependencyProperty SelectedSidebarItemProperty = DependencyProperty.Register(nameof(SelectedSidebarItem), typeof(INavigationControlItem), typeof(SidebarControl), new PropertyMetadata(null));

		public INavigationControlItem SelectedSidebarItem
		{
			get => (INavigationControlItem)GetValue(SelectedSidebarItemProperty);
			set
			{
				if (this.IsLoaded)
				{
					SetValue(SelectedSidebarItemProperty, value);
				}
			}
		}

		public static readonly DependencyProperty TabContentProperty = DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SidebarControl), new PropertyMetadata(null));

		public UIElement TabContent
		{
			get => (UIElement)GetValue(TabContentProperty);
			set => SetValue(TabContentProperty, value);
		}

		public ICommand EmptyRecycleBinCommand
		{
			get => (ICommand)GetValue(EmptyRecycleBinCommandProperty);
			set => SetValue(EmptyRecycleBinCommandProperty, value);
		}

		public readonly ICommand CreateLibraryCommand = new RelayCommand(LibraryManager.ShowCreateNewLibraryDialog);

		public readonly ICommand RestoreLibrariesCommand = new RelayCommand(LibraryManager.ShowRestoreDefaultLibrariesDialog);

		private ICommand HideSectionCommand { get; }

		private ICommand PinItemCommand { get; }

		private ICommand UnpinItemCommand { get; }

		private ICommand MoveItemToTopCommand { get; }

		private ICommand MoveItemUpCommand { get; }

		private ICommand MoveItemDownCommand { get; }

		private ICommand MoveItemToBottomCommand { get; }

		private ICommand OpenInNewTabCommand { get; }

		private ICommand OpenInNewWindowCommand { get; }

		private ICommand OpenInNewPaneCommand { get; }

		private ICommand EjectDeviceCommand { get; }

		private ICommand OpenPropertiesCommand { get; }

		private bool IsInPointerPressed = false;

		private DispatcherQueueTimer dragOverSectionTimer, dragOverItemTimer;

		public SidebarControl()
		{
			InitializeComponent();

			dragOverSectionTimer = DispatcherQueue.CreateTimer();
			dragOverItemTimer = DispatcherQueue.CreateTimer();

			HideSectionCommand = new RelayCommand(HideSection);
			UnpinItemCommand = new RelayCommand(UnpinItem);
			PinItemCommand = new RelayCommand(PinItem);
			MoveItemToTopCommand = new RelayCommand(MoveItemToTop);
			MoveItemUpCommand = new RelayCommand(MoveItemUp);
			MoveItemDownCommand = new RelayCommand(MoveItemDown);
			MoveItemToBottomCommand = new RelayCommand(MoveItemToBottom);
			OpenInNewTabCommand = new RelayCommand(OpenInNewTab);
			OpenInNewWindowCommand = new RelayCommand(OpenInNewWindow);
			OpenInNewPaneCommand = new RelayCommand(OpenInNewPane);
			EjectDeviceCommand = new RelayCommand(EjectDevice);
			OpenPropertiesCommand = new RelayCommand<CommandBarFlyout>(OpenProperties);
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

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private List<ContextMenuFlyoutItemViewModel> GetLocationItemMenuItems(INavigationControlItem item, CommandBarFlyout menu)
		{
			var options = item.MenuOptions;

			var favoriteModel = App.SidebarPinnedController.Model;
			var favoriteIndex = favoriteModel.IndexOfItem(item);
			var favoriteCount = favoriteModel.FavoriteItems.Count;

			var isFavoriteItem = item.Section is SectionType.Favorites && favoriteIndex is not -1;
			var showMoveItemUp = isFavoriteItem && favoriteIndex > 0;
			var showMoveItemDown = isFavoriteItem && favoriteIndex < favoriteCount - 1;

			var isDriveItem = item is DriveItem;
			var isDriveItemPinned = isDriveItem && (item as DriveItem).IsPinned;

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
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutEmptyRecycleBin/Text".GetLocalizedResource(),
					Glyph = "\uEF88",
					GlyphFontFamilyName = "RecycleBinIcons",
					Command = EmptyRecycleBinCommand,
					ShowItem = options.ShowEmptyRecycleBin,
					IsEnabled = false,
					ID = "EmptyRecycleBin",
					Tag = "EmptyRecycleBin",
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewPane/Text".GetLocalizedResource(),
					Glyph = "\uF117",
					GlyphFontFamilyName = "CustomGlyph",
					Command = OpenInNewPaneCommand,
					ShowItem = options.IsLocationItem && CanOpenInNewPane
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewTab/Text".GetLocalizedResource(),
					Glyph = "\uF113",
					GlyphFontFamilyName = "CustomGlyph",
					Command = OpenInNewTabCommand,
					ShowItem = options.IsLocationItem
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarOpenInNewWindow/Text".GetLocalizedResource(),
					Glyph = "\uE737",
					Command = OpenInNewWindowCommand,
					ShowItem = options.IsLocationItem
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarFavoritesMoveToTop".GetLocalizedResource(),
					Glyph = "\uE11C",
					Command = MoveItemToTopCommand,
					ShowItem = showMoveItemUp
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarFavoritesMoveOneUp".GetLocalizedResource(),
					Glyph = "\uE70E",
					Command = MoveItemUpCommand,
					ShowItem = showMoveItemUp
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarFavoritesMoveOneDown".GetLocalizedResource(),
					Glyph = "\uE70D",
					Command = MoveItemDownCommand,
					ShowItem = showMoveItemDown
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarFavoritesMoveToBottom".GetLocalizedResource(),
					Glyph = "\uE118",
					Command = MoveItemToBottomCommand,
					ShowItem = showMoveItemDown
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE840",
					Command = PinItemCommand,
					ShowItem = isDriveItem && !isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarUnpinFromFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE77A",
					Command = UnpinItemCommand,
					ShowItem = options.ShowUnpinItem || isDriveItemPinned
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
					Glyph = "\uF10B",
					GlyphFontFamilyName = "CustomGlyph",
					Command = EjectDeviceCommand,
					ShowItem = options.ShowEjectDevice
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutPropertiesFolder/Text".GetLocalizedResource(),
					Glyph = "\uE946",
					Command = OpenPropertiesCommand,
					CommandParameter = menu,
					ShowItem = options.ShowProperties
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ContextMenuMoreItemsLabel".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsHidden = true,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		private void HideSection()
		{
			switch (rightClickedItem.Section)
			{
				case SectionType.Favorites:
					UserSettingsService.PreferencesSettingsService.ShowFavoritesSection = false;
					break;
				case SectionType.Library:
					UserSettingsService.PreferencesSettingsService.ShowLibrarySection = false;
					break;
				case SectionType.CloudDrives:
					UserSettingsService.PreferencesSettingsService.ShowCloudDrivesSection = false;
					break;
				case SectionType.Drives:
					UserSettingsService.PreferencesSettingsService.ShowDrivesSection = false;
					break;
				case SectionType.Network:
					UserSettingsService.PreferencesSettingsService.ShowNetworkDrivesSection = false;
					break;
				case SectionType.WSL:
					UserSettingsService.PreferencesSettingsService.ShowWslSection = false;
					break;
				case SectionType.FileTag:
					UserSettingsService.PreferencesSettingsService.ShowFileTagsSection = false;
					break;
			}
		}

		private async void OpenInNewPane()
		{
			if (await CheckEmptyDrive(rightClickedItem.Path))
				return;

			SidebarItemNewPaneInvoked?.Invoke(this, new SidebarItemNewPaneInvokedEventArgs(rightClickedItem));
		}

		private async void OpenInNewTab()
		{
			if (await CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewTab(rightClickedItem.Path);
		}

		private async void OpenInNewWindow()
		{
			if (await CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewWindowAsync(rightClickedItem.Path);
		}

		private void PinItem()
		{
			if (rightClickedItem is DriveItem)
				App.SidebarPinnedController.Model.AddItem(rightClickedItem.Path);
		}
		private void UnpinItem()
		{
			if (rightClickedItem.Section == SectionType.Favorites || rightClickedItem is DriveItem)
				App.SidebarPinnedController.Model.RemoveItem(rightClickedItem.Path);
		}

		private void MoveItemToTop()
		{
			MoveItemToNewIndex(0);
		}

		private void MoveItemUp()
		{
			MoveItemToNewIndex(App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem) - 1);
		}

		private void MoveItemDown()
		{
			MoveItemToNewIndex(App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem) + 1);
		}

		private void MoveItemToBottom()
		{
			MoveItemToNewIndex(App.SidebarPinnedController.Model.FavoriteItems.Count - 1);
		}

		private void MoveItemToNewIndex(int newIndex)
		{
			if (rightClickedItem.Section != SectionType.Favorites) 
				return;

			var isSelectedSidebarItem = SelectedSidebarItem == rightClickedItem;

			var oldIndex = App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem);
			App.SidebarPinnedController.Model.MoveItem(rightClickedItem, oldIndex, newIndex);

			if (isSelectedSidebarItem)
				SetValue(SelectedSidebarItemProperty, rightClickedItem);
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

		private async void EjectDevice()
		{
			var result = await DriveHelpers.EjectDeviceAsync(rightClickedItem.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(result);
		}

		private async void Sidebar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			if (IsInPointerPressed || args.InvokedItem is null || args.InvokedItemContainer is null)
			{
				IsInPointerPressed = false;
				return;
			}

			var navigationPath = args.InvokedItemContainer.Tag?.ToString();

			if (await CheckEmptyDrive(navigationPath))
				return;

			var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
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

			if (!properties.IsMiddleButtonPressed || context is not INavigationControlItem item || item.Path is null)
				return;

			if (await CheckEmptyDrive(item.Path))
				return;

			IsInPointerPressed = true;
			e.Handled = true;
			await NavigationHelpers.OpenPathInNewTab(item.Path);
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
			var sidebarItem = sender as NavigationViewItem;
			var item = sidebarItem.DataContext as INavigationControlItem;

			rightClickedItem = item;

			var menuItems = GetLocationItemMenuItems(item, itemContextMenuFlyout);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			if (!UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
				secondaryElements.OfType<FrameworkElement>()
								 .ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width if the overflow menu setting is disabled

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(sidebarItem, new FlyoutShowOptions { Position = e.GetPosition(sidebarItem) });

			if (item.MenuOptions.ShowShellItems)
				LoadShellMenuItems(itemContextMenuFlyout, item.MenuOptions);

			e.Handled = true;
		}

		private void NavigationViewItem_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			if ((sender as NavigationViewItem).DataContext is not LocationItem locationItem)
				return;

			// Adding the original Location item dragged to the DragEvents data view
			var navItem = (sender as NavigationViewItem);
			args.Data.Properties.Add("sourceLocationItem", navItem);
		}

		private void NavigationViewItem_DragEnter(object sender, DragEventArgs e)
		{
			VisualStateManager.GoToState(sender as NavigationViewItem, "DragEnter", false);

			if ((sender as NavigationViewItem).DataContext is not INavigationControlItem iNavItem) 
				return;

			if (string.IsNullOrEmpty(iNavItem.Path))
			{
				HandleDragOverSection(sender);
			}
			else
			{
				HandleDragOverItem(sender);
			}
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
			VisualStateManager.GoToState(sender as NavigationViewItem, "DragLeave", false);

			isDropOnProcess = false;

			if ((sender as NavigationViewItem).DataContext is not INavigationControlItem)
				return;

			if (sender == dragOverItem)
				dragOverItem = null; // Reset dragged over item

			if (sender == dragOverSection)
				dragOverSection = null; // Reset dragged over item
		}

		private async void NavigationViewLocationItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as NavigationViewItem).DataContext is not LocationItem locationItem)
				return;

			var deferral = e.GetDeferral();

			if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.Handled = true;
				isDropOnProcess = true;

				var handledByFtp = await FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
				var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);
				var hasStorageItems = storageItems.Any();

				if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Favorites.Equals(locationItem.Section) && hasStorageItems)
				{
					var haveFoldersToPin = storageItems.Any(item => item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path));

					if (!haveFoldersToPin)
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						var captionText = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource();
						CompleteDragEventArgs(e, captionText, DataPackageOperation.Move);
					}
				}
				else if (string.IsNullOrEmpty(locationItem.Path) ||
					(hasStorageItems && storageItems.AreItemsAlreadyInFolder(locationItem.Path)) ||
					locationItem.Path.StartsWith("Home".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else if (handledByFtp)
				{
					if (locationItem.Path.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						var captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						CompleteDragEventArgs(e, captionText, DataPackageOperation.Copy);
					}
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
					else if (storageItems.AreItemsInSameDrive(locationItem.Path) || locationItem.IsDefaultLocation)
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
			else if ((e.DataView.Properties["sourceLocationItem"] as NavigationViewItem)?.DataContext is LocationItem sourceLocationItem)
			{
				// else if the drag over event is called over a location item
				NavigationViewLocationItem_DragOver_SetCaptions(locationItem, sourceLocationItem, e);
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

		private void NavigationViewLocationItem_DragOver_SetCaptions(LocationItem senderLocationItem, LocationItem sourceLocationItem, DragEventArgs e)
		{
			// If the location item is the same as the original dragged item
			if (sourceLocationItem.Equals(senderLocationItem))
			{
				e.AcceptedOperation = DataPackageOperation.None;
				e.DragUIOverride.IsCaptionVisible = false;
			}
			else
			{
				CompleteDragEventArgs(e, "PinToSidebarByDraggingCaptionText".GetLocalizedResource(), DataPackageOperation.Move);
			}
		}

		private async void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
				return;

			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if ((sender as NavigationViewItem).DataContext is not LocationItem locationItem)
				return;

			// If the dropped item is a folder or file from a file system
			if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				VisualStateManager.GoToState(sender as NavigationViewItem, "Drop", false);

				var deferral = e.GetDeferral();

				if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Favorites.Equals(locationItem.Section) && isDropOnProcess) // Pin to Favorites section
				{
					var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);
					foreach (var item in storageItems)
					{
						if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
							SidebarPinnedModel.AddItem(item.Path);
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
			else if ((e.DataView.Properties["sourceLocationItem"] as NavigationViewItem)?.DataContext is LocationItem sourceLocationItem)
			{
				SidebarPinnedModel.SwapItems(sourceLocationItem, locationItem);
			}

			await Task.Yield();
			lockFlag = false;
		}

		private async void NavigationViewDriveItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as NavigationViewItem).DataContext is not DriveItem driveItem ||
				!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			var deferral = e.GetDeferral();
			e.Handled = true;

			var handledByFtp = await FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
			var storageItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);
			var hasStorageItems = storageItems.Any();

			if ("DriveCapacityUnknown".GetLocalizedResource().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
				(hasStorageItems && storageItems.AreItemsAlreadyInFolder(driveItem.Path)))
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else if (handledByFtp)
			{
				var captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
				CompleteDragEventArgs(e, captionText, DataPackageOperation.Copy);
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

			if ((sender as NavigationViewItem).DataContext is not DriveItem driveItem)
				return;

			VisualStateManager.GoToState(sender as NavigationViewItem, "Drop", false);

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
			if ((sender as NavigationViewItem).DataContext is not FileTagItem fileTagItem ||
				!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			var deferral = e.GetDeferral();
			e.Handled = true;

			var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
			var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (handledByFtp)
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else if (!storageItems.Any())
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

			if ((sender as NavigationViewItem).DataContext is not FileTagItem fileTagItem)
				return;

			VisualStateManager.GoToState(sender as NavigationViewItem, "Drop", false);

			var deferral = e.GetDeferral();

			var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
			var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (handledByFtp)
				return;

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
			var step = 1;
			var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
			originalSize = IsPaneOpen ? UserSettingsService.AppearanceSettingsService.SidebarWidth : CompactPaneLength;

			if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
				step = 5;

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

			UserSettingsService.AppearanceSettingsService.SidebarWidth = OpenPaneLength;
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

			((Border)sender).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(((Border)sender).FindAscendant<SplitView>(), "ResizerNormal", true);
		}

		private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (DisplayMode != NavigationViewDisplayMode.Expanded)
				return;

			((Border)sender).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(((Border)sender).FindAscendant<SplitView>(), "ResizerPointerOver", true);
		}

		private void SetSize(double val, bool closeImmediatleyOnOversize = false)
		{
			if (IsPaneOpen)
			{
				var newSize = originalSize + val;
				if (newSize <= Constants.UI.MaximumSidebarWidth && newSize >= Constants.UI.MinimumSidebarWidth)
					OpenPaneLength = newSize; // passing a negative value will cause an exception

				if (newSize < Constants.UI.MinimumSidebarWidth &&
				    (Constants.UI.MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatleyOnOversize)) // if the new size is below the minimum, check whether to toggle the pane
					IsPaneOpen = false; // collapse the sidebar
			}
			else
			{
				if (val < Constants.UI.MinimumSidebarWidth - CompactPaneLength &&
				    !closeImmediatleyOnOversize)
					return;

				OpenPaneLength = Constants.UI.MinimumSidebarWidth + (val + CompactPaneLength - Constants.UI.MinimumSidebarWidth); // set open sidebar length to minimum value to keep it smooth
				IsPaneOpen = true;
			}
		}

		private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			((Border)sender).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(((Border)sender).FindAscendant<SplitView>(), "ResizerNormal", true);
			UserSettingsService.AppearanceSettingsService.SidebarWidth = OpenPaneLength;
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

			originalSize = IsPaneOpen ? UserSettingsService.AppearanceSettingsService.SidebarWidth : CompactPaneLength;
			((Border)sender).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(((Border)sender).FindAscendant<SplitView>(), "ResizerPressed", true);
			dragging = true;
		}

		private async Task<bool> CheckEmptyDrive(string drivePath)
		{
			if (drivePath is null)
				return false;

			var matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x => drivePath.StartsWith(x.Path, StringComparison.Ordinal));
			if (matchingDrive is null || matchingDrive.Type != DriveType.CDRom || matchingDrive.MaxSpace != ByteSizeLib.ByteSize.FromBytes(0))
				return false;

			var ejectButton = await DialogDisplayHelper.ShowDialogAsync("InsertDiscDialog/Title".GetLocalizedResource(), string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path), "InsertDiscDialog/OpenDriveButton".GetLocalizedResource(), "Close".GetLocalizedResource());
			if (ejectButton)
			{
				var result = await DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
				await UIHelpers.ShowDeviceEjectResultAsync(result);
			}
			return true;
		}

		private async void LoadShellMenuItems(CommandBarFlyout itemContextMenuFlyout, ContextMenuOptions options)
		{
			try
			{
				if (options.ShowEmptyRecycleBin)
				{
					var emptyRecycleBinItem = itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "EmptyRecycleBin") as AppBarButton;
					if (emptyRecycleBinItem is not null)
					{
						var binHasItems = new RecycleBinHelpers().RecycleBinHasItems();
						emptyRecycleBinItem.IsEnabled = binHasItems;
					}
				}

				if (!options.IsLocationItem)
					return;

				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
				var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(currentInstanceViewModel: null, workingDir: null,
					new List<ListedItem>() { new ListedItem(null) { ItemPath = rightClickedItem.Path } }, shiftPressed: shiftPressed, showOpenMenu: false, default);
				if (!UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
				{
					var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(shellMenuItems);
					if (!secondaryElements.Any())
						return;

					var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);
					var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

					var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
					if (itemsControl is not null)
						secondaryElements.OfType<FrameworkElement>()
										 .ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin); // Set items max width to current menu width (#5555)

					itemContextMenuFlyout.SecondaryCommands.Add(new AppBarSeparator());
					secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
				}
				else
				{
					var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(shellMenuItems);
					if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") is not AppBarButton overflowItem) 
						return;

					overflowItems.ForEach(i => (overflowItem.Flyout as MenuFlyout).Items.Add(i));
					overflowItem.Visibility = overflowItems.Any() ? Visibility.Visible : Visibility.Collapsed;
				}
			}
			catch { }
		}

		public static GridLength GetSidebarCompactSize()
		{
			if (App.Current.Resources.TryGetValue("NavigationViewCompactPaneLength", out object paneLength) &&
			    paneLength is double paneLengthDouble)
				return new GridLength(paneLengthDouble);

			return new GridLength(200);
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
			{
				App.AppSettings.Set(isCollapsed, $"section:{loc.Text.Replace('\\', '_')}");
			}
		}

		private void NavigationView_PaneOpened(NavigationView sender, object args)
		{
			// Restore expanded state when pane is opened
			foreach (var loc in ViewModel.SideBarItems.OfType<LocationItem>().Where(x => x.ChildItems is not null))
			{
				loc.IsExpanded = App.AppSettings.Get(loc.Text == "SidebarFavorites".GetLocalizedResource(), $"section:{loc.Text.Replace('\\', '_')}");
			}
		}

		private void NavigationView_PaneClosed(NavigationView sender, object args)
		{
			// Collapse all sections but do not store the state when pane is closed
			foreach (var loc in ViewModel.SideBarItems.OfType<LocationItem>().Where(x => x.ChildItems is not null))
			{
				loc.IsExpanded = false;
			}
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
