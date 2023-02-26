using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Interacts;
using Files.App.ViewModels;
using Files.Backend.Helpers;
using Files.Backend.Services;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Files.App.Helpers
{
	public static class ContextFlyoutItemHelper
	{
		private static readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private static readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

		public static List<ContextMenuFlyoutItemViewModel> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, ItemViewModel? itemViewModel = null)
		{
			var menuItemsList = GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, currentInstanceViewModel: currentInstanceViewModel, itemViewModel: itemViewModel);
			menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, removeOverflowMenu: false);
			return menuItemsList;
		}

		public static Task<List<ContextMenuFlyoutItemViewModel>> GetItemContextShellCommandsAsync(string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu, CancellationToken cancellationToken)
			=> ShellContextmenuHelper.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, workingDirectory: workingDir, selectedItems: selectedItems, cancellationToken: cancellationToken);

		public static List<ContextMenuFlyoutItemViewModel> Filter(List<ContextMenuFlyoutItemViewModel> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel, bool removeOverflowMenu = true)
		{
			items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList();
			items.ForEach(x => x.Items = x.Items?.Where(y => Check(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList());

			var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
			if (overflow is not null)
			{
				if (!shiftPressed && userSettingsService.PreferencesSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ItemType.Separator)
						overflow.Items.Add(new ContextMenuFlyoutItemViewModel { ItemType = ItemType.Separator });

					items = items.Except(overflowItems).ToList();
					overflow.Items.AddRange(overflowItems);
				}

				// remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool Check(ContextMenuFlyoutItemViewModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems)
		{
			return (item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin)
				&& (item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults)
				&& (item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp)
				&& (item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder)
				&& (!item.SingleItemOnly || selectedItems.Count == 1)
				&& item.ShowItem;
		}

		public static List<ContextMenuFlyoutItemViewModel> GetBaseItemMenuItems(
			BaseLayoutCommandsViewModel commandsViewModel,
			SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
			List<ListedItem> selectedItems,
			CurrentInstanceViewModel currentInstanceViewModel,
			ItemViewModel itemViewModel = null)
		{
			bool itemsSelected = itemViewModel is null;
			bool canDecompress = selectedItems.Any() && selectedItems.All(x => x.IsArchive)
				|| selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && FileExtensionHelpers.IsZipFile(x.FileExtension));
			bool canCompress = !canDecompress || selectedItems.Count > 1;
			bool showOpenItemWith = selectedItems.All(
				i => (i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) || (i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));
			bool areAllItemsFolders = selectedItems.All(i => i.PrimaryItemAttribute == StorageItemTypes.Folder);
			bool isFirstFileExecutable = FileExtensionHelpers.IsExecutableFile(selectedItems.FirstOrDefault()?.FileExtension);
			string newArchiveName =
				Path.GetFileName(selectedItems.Count is 1 ? selectedItems[0].ItemPath : Path.GetDirectoryName(selectedItems[0].ItemPath))
				?? string.Empty;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "LayoutMode".GetLocalizedResource(),
					Glyph = "\uE152",
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>()
					{
						// Details view
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Details".GetLocalizedResource(),
							Glyph = "\uE179",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ToggleLayoutModeDetailsViewCommand,
							CommandParameter = true,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutDetails/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number1, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
						// Tiles view
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Tiles".GetLocalizedResource(),
							Glyph = "\uE15C",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeTilesCommand,
							CommandParameter = true,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutTiles/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number2, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
						// Grid view small
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "SmallIcons".GetLocalizedResource(),
							Glyph = "\uE80A",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmallCommand,
							CommandParameter = true,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutSmallIcons/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number3, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
						// Grid view medium
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "MediumIcons".GetLocalizedResource(),
							Glyph = "\uF0E2",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMediumCommand,
							CommandParameter = true,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutMediumIcons/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number4, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
						// Grid view large
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "LargeIcons".GetLocalizedResource(),
							Glyph = "\uE739",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command =  currentInstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLargeCommand,
							CommandParameter = true,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutLargeIcons/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number5, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
						// Column view
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Columns".GetLocalizedResource(),
							Glyph = "\uF115",
							GlyphFontFamilyName = "CustomGlyph",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ToggleLayoutModeColumnViewCommand,
							CommandParameter = true,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutColumn/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number6, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
						// Column view
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Adaptive".GetLocalizedResource(),
							Glyph = "\uF576",
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ToggleLayoutModeAdaptiveCommand,
							KeyboardAcceleratorTextOverride = "BaseLayoutContextFlyoutAdaptive/KeyboardAcceleratorTextOverride".GetLocalizedResource(),
							KeyboardAccelerator = new KeyboardAccelerator{Key = VirtualKey.Number7, Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, IsEnabled = false}
						},
					},
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SortBy".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF029",
						OverlayLayerGlyph = "\uF02A",
					},
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>()
					{
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Name".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByName ?? false,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByName = true) : null!,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByDate ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByDate = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateCreated".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByDateCreated ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByDateCreated = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Type".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByType ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByType = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Size".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedBySize ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedBySize = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "SyncStatus".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedBySyncStatus ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedBySyncStatus = true) : null!,
							ShowItem = currentInstanceViewModel.IsPageTypeCloudDrive,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "FileTags".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByFileTag ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByFileTag = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "OriginalPath".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByOriginalPath ?? false,
							ShowInRecycleBin = true,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByOriginalPath = true) : null!,
							ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedByDateDeleted ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedByDateDeleted = true) : null!,
							ShowInRecycleBin = true,
							ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							ItemType = ItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Ascending".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedAscending ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedAscending = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							ItemType = ItemType.Toggle
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Descending".GetLocalizedResource(),
							IsChecked = itemViewModel?.IsSortedDescending ?? false,
							Command = itemViewModel is not null ? new RelayCommand(() => itemViewModel.IsSortedDescending = true) : null!,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							ItemType = ItemType.Toggle
						},
					},
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "NavToolbarGroupByRadioButtons/Text".GetLocalizedResource(),
					Glyph = "\uF168",
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>()
					{
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "None".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.None,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.None,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Name".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.Name,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.Name,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateModified,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.DateModified,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateCreated".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateCreated,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.DateCreated,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Type".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FileType,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.FileType,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Size".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.Size,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.Size,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "SyncStatus".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.SyncStatus,
							ShowItem = currentInstanceViewModel.IsPageTypeCloudDrive,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.SyncStatus,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "FileTags".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FileTag,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.FileTag,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "NavToolbarArrangementOptionOriginalFolder/Text".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.OriginalFolder,
							ShowInRecycleBin = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.OriginalFolder,
							ItemType = ItemType.Toggle,
							ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.DateDeleted,
							ShowInRecycleBin = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.DateDeleted,
							ItemType = ItemType.Toggle,
							ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "NavToolbarArrangementOptionFolderPath/Text".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FolderPath,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupOptionCommand,
							CommandParameter = GroupOption.FolderPath,
							ItemType = ItemType.Toggle,
							ShowItem = currentInstanceViewModel.IsPageTypeLibrary,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							ItemType = ItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Ascending".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupDirection == SortDirection.Ascending,
							IsEnabled = currentInstanceViewModel.FolderSettings.DirectoryGroupOption != GroupOption.None,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupDirectionCommand,
							CommandParameter = SortDirection.Ascending,
							ItemType = ItemType.Toggle,
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "Descending".GetLocalizedResource(),
							IsChecked = currentInstanceViewModel.FolderSettings.DirectoryGroupDirection == SortDirection.Descending,
							IsEnabled = currentInstanceViewModel.FolderSettings.DirectoryGroupOption != GroupOption.None,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Command = currentInstanceViewModel.FolderSettings.ChangeGroupDirectionCommand,
							CommandParameter = SortDirection.Descending,
							ItemType = ItemType.Toggle,
						},
					},
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutRefresh/Text".GetLocalizedResource(),
					Glyph = "\uE72C",
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Command = commandsViewModel.RefreshCommand,
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.F5,
						IsEnabled = false,
					},
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutNew/Label".GetLocalizedResource(),
					OpacityIcon = new UserControls.OpacityIcon()
					{
						Style = (Style)Application.Current.Resources["ColorIconNew"],
					},
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.N,
						Modifiers = VirtualKeyModifiers.Control,
						IsEnabled = false,
					},
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "FormatDriveText".GetLocalizedResource(),
					Command = commandsViewModel.FormatDriveCommand,
					CommandParameter = itemViewModel?.CurrentFolder,
					ShowItem = itemViewModel?.CurrentFolder is not null && (App.DrivesManager.Drives.FirstOrDefault(x => string.Equals(x.Path, itemViewModel?.CurrentFolder.ItemPath))?.MenuOptions.ShowFormatDrive ?? false),
				},
				new ContextMenuFlyoutItemViewModel(commands.EmptyRecycleBin)
				{
					IsEnabled = RecycleBinHelpers.RecycleBinHasItems(),
					ShowItem = !itemsSelected && currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RestoreAllItems".GetLocalizedResource(),
					Glyph = "\xE777",
					Command = commandsViewModel.RestoreRecycleBinCommand,
					ShowItem = !itemsSelected && currentInstanceViewModel.IsPageTypeRecycleBin,
					ShowInRecycleBin = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Restore".GetLocalizedResource(),
					Glyph = "\xE777",
					Command = commandsViewModel.RestoreItemCommand,
					ShowInRecycleBin = true,
					ShowItem = itemsSelected && selectedItems.All(x => x.IsRecycleBinItem)
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Open".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF047",
						OverlayLayerGlyph = "\uF048",
					},
					Command = commandsViewModel.OpenItemCommand,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected && selectedItems.Count <= 10,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF049",
						OverlayLayerGlyph = "\uF04A",
					},
					Command = commandsViewModel.OpenItemWithApplicationPickerCommand,
					Tag = "OpenWith",
					CollapseLabel = true,
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF049",
						OverlayLayerGlyph = "\uF04A",
					},
					Tag = "OpenWithOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<ContextMenuFlyoutItemViewModel>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uE8DA",
					Command = commandsViewModel.OpenFileLocationCommand,
					ShowItem = itemsSelected && selectedItems.All(i => i.IsShortcut),
					ShowInSearchPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					Glyph = "\uF113",
					GlyphFontFamilyName = "CustomGlyph",
					Command = commandsViewModel.OpenDirectoryInNewTabCommand,
					ShowItem = itemsSelected && selectedItems.Count < 5 && areAllItemsFolders && userSettingsService.PreferencesSettingsService.ShowOpenInNewTab,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					Glyph = "\uE737",
					Command = commandsViewModel.OpenInNewWindowItemCommand,
					ShowItem = itemsSelected && selectedItems.Count < 5 && areAllItemsFolders && userSettingsService.PreferencesSettingsService.ShowOpenInNewWindow,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseBackdropGlyph = "\uF056",
						BaseLayerGlyph = "\uF03B",
						OverlayLayerGlyph = "\uF03C",
					},
					Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
					ShowItem = itemsSelected && userSettingsService.PreferencesSettingsService.ShowOpenInNewPane && areAllItemsFolders,
					SingleItemOnly = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					ShowInSearchPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>()
					{
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "SetAsBackground".GetLocalizedResource(),
							Glyph = "\uE91B",
							Command = commandsViewModel.SetAsDesktopBackgroundItemCommand,
							ShowInSearchPage = true,
							ShowItem = selectedItemsPropertiesViewModel?.SelectedItemsCount == 1
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "BaseLayoutItemContextFlyoutSetAsLockscreenBackground/Text".GetLocalizedResource(),
							Glyph = "\uF114",
							GlyphFontFamilyName = "CustomGlyph",
							Command = commandsViewModel.SetAsLockscreenBackgroundItemCommand,
							ShowInSearchPage = true,
							ShowItem = selectedItemsPropertiesViewModel?.SelectedItemsCount == 1
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "SetAsSlideshow".GetLocalizedResource(),
							Glyph = "\uE91B",
							GlyphFontFamilyName = "CustomGlyph",
							Command = commandsViewModel.SetAsDesktopBackgroundItemCommand,
							ShowInSearchPage = true,
							ShowItem = selectedItemsPropertiesViewModel?.SelectedItemsCount > 1
						},
					}
				},
				new ContextMenuFlyoutItemViewModel
				{
					Text = "RotateRight".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel
					{
						BaseLayerGlyph = "\uF045",
						OverlayLayerGlyph = "\uF046",
					},
					Command = commandsViewModel.RotateImageRightCommand,
					ShowInSearchPage = true,
					ShowItem = selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false
				},
				new ContextMenuFlyoutItemViewModel
				{
					Text = "RotateLeft".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel
					{
						BaseLayerGlyph = "\uF043",
						OverlayLayerGlyph = "\uF044",
					},
					Command = commandsViewModel.RotateImageLeftCommand,
					ShowInSearchPage = true,
					ShowItem = selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "RunAsAdministrator".GetLocalizedResource(),
					Glyph = "\uE7EF",
					Command = commandsViewModel.RunAsAdminCommand,
					ShowInSearchPage = true,
					ShowItem = itemsSelected && isFirstFileExecutable
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource(),
					Glyph = "\uE7EE",
					Command = commandsViewModel.RunAsAnotherUserCommand,
					ShowInSearchPage = true,
					ShowItem = itemsSelected && isFirstFileExecutable
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutCut/Text".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF03D",
						OverlayLayerGlyph = "\uF03E",
					},
					Command = commandsViewModel.CutItemCommand,
					IsPrimary = true,
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.X,
						Modifiers = VirtualKeyModifiers.Control,
						IsEnabled = false,
					},
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Copy".GetLocalizedResource(),
					//Glyph = "\uF8C8",
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF021",
						OverlayLayerGlyph = "\uF022",
					},
					Command = commandsViewModel.CopyItemCommand,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsPrimary = true,
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.C,
						Modifiers = VirtualKeyModifiers.Control,
						IsEnabled = false,
					},
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "CopyLocation".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF04F",
						OverlayLayerGlyph = "\uF050"
					},
					Command = commandsViewModel.CopyPathOfSelectedItemCommand,
					SingleItemOnly = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Paste".GetLocalizedResource(),
					//Glyph = "\uF16D",
					IsPrimary = true,
					ColoredIcon = new ColoredIconModel()
					{
						BaseBackdropGlyph = "\uF052",
						BaseLayerGlyph = "\uF023",
						OverlayLayerGlyph = "\uF024",
					},
					Command = commandsViewModel.PasteItemsFromClipboardCommand,
					ShowItem = areAllItemsFolders || !itemsSelected,
					SingleItemOnly = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = App.AppModel.IsPasteEnabled,
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.V,
						Modifiers = VirtualKeyModifiers.Control,
						IsEnabled = false,
					},
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutCreateFolderWithSelection/Text".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF033",
						OverlayLayerGlyph = "\uF034"
					},
					Command = commandsViewModel.CreateFolderWithSelection,
					ShowItem = itemsSelected,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutShortcut/Text".GetLocalizedResource(),
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF04B",
						OverlayLayerGlyph = "\uF04C"
					},
					Command = commandsViewModel.CreateShortcutCommand,
					ShowItem = itemsSelected && (!selectedItems.FirstOrDefault()?.IsShortcut ?? false),
					SingleItemOnly = true,
					ShowInSearchPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Rename".GetLocalizedResource(),
					IsPrimary = true,
					ColoredIcon = new ColoredIconModel()
					{
						BaseBackdropGlyph = "\uF054",
						BaseLayerGlyph = "\uF027",
						OverlayLayerGlyph = "\uF028",
					},
					Command = commandsViewModel.RenameItemCommand,
					SingleItemOnly = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.F2,
						IsEnabled = false,
					},
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutShare/Text".GetLocalizedResource(),
					IsPrimary = true,
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF025",
						OverlayLayerGlyph = "\uF026",
					},
					Command = commandsViewModel.ShareItemCommand,
					ShowItem = itemsSelected && DataTransferManager.IsSupported() && !selectedItems.Any(i => i.IsHiddenItem || (i.IsShortcut && !i.IsLinkItem) || (i.PrimaryItemAttribute == StorageItemTypes.Folder && !i.IsArchive)),
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Delete".GetLocalizedResource(),
					IsPrimary = true,
					ColoredIcon = new ColoredIconModel()
					{
						BaseBackdropGlyph = "\uF053",
						BaseLayerGlyph = "\uF035",
						OverlayLayerGlyph = "\uF036"
					},
					Command = commandsViewModel.DeleteItemCommand,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					KeyboardAccelerator = new KeyboardAccelerator
					{
						Key = VirtualKey.Delete,
						IsEnabled = false,
					},
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					IsPrimary = true,
					ColoredIcon = new ColoredIconModel()
					{
						BaseLayerGlyph = "\uF031",
						OverlayLayerGlyph = "\uF032"
					},
					Command = commandsViewModel.ShowPropertiesCommand,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalizedResource(),
					Glyph = "\uE197",
					Command = commandsViewModel.OpenParentFolderCommand,
					ShowItem = itemsSelected && currentInstanceViewModel.IsPageTypeSearchResults,
					SingleItemOnly = true,
					ShowInSearchPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutPinToFavorites/Text".GetLocalizedResource(),
					Glyph = "\uE840",
					Command = commandsViewModel.SidebarPinItemCommand,
					ShowItem = userSettingsService.PreferencesSettingsService.ShowFavoritesSection && selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsArchive && !x.IsPinned),
					ShowInSearchPage = true,
					ShowInFtpPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					Glyph = "\uE77A",
					Command = commandsViewModel.SidebarUnpinItemCommand,
					ShowItem = userSettingsService.PreferencesSettingsService.ShowFavoritesSection && selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsArchive && x.IsPinned),
					ShowInSearchPage = true,
					ShowInFtpPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinItemToStart/Text".GetLocalizedResource(),
					Glyph = "\uE840",
					Command = commandsViewModel.PinItemToStartCommand,
					ShowOnShift = true,
					ShowItem = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					SingleItemOnly = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinItemFromStart/Text".GetLocalizedResource(),
					Glyph = "\uE77A",
					Command = commandsViewModel.UnpinItemFromStartCommand,
					ShowOnShift = true,
					ShowItem = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					SingleItemOnly = true,
				},
				new ContextMenuFlyoutItemViewModel
				{
					Text = "Archive".GetLocalizedResource(),
					ShowInSearchPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>
					{
						new ContextMenuFlyoutItemViewModel
						{
							Text = "ExtractFiles".GetLocalizedResource(),
							Glyph = "\xF11A",
							GlyphFontFamilyName = "CustomGlyph",
							Command = commandsViewModel.DecompressArchiveCommand,
							ShowItem = canDecompress,
							ShowInSearchPage = true,
						},
						new ContextMenuFlyoutItemViewModel
						{
							Text = "ExtractHere".GetLocalizedResource(),
							Glyph = "\xF11A",
							GlyphFontFamilyName = "CustomGlyph",
							Command = commandsViewModel.DecompressArchiveHereCommand,
							ShowItem = canDecompress,
							ShowInSearchPage = true,
						},
						new ContextMenuFlyoutItemViewModel
						{
							Text = selectedItems.Count > 1
								? string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(), "*")
								: string.Format("BaseLayoutItemContextFlyoutExtractToChildFolder".GetLocalizedResource(),
									Path.GetFileNameWithoutExtension(selectedItems.First().Name)),
							Glyph = "\xF11A",
							GlyphFontFamilyName = "CustomGlyph",
							Command = commandsViewModel.DecompressArchiveToChildFolderCommand,
							ShowInSearchPage = true,
							ShowItem = canDecompress,
						},
						new ContextMenuFlyoutItemViewModel
						{
							ShowItem = canDecompress && canCompress,
							ItemType = ItemType.Separator,
						},
						new ContextMenuFlyoutItemViewModel
						{
							Text = "CreateArchive".GetLocalizedResource(),
							Glyph = "\uE8DE",
							Command = commandsViewModel.CompressIntoArchiveCommand,
							ShowItem = canCompress,
							ShowInSearchPage = true,
						},
						new ContextMenuFlyoutItemViewModel
						{
							Text = string.Format("CreateNamedArchive".GetLocalizedResource(), $"{newArchiveName}.zip"),
							Glyph = "\uE8DE",
							Command = commandsViewModel.CompressIntoZipCommand,
							ShowItem = canCompress,
							ShowInSearchPage = true,
						},
						new ContextMenuFlyoutItemViewModel
						{
							Text = string.Format("CreateNamedArchive".GetLocalizedResource(), $"{newArchiveName}.7z"),
							Glyph = "\uE8DE",
							Command = commandsViewModel.CompressIntoSevenZipCommand,
							ShowItem = canCompress,
							ShowInSearchPage = true,
						},
					},
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					CollapseLabel = true,
					ShowInSearchPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<ContextMenuFlyoutItemViewModel>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					Tag = "OverflowSeparator",
					ShowInSearchPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					ShowInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.ShowItem).ToList();
		}

		public static List<ContextMenuFlyoutItemViewModel> GetNewItemItems(BaseLayoutCommandsViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Folder".GetLocalizedResource(),
					Glyph = "\uE8B7",
					Command = commandsViewModel.CreateNewFolderCommand,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					CommandParameter = null,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new ContextMenuFlyoutItemViewModel
				{
					Text = "Shortcut".GetLocalizedResource(),
					Glyph = "\uF10A",
					GlyphFontFamilyName = "CustomGlyph",
					Command = commandsViewModel.CreateShortcutFromDialogCommand
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
				}
			};

			if (canCreateFileInPage)
			{
				var cachedNewContextMenuEntries = addItemService.GetNewEntriesAsync().Result;
				cachedNewContextMenuEntries?.ForEach(i =>
				{
					if (!string.IsNullOrEmpty(i.IconBase64))
					{
						// loading the bitmaps takes a while, so this caches them
						byte[] bitmapData = Convert.FromBase64String(i.IconBase64);
						using var ms = new MemoryStream(bitmapData);
						var bitmap = new BitmapImage();
						_ = bitmap.SetSourceAsync(ms.AsRandomAccessStream());
						list.Add(new ContextMenuFlyoutItemViewModel()
						{
							Text = i.Name,
							BitmapIcon = bitmap,
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
					else
					{
						list.Add(new ContextMenuFlyoutItemViewModel()
						{
							Text = i.Name,
							Glyph = "\xE7C3",
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
				});
			}

			return list;
		}
	}
}