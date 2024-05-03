// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels.Layouts;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage;

namespace Files.App.Data.Factories
{
	/// <summary>
	/// Represents a factory to generate a list for layout pages.
	/// </summary>
	public static class ContentPageContextFlyoutFactory
	{
		// Dependency injections

		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static readonly IModifiableCommandManager ModifiableCommands = Ioc.Default.GetRequiredService<IModifiableCommandManager>();
		private static readonly IAddItemService AddItemService = Ioc.Default.GetRequiredService<IAddItemService>();
		private static readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<ContextFlyoutItemModel> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, BaseLayoutViewModel commandsViewModel, bool shiftPressed, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, ItemViewModel? itemViewModel = null)
		{
			var menuItemsList = GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, currentInstanceViewModel: currentInstanceViewModel, itemViewModel: itemViewModel);
			menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, removeOverflowMenu: false);
			return menuItemsList;
		}

		public static Task<List<ContextFlyoutItemModel>> GetItemContextShellCommandsAsync(string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu, CancellationToken cancellationToken)
		{
			return ShellContextFlyoutFactory.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, workingDirectory: workingDir, selectedItems: selectedItems, cancellationToken: cancellationToken);
		}

		public static List<ContextFlyoutItemModel> Filter(List<ContextFlyoutItemModel> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel, bool removeOverflowMenu = true)
		{
			items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList();
			items.ForEach(x => x.Items = x.Items?.Where(y => Check(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList());

			var overflow = items.FirstOrDefault(x => x.Tag == "ItemOverflow");
			if (overflow is not null)
			{
				if (!shiftPressed && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.IsVisibleOnShiftPressed).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextFlyoutItemType.Separator)
						overflow.Items.Add(new() { ItemType = ContextFlyoutItemType.Separator });

					items = items.Except(overflowItems).ToList();
					overflow.Items.AddRange(overflowItems);
				}

				// remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool Check(ContextFlyoutItemModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems)
		{
			return
				(item.IsVisibleInRecycleBinPage || !currentInstanceViewModel.IsPageTypeRecycleBin) &&
				(item.IsVisibleInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) &&
				(item.IsVisibleInFtpPage || !currentInstanceViewModel.IsPageTypeFtp) &&
				(item.IsVisibleInArchivePage || !currentInstanceViewModel.IsPageTypeZipFolder) &&
				item.IsAvailable;
		}

		public static List<ContextFlyoutItemModel> GetBaseItemMenuItems(
			BaseLayoutViewModel commandsViewModel,
			SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
			List<ListedItem> selectedItems,
			CurrentInstanceViewModel currentInstanceViewModel,
			ItemViewModel? itemViewModel = null)
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

			bool isDriveRoot = itemViewModel?.CurrentFolder is not null && (itemViewModel.CurrentFolder.ItemPath == Path.GetPathRoot(itemViewModel.CurrentFolder.ItemPath));

			return new List<ContextFlyoutItemModel>()
			{
				new()
				{
					Text = "Layout".GetLocalizedResource(),
					Glyph = "\uE8A9",
					IsAvailable = !itemsSelected,
					IsVisibleInRecycleBinPage = true,
					IsVisibleInSearchPage = true,
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					Items =
					[
						new(ContextFlyoutItemType.Toggle, Commands.LayoutDetails),
						new(ContextFlyoutItemType.Toggle, Commands.LayoutTiles),
						new(ContextFlyoutItemType.Toggle, Commands.LayoutList),
						new(ContextFlyoutItemType.Toggle, Commands.LayoutGrid),
						new(ContextFlyoutItemType.Toggle, Commands.LayoutColumns),
						new(ContextFlyoutItemType.Toggle, Commands.LayoutAdaptive),
					],
				},
				new()
				{
					Text = "SortBy".GetLocalizedResource(),
					OpacityIcon = new("ColorIconSort"),
					IsAvailable = !itemsSelected,
					IsVisibleInRecycleBinPage = true,
					IsVisibleInSearchPage = true,
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					Items =
					[
						new(ContextFlyoutItemType.Toggle, Commands.SortByName),
						new(ContextFlyoutItemType.Toggle, Commands.SortByDateModified),
						new(ContextFlyoutItemType.Toggle, Commands.SortByDateCreated),
						new(ContextFlyoutItemType.Toggle, Commands.SortByType),
						new(ContextFlyoutItemType.Toggle, Commands.SortBySize),
						new(ContextFlyoutItemType.Toggle, Commands.SortBySyncStatus),
						new(ContextFlyoutItemType.Toggle, Commands.SortByTag),
						new(ContextFlyoutItemType.Toggle, Commands.SortByPath),
						new(ContextFlyoutItemType.Toggle, Commands.SortByOriginalFolder),
						new(ContextFlyoutItemType.Toggle, Commands.SortByDateDeleted),
						new(ContextFlyoutItemType.Separator)
						{
							IsVisibleInRecycleBinPage = true,
							IsVisibleInSearchPage = true,
							IsVisibleInFtpPage = true,
							IsVisibleInArchivePage = true,
						},
						new(ContextFlyoutItemType.Toggle, Commands.SortAscending),
						new(ContextFlyoutItemType.Toggle, Commands.SortDescending),
					],
				},
				new()
				{
					Text = "GroupBy".GetLocalizedResource(),
					Glyph = "\uF168",
					IsAvailable = !itemsSelected,
					IsVisibleInRecycleBinPage = true,
					IsVisibleInSearchPage = true,
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					Items =
					[
						new(ContextFlyoutItemType.Toggle, Commands.GroupByNone),
						new(ContextFlyoutItemType.Toggle, Commands.GroupByName),
						new()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							IsVisibleInRecycleBinPage = true,
							IsVisibleInSearchPage = true,
							IsVisibleInFtpPage = true,
							IsVisibleInArchivePage = true,
							Items =
							[
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateModifiedYear),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateModifiedMonth),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateModifiedDay),
							],
						},
						new()
						{
							Text = "DateCreated".GetLocalizedResource(),
							IsVisibleInRecycleBinPage = true,
							IsVisibleInSearchPage = true,
							IsVisibleInFtpPage = true,
							IsVisibleInArchivePage = true,
							Items =
							[
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateCreatedYear),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateCreatedMonth),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateCreatedDay),
							],
						},
						new(ContextFlyoutItemType.Toggle, Commands.GroupByType),
						new(ContextFlyoutItemType.Toggle, Commands.GroupBySize),
						new(ContextFlyoutItemType.Toggle, Commands.GroupBySyncStatus),
						new(ContextFlyoutItemType.Toggle, Commands.GroupByTag),
						new(ContextFlyoutItemType.Toggle, Commands.GroupByOriginalFolder),
						new()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							IsVisibleInRecycleBinPage = true,
							IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin,
							Items =
							[
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateDeletedYear),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateDeletedMonth),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateDeletedDay),
							],
						},
						new(ContextFlyoutItemType.Toggle, Commands.GroupByFolderPath),
						new(ContextFlyoutItemType.Separator)
						{
							IsVisibleInRecycleBinPage = true,
							IsVisibleInSearchPage = true,
							IsVisibleInFtpPage = true,
							IsVisibleInArchivePage = true,
						},
						new(ContextFlyoutItemType.Toggle, Commands.GroupAscending),
						new(ContextFlyoutItemType.Toggle, Commands.GroupDescending),
					],
				},
				new(ContextFlyoutItemType.Toggle, Commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				},
				new()
				{
					ItemType = ContextFlyoutItemType.Separator,
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					IsAvailable = !itemsSelected
				},
				new()
				{
					OpacityIcon = new(Commands.AddItem.Glyph.OpacityStyle),
					Text = Commands.AddItem.Label,
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					IsAvailable = !itemsSelected,
					IsVisibleInFtpPage = true
				},
				new(ContextFlyoutItemType.Button, Commands.FormatDrive),
				new(ContextFlyoutItemType.Button, Commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				},
				new(ContextFlyoutItemType.Button, Commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				},
				new(ContextFlyoutItemType.Button, Commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				},
				new(ContextFlyoutItemType.Button, Commands.OpenItem),
				new(ContextFlyoutItemType.Button, Commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				},
				new()
				{
					// TODO add back text and icon when https://github.com/microsoft/microsoft-ui-xaml/issues/9409 is resolved
					//Text = "OpenWith".GetLocalizedResource(),
					//OpacityIcon = new OpacityIconModel()
					//{
					//	OpacityIconStyle = "ColorIconOpenWith"
					//},
					Tag = "OpenWithOverflow",
					IsVisible = false,
					IsTextVisible = false,
					Items =
					[
						new()
						{
							Text = "Placeholder",
							IsVisibleInSearchPage = true,
						}
					],
					IsVisibleInSearchPage = true,
					IsAvailable = itemsSelected && showOpenItemWith
				},
				new(ContextFlyoutItemType.Button, Commands.OpenFileLocation),
				new(ContextFlyoutItemType.Button, Commands.OpenDirectoryInNewTabAction),
				new(ContextFlyoutItemType.Button, Commands.OpenInNewWindowItemAction),
				new(ContextFlyoutItemType.Button, Commands.OpenDirectoryInNewPaneAction),
				new()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					IsAvailable = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					IsVisibleInSearchPage = true,
					Items =
					[
						new(ContextFlyoutItemType.Button, Commands.SetAsWallpaperBackground),
						new(ContextFlyoutItemType.Button, Commands.SetAsLockscreenBackground),
						new(ContextFlyoutItemType.Button, Commands.SetAsSlideshowBackground),
					]
				},
				new(ContextFlyoutItemType.Button, Commands.RotateLeft)
				{
					IsVisible =
						!currentInstanceViewModel.IsPageTypeRecycleBin &&
						!currentInstanceViewModel.IsPageTypeZipFolder &&
						(selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				},
				new(ContextFlyoutItemType.Button, Commands.RotateRight)
				{
					IsVisible =
						!currentInstanceViewModel.IsPageTypeRecycleBin &&
						!currentInstanceViewModel.IsPageTypeZipFolder &&
						(selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				},
				new(ContextFlyoutItemType.Button, Commands.RunAsAdmin),
				new(ContextFlyoutItemType.Button, Commands.RunAsAnotherUser),
				new(ContextFlyoutItemType.Separator)
				{
					IsVisibleInSearchPage = true,
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					IsAvailable = itemsSelected
				},
				new(ContextFlyoutItemType.Button, Commands.CutItem)
				{
					IsPrimary = true,
				},
				new(ContextFlyoutItemType.Button, Commands.CopyItem)
				{
					IsPrimary = true,
				},
				new(ContextFlyoutItemType.Button, Commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				},
				new(ContextFlyoutItemType.Button, Commands.CopyPath)
				{
					IsVisible =
						UserSettingsService.GeneralSettingsService.ShowCopyPath &&
						itemsSelected &&
						!currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new(ContextFlyoutItemType.Button, Commands.CreateFolderWithSelection)
				{
					IsVisible =
						UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection &&
						itemsSelected
				},
				new(ContextFlyoutItemType.Button, Commands.CreateShortcut)
				{
					IsVisible =
						UserSettingsService.GeneralSettingsService.ShowCreateShortcut &&
						itemsSelected &&
						(!selectedItems.FirstOrDefault()?.IsShortcut ?? false) &&
						!currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new(ContextFlyoutItemType.Button, Commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				},
				new(ContextFlyoutItemType.Button, Commands.ShareItem)
				{
					IsPrimary = true
				},
				new(ContextFlyoutItemType.Button, ModifiableCommands.DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				},
				new(ContextFlyoutItemType.Button, Commands.OpenProperties)
				{
					IsPrimary = true,
					IsVisible = Commands.OpenProperties.IsExecutable
				},
				new(ContextFlyoutItemType.Button, Commands.OpenParentFolder),
				new(ContextFlyoutItemType.Button, Commands.PinFolderToSidebar)
				{
					IsVisible =
						Commands.PinFolderToSidebar.IsExecutable &&
						UserSettingsService.GeneralSettingsService.ShowPinnedSection,
				},
				new(ContextFlyoutItemType.Button, Commands.UnpinFolderFromSidebar)
				{
					IsVisible =
						Commands.UnpinFolderFromSidebar.IsExecutable &&
						UserSettingsService.GeneralSettingsService.ShowPinnedSection,
				},
				new(ContextFlyoutItemType.Button, Commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable || (x is ShortcutItem shortcutItem && FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath))) && !x.IsArchive && !x.IsItemPinnedToStart),
					IsVisibleOnShiftPressed = true,
				},
				new(ContextFlyoutItemType.Button, Commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable|| (x is ShortcutItem shortcutItem && FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath))) && !x.IsArchive && x.IsItemPinnedToStart),
					IsVisibleOnShiftPressed = true,
				},
				new()
				{
					Text = "Compress".GetLocalizedResource(),
					IsVisibleInSearchPage = true,
					OpacityIcon = new("ColorIconZip"),
					Items =
					[
						new(ContextFlyoutItemType.Button, Commands.CompressIntoArchive),
						new(ContextFlyoutItemType.Button, Commands.CompressIntoZip),
						new(ContextFlyoutItemType.Button, Commands.CompressIntoSevenZip),
					],
					IsAvailable = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && itemsSelected && CompressHelper.CanCompress(selectedItems)
				},
				new()
				{
					Text = "Extract".GetLocalizedResource(),
					IsVisibleInSearchPage = true,
					OpacityIcon = new("ColorIconZip"),
					Items =
					[
						new(ContextFlyoutItemType.Button, Commands.DecompressArchive),
						new(ContextFlyoutItemType.Button, Commands.DecompressArchiveHereSmart),
						new(ContextFlyoutItemType.Button, Commands.DecompressArchiveHere),
						new(ContextFlyoutItemType.Button, Commands.DecompressArchiveToChildFolder),
					],
					IsAvailable = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && CompressHelper.CanDecompress(selectedItems)
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					IsTextVisible = false,
					IsVisibleInSearchPage = true,
					IsAvailable = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsVisible = false,
					IsTextVisible = false,
					Items =
					[
						new()
						{
							Text = "Placeholder",
							IsVisibleInSearchPage = true,
						}
					],
					IsVisibleInSearchPage = true,
					IsAvailable = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					IsTextVisible = false,
					IsEnabled = false,
					IsAvailable = isDriveRoot
				},
				new()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					IsTextVisible = false,
					IsAvailable = isDriveRoot,
					IsEnabled = false
				},
				// Shell extensions are not available on the FTP server or in the archive,
				// but following items are intentionally added because icons in the context menu will not appear
				// unless there is at least one menu item with an icon that is not an OpacityIcon. (#12943)
				new()
				{
					ItemType = ContextFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					IsVisibleInRecycleBinPage = true,
					IsVisibleInSearchPage = true,
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					Tag = "ItemOverflow",
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					IsVisibleInRecycleBinPage = true,
					IsVisibleInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.IsAvailable).ToList();
		}

		public static List<ContextFlyoutItemModel> GetNewItemItems(BaseLayoutViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextFlyoutItemModel>()
			{
				new(ContextFlyoutItemType.Button, Commands.CreateFolder),
				new()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					IsVisibleInFtpPage = true,
					IsVisibleInArchivePage = true,
					IsEnabled = canCreateFileInPage
				},
				new(ContextFlyoutItemType.Button,Commands.CreateShortcutFromDialog),
				new(ContextFlyoutItemType.Separator),
			};

			if (canCreateFileInPage)
			{
				var cachedNewContextMenuEntries = AddItemService.GetEntries();
				cachedNewContextMenuEntries?.ForEach(i =>
				{
					if (!string.IsNullOrEmpty(i.IconBase64))
					{
						// loading the bitmaps takes a while, so this caches them
						byte[] bitmapData = Convert.FromBase64String(i.IconBase64);
						using var ms = new MemoryStream(bitmapData);
						var bitmap = new BitmapImage();
						_ = bitmap.SetSourceAsync(ms.AsRandomAccessStream());
						list.Add(new()
						{
							Text = i.Name,
							BitmapIcon = bitmap,
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
					else
					{
						list.Add(new()
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

		public static void SwapPlaceholderWithShellOption(CommandBarFlyout contextMenu, string placeholderName, ContextFlyoutItemModel? replacingItem, int position)
		{
			var placeholder = contextMenu.SecondaryCommands
				.FirstOrDefault(x => Equals((x as AppBarButton)?.Tag, placeholderName)) as AppBarButton;

			if (placeholder is not null)
				placeholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

			if (replacingItem is not null)
			{
				var (_, bitLockerCommands) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel([replacingItem]);
				contextMenu.SecondaryCommands.Insert(
					position,
					bitLockerCommands.FirstOrDefault()
				);
			}
		}
	}
}
