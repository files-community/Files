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

		/// <summary>
		/// The Model for the pinned sidebar items
		/// </summary>
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
			this.InitializeComponent();
			this.Loaded += SidebarNavView_Loaded;

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
			ContextMenuOptions options = item.MenuOptions;

			var favoriteModel = App.SidebarPinnedController.Model;
			int favoriteIndex = favoriteModel.IndexOfItem(item);
			int favoriteCount = favoriteModel.FavoriteItems.Count;

			bool isFavoriteItem = item.Section is SectionType.Favorites && favoriteIndex is not -1;
			bool showMoveItemUp = isFavoriteItem && favoriteIndex > 0;
			bool showMoveItemDown = isFavoriteItem && favoriteIndex < favoriteCount - 1;

			bool isDriveItem = item is DriveItem;
			bool isDriveItemPinned = isDriveItem && (item as DriveItem).IsPinned;

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
					UserSettingsService.AppearanceSettingsService.ShowFavoritesSection = false;
					break;
				case SectionType.Library:
					UserSettingsService.AppearanceSettingsService.ShowLibrarySection = false;
					break;
				case SectionType.CloudDrives:
					UserSettingsService.AppearanceSettingsService.ShowCloudDrivesSection = false;
					break;
				case SectionType.Drives:
					UserSettingsService.AppearanceSettingsService.ShowDrivesSection = false;
					break;
				case SectionType.Network:
					UserSettingsService.AppearanceSettingsService.ShowNetworkDrivesSection = false;
					break;
				case SectionType.WSL:
					UserSettingsService.AppearanceSettingsService.ShowWslSection = false;
					break;
				case SectionType.FileTag:
					UserSettingsService.AppearanceSettingsService.ShowFileTagsSection = false;
					break;
			}
		}

		private async void OpenInNewPane()
		{
			if (await CheckEmptyDrive((rightClickedItem as INavigationControlItem)?.Path))
			{
				return;
			}
			SidebarItemNewPaneInvoked?.Invoke(this, new SidebarItemNewPaneInvokedEventArgs(rightClickedItem));
		}

		private async void OpenInNewTab()
		{
			if (await CheckEmptyDrive(rightClickedItem.Path))
			{
				return;
			}
			await NavigationHelpers.OpenPathInNewTab(rightClickedItem.Path);
		}

		private async void OpenInNewWindow()
		{
			if (await CheckEmptyDrive(rightClickedItem.Path))
			{
				return;
			}
			await NavigationHelpers.OpenPathInNewWindowAsync(rightClickedItem.Path);
		}

		private void PinItem()
		{
			if (rightClickedItem is DriveItem)
			{
				App.SidebarPinnedController.Model.AddItem(rightClickedItem.Path);
			}
		}

		private void UnpinItem()
		{
			if (rightClickedItem.Section == SectionType.Favorites || rightClickedItem is DriveItem)
			{
				App.SidebarPinnedController.Model.RemoveItem(rightClickedItem.Path);
			}
		}

		private void MoveItemToTop()
		{
			if (rightClickedItem.Section == SectionType.Favorites)
			{
				bool isSelectedSidebarItem = false;

				if (SelectedSidebarItem == rightClickedItem)
				{
					isSelectedSidebarItem = true;
				}

				int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem);
				App.SidebarPinnedController.Model.MoveItem(rightClickedItem, oldIndex, 0);

				if (isSelectedSidebarItem)
				{
					SetValue(SelectedSidebarItemProperty, rightClickedItem);
				}
			}
		}

		private void MoveItemUp()
		{
			if (rightClickedItem.Section == SectionType.Favorites)
			{
				bool isSelectedSidebarItem = false;

				if (SelectedSidebarItem == rightClickedItem)
				{
					isSelectedSidebarItem = true;
				}

				int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem);
				App.SidebarPinnedController.Model.MoveItem(rightClickedItem, oldIndex, oldIndex - 1);

				if (isSelectedSidebarItem)
				{
					SetValue(SelectedSidebarItemProperty, rightClickedItem);
				}
			}
		}

		private void MoveItemDown()
		{
			if (rightClickedItem.Section == SectionType.Favorites)
			{
				bool isSelectedSidebarItem = false;

				if (SelectedSidebarItem == rightClickedItem)
				{
					isSelectedSidebarItem = true;
				}

				int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem);
				App.SidebarPinnedController.Model.MoveItem(rightClickedItem, oldIndex, oldIndex + 1);

				if (isSelectedSidebarItem)
				{
					SetValue(SelectedSidebarItemProperty, rightClickedItem);
				}
			}
		}

		private void MoveItemToBottom()
		{
			if (rightClickedItem.Section == SectionType.Favorites)
			{
				bool isSelectedSidebarItem = false;

				if (SelectedSidebarItem == rightClickedItem)
				{
					isSelectedSidebarItem = true;
				}

				int oldIndex = App.SidebarPinnedController.Model.IndexOfItem(rightClickedItem);
				App.SidebarPinnedController.Model.MoveItem(rightClickedItem, oldIndex, App.SidebarPinnedController.Model.FavoriteItems.Count - 1);

				if (isSelectedSidebarItem)
				{
					SetValue(SelectedSidebarItemProperty, rightClickedItem);
				}
			}
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

			string navigationPath = args.InvokedItemContainer.Tag?.ToString();

			if (await CheckEmptyDrive(navigationPath))
			{
				return;
			}

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
			var context = (sender as NavigationViewItem).DataContext;
			if (properties.IsMiddleButtonPressed && context is INavigationControlItem item && item.Path is not null)
			{
				if (await CheckEmptyDrive(item.Path))
				{
					return;
				}
				IsInPointerPressed = true;
				e.Handled = true;
				await NavigationHelpers.OpenPathInNewTab(item.Path);
			}
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
			{
				secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width if the overflow menu setting is disabled
			}

			secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
			itemContextMenuFlyout.ShowAt(sidebarItem, new FlyoutShowOptions { Position = e.GetPosition(sidebarItem) });

			if (item.MenuOptions.ShowShellItems)
			{
				LoadShellMenuItems(itemContextMenuFlyout, item.MenuOptions);
			}

			e.Handled = true;
		}

		private void NavigationViewItem_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			if (!((sender as NavigationViewItem).DataContext is LocationItem locationItem))
			{
				return;
			}

			// Adding the original Location item dragged to the DragEvents data view
			var navItem = (sender as NavigationViewItem);
			args.Data.Properties.Add("sourceLocationItem", navItem);
		}

		private void NavigationViewItem_DragEnter(object sender, DragEventArgs e)
		{
			VisualStateManager.GoToState(sender as NavigationViewItem, "DragEnter", false);

			if ((sender as NavigationViewItem).DataContext is INavigationControlItem iNavItem)
			{
				if (string.IsNullOrEmpty(iNavItem.Path))
				{
					dragOverSection = sender;
					dragOverSectionTimer.Stop();
					dragOverSectionTimer.Debounce(() =>
					{
						if (dragOverSection is not null)
						{
							dragOverSectionTimer.Stop();
							if ((dragOverSection as NavigationViewItem).DataContext is LocationItem section)
							{
								section.IsExpanded = true;
							}
							dragOverSection = null;
						}
					}, TimeSpan.FromMilliseconds(1000), false);
				}
				else
				{
					dragOverItem = sender;
					dragOverItemTimer.Stop();
					dragOverItemTimer.Debounce(() =>
					{
						if (dragOverItem is not null)
						{
							dragOverItemTimer.Stop();
							SidebarItemInvoked?.Invoke(this, new SidebarItemInvokedEventArgs(dragOverItem as NavigationViewItemBase));
							dragOverItem = null;
						}
					}, TimeSpan.FromMilliseconds(1000), false);
				}
			}
		}

		private void NavigationViewItem_DragLeave(object sender, DragEventArgs e)
		{
			VisualStateManager.GoToState(sender as NavigationViewItem, "DragLeave", false);

			isDropOnProcess = false;

			if ((sender as NavigationViewItem).DataContext is INavigationControlItem)
			{
				if (sender == dragOverItem)
				{
					// Reset dragged over item
					dragOverItem = null;
				}
				if (sender == dragOverSection)
				{
					// Reset dragged over item
					dragOverSection = null;
				}
			}
		}

		private async void NavigationViewLocationItem_DragOver(object sender, DragEventArgs e)
		{
			if (!((sender as NavigationViewItem)?.DataContext is LocationItem locationItem))
			{
				return;
			}

			var deferral = e.GetDeferral();

			if (Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.Handled = true;
				isDropOnProcess = true;

				var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
				var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

				if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Favorites.Equals(locationItem.Section) && storageItems.Any())
				{
					bool haveFoldersToPin = false;

					foreach (var item in storageItems)
					{
						if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
						{
							haveFoldersToPin = true;
							break;
						}
					}

					if (!haveFoldersToPin)
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						e.DragUIOverride.IsCaptionVisible = true;
						e.DragUIOverride.Caption = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource();
						e.AcceptedOperation = DataPackageOperation.Move;
					}
				}
				else if (string.IsNullOrEmpty(locationItem.Path) ||
					(storageItems.Any() && storageItems.AreItemsAlreadyInFolder(locationItem.Path))
					|| locationItem.Path.StartsWith("Home".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
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
						e.DragUIOverride.IsCaptionVisible = true;
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
				}
				else if (!storageItems.Any())
				{
					e.AcceptedOperation = DataPackageOperation.None;
				}
				else
				{
					e.DragUIOverride.IsCaptionVisible = true;
					if (locationItem.Path.StartsWith(CommonPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						e.AcceptedOperation = DataPackageOperation.Move;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
					{
						e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						e.AcceptedOperation = DataPackageOperation.Link;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
					{
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
					else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
					{
						e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						e.AcceptedOperation = DataPackageOperation.Move;
					}
					else if (storageItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
						|| ZipStorageFolder.IsZipPath(locationItem.Path))
					{
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						e.AcceptedOperation = DataPackageOperation.Copy;
					}
					else if (storageItems.AreItemsInSameDrive(locationItem.Path) || locationItem.IsDefaultLocation)
					{
						e.AcceptedOperation = DataPackageOperation.Move;
						e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
					}
					else
					{
						e.AcceptedOperation = DataPackageOperation.Copy;
						e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
					}
				}
			}
			else if ((e.DataView.Properties["sourceLocationItem"] as NavigationViewItem)?.DataContext is LocationItem sourceLocationItem)
			{
				// else if the drag over event is called over a location item
				NavigationViewLocationItem_DragOver_SetCaptions(locationItem, sourceLocationItem, e);
			}

			deferral.Complete();
		}

		/// <summary>
		/// Sets the captions when dragging a location item over another location item
		/// </summary>
		/// <param name="senderLocationItem">The location item which fired the DragOver event</param>
		/// <param name="sourceLocationItem">The source location item</param>
		/// <param name="e">DragEvent args</param>
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
				e.AcceptedOperation = DataPackageOperation.Move;
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = "PinToSidebarByDraggingCaptionText".GetLocalizedResource();
			}
		}

		private async void NavigationViewLocationItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
			{
				return;
			}
			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if (!((sender as NavigationViewItem).DataContext is LocationItem locationItem))
			{
				return;
			}

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
						{
							SidebarPinnedModel.AddItem(item.Path);
						}
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
				// Else if the dropped item is a location item

				// Swap the two items
				SidebarPinnedModel.SwapItems(sourceLocationItem, locationItem);
			}

			await Task.Yield();
			lockFlag = false;
		}

		private async void NavigationViewDriveItem_DragOver(object sender, DragEventArgs e)
		{
			if (!((sender as NavigationViewItem).DataContext is DriveItem driveItem) ||
				!Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				return;
			}

			var deferral = e.GetDeferral();
			e.Handled = true;

			var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
			var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if ("DriveCapacityUnknown".GetLocalizedResource().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
				(storageItems.Any() && storageItems.AreItemsAlreadyInFolder(driveItem.Path)))
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else if (handledByFtp)
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
				e.AcceptedOperation = DataPackageOperation.Copy;
			}
			else if (!storageItems.Any())
			{
				e.AcceptedOperation = DataPackageOperation.None;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
				{
					e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					e.AcceptedOperation = DataPackageOperation.Link;
				}
				else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
				{
					e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					e.AcceptedOperation = DataPackageOperation.Copy;
				}
				else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
				{
					e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					e.AcceptedOperation = DataPackageOperation.Move;
				}
				else if (storageItems.AreItemsInSameDrive(driveItem.Path))
				{
					e.AcceptedOperation = DataPackageOperation.Move;
					e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
				}
				else
				{
					e.AcceptedOperation = DataPackageOperation.Copy;
					e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
				}
			}

			deferral.Complete();
		}

		private async void NavigationViewDriveItem_Drop(object sender, DragEventArgs e)
		{
			if (lockFlag)
			{
				return;
			}
			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if (!((sender as NavigationViewItem).DataContext is DriveItem driveItem))
			{
				return;
			}

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
			if (!((sender as NavigationViewItem).DataContext is FileTagItem fileTagItem) ||
				!Filesystem.FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				return;
			}

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
			{
				return;
			}
			lockFlag = true;

			dragOverItem = null; // Reset dragged over item
			dragOverSection = null; // Reset dragged over section

			if (!((sender as NavigationViewItem).DataContext is FileTagItem fileTagItem))
			{
				return;
			}

			VisualStateManager.GoToState(sender as NavigationViewItem, "Drop", false);

			var deferral = e.GetDeferral();

			var handledByFtp = await Filesystem.FilesystemHelpers.CheckDragNeedsFulltrust(e.DataView);
			var storageItems = await Filesystem.FilesystemHelpers.GetDraggedStorageItems(e.DataView);

			if (handledByFtp)
			{
				return;
			}

			foreach (var item in storageItems.Where(x => !string.IsNullOrEmpty(x.Path)))
			{
				var listedItem = new ListedItem(null) { ItemPath = item.Path };
				listedItem.FileFRN = await FileTagsHelper.GetFileFRN(item.Item);
				listedItem.FileTags = new[] { fileTagItem.FileTag.Uid };
			}

			deferral.Complete();
			await Task.Yield();
			lockFlag = false;
		}

		private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
		{
			(this.FindDescendant("TabContentBorder") as Border).Child = TabContent;
		}

		private void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
		{
			IsPaneToggleButtonVisible = args.DisplayMode == NavigationViewDisplayMode.Minimal;
		}

		private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var step = 1;
			var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
			originalSize = IsPaneOpen ? UserSettingsService.AppearanceSettingsService.SidebarWidth : CompactPaneLength;

			if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
			{
				step = 5;
			}

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
			{
				SetSize(e.Cumulative.Translation.X);
			}
		}

		private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (!dragging) // keep showing pressed event if currently resizing the sidebar
			{
				(sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
				VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
			}
		}

		private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (DisplayMode == NavigationViewDisplayMode.Expanded)
			{
				(sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
				VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPointerOver", true);
			}
		}

		private void SetSize(double val, bool closeImmediatleyOnOversize = false)
		{
			if (IsPaneOpen)
			{
				var newSize = originalSize + val;
				if (newSize <= Constants.UI.MaximumSidebarWidth && newSize >= Constants.UI.MinimumSidebarWidth)
				{
					OpenPaneLength = newSize; // passing a negative value will cause an exception
				}

				if (newSize < Constants.UI.MinimumSidebarWidth) // if the new size is below the minimum, check whether to toggle the pane
				{
					if (Constants.UI.MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatleyOnOversize) // collapse the sidebar
					{
						IsPaneOpen = false;
					}
				}
			}
			else
			{
				if (val >= Constants.UI.MinimumSidebarWidth - CompactPaneLength || closeImmediatleyOnOversize)
				{
					OpenPaneLength = Constants.UI.MinimumSidebarWidth + (val + CompactPaneLength - Constants.UI.MinimumSidebarWidth); // set open sidebar length to minimum value to keep it smooth
					IsPaneOpen = true;
				}
			}
		}

		private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			(sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
			UserSettingsService.AppearanceSettingsService.SidebarWidth = OpenPaneLength;
			dragging = false;
		}

		private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			IsPaneOpen = !IsPaneOpen;
		}

		private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			if (DisplayMode == NavigationViewDisplayMode.Expanded)
			{
				originalSize = IsPaneOpen ? UserSettingsService.AppearanceSettingsService.SidebarWidth : CompactPaneLength;
				(sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
				VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPressed", true);
				dragging = true;
			}
		}

		private async Task<bool> CheckEmptyDrive(string drivePath)
		{
			if (drivePath is not null)
			{
				var matchingDrive = App.DrivesManager.Drives.FirstOrDefault(x => drivePath.StartsWith(x.Path, StringComparison.Ordinal));
				if (matchingDrive is not null && matchingDrive.Type == DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
				{
					bool ejectButton = await DialogDisplayHelper.ShowDialogAsync("InsertDiscDialog/Title".GetLocalizedResource(), string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path), "InsertDiscDialog/OpenDriveButton".GetLocalizedResource(), "Close".GetLocalizedResource());
					if (ejectButton)
					{
						var result = await DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
						await UIHelpers.ShowDeviceEjectResultAsync(result);
					}
					return true;
				}
			}
			return false;
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
				if (options.IsLocationItem)
				{
					var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
					var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(currentInstanceViewModel: null, workingDir: null,
						new List<ListedItem>() { new ListedItem(null) { ItemPath = rightClickedItem.Path } }, shiftPressed: shiftPressed, showOpenMenu: false, default);
					if (!UserSettingsService.AppearanceSettingsService.MoveOverflowMenuItemsToSubMenu)
					{
						var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(shellMenuItems);
						if (secondaryElements.Any())
						{
							var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);
							var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");
							var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
							if (itemsControl is not null)
							{
								secondaryElements.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin); // Set items max width to current menu width (#5555)
							}

							itemContextMenuFlyout.SecondaryCommands.Add(new AppBarSeparator());
							secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
						}
					}
					else
					{
						var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(shellMenuItems);
						var overflowItem = itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
						if (overflowItem is not null)
						{
							overflowItems.ForEach(i => (overflowItem.Flyout as MenuFlyout).Items.Add(i));
							overflowItem.Visibility = overflowItems.Any() ? Visibility.Visible : Visibility.Collapsed;
						}
					}
				}
			}
			catch { }
		}

		public static GridLength GetSidebarCompactSize()
		{
			if (App.Current.Resources.TryGetValue("NavigationViewCompactPaneLength", out object paneLength))
			{
				if (paneLength is double paneLengthDouble)
				{
					return new GridLength(paneLengthDouble);
				}
			}
			return new GridLength(200);
		}

		#region Sidebar sections expanded state management

		private async void NavigationView_Expanding(NavigationView sender, NavigationViewItemExpandingEventArgs args)
		{
			if (args.ExpandingItem is LocationItem loc && loc.ChildItems is not null)
			{
				await Task.Delay(50); // Wait a little so IsPaneOpen tells the truth when in minimal mode
				if (sender.IsPaneOpen) // Don't store expanded state if sidebar pane is closed
				{
					App.AppSettings.Set(true, $"section:{loc.Text.Replace('\\', '_')}");
				}
			}
		}

		private async void NavigationView_Collapsed(NavigationView sender, NavigationViewItemCollapsedEventArgs args)
		{
			if (args.CollapsedItem is LocationItem loc && loc.ChildItems is not null)
			{
				await Task.Delay(50); // Wait a little so IsPaneOpen tells the truth when in minimal mode
				if (sender.IsPaneOpen) // Don't store expanded state if sidebar pane is closed
				{
					App.AppSettings.Set(false, $"section:{loc.Text.Replace('\\', '_')}");
				}
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

		protected override DataTemplate SelectTemplateCore(object item)
		{
			if (item is not null && item is INavigationControlItem)
			{
				INavigationControlItem navControlItem = item as INavigationControlItem;
				switch (navControlItem.ItemType)
				{
					case NavigationControlItemType.Location:
						return LocationNavItemTemplate;

					case NavigationControlItemType.Drive:
						return DriveNavItemTemplate;

					case NavigationControlItemType.CloudDrive:
						return DriveNavItemTemplate;

					case NavigationControlItemType.LinuxDistro:
						return LinuxNavItemTemplate;

					case NavigationControlItemType.FileTag:
						return FileTagNavItemTemplate;
				}
			}
			return null;
		}
	}
}