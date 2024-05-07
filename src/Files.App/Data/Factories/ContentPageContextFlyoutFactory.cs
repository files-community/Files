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

			var overflow = items.FirstOrDefault(x => x.ID == "ItemOverflow");
			if (overflow is not null)
			{
				if (!shiftPressed && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextFlyoutItemType.Separator)
						overflow.Items.Add(new(ContextFlyoutItemType.Separator));

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
				(item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) &&
				(item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) &&
				(item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp) &&
				(item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder) &&
				(!item.SingleItemOnly || selectedItems.Count == 1) &&
				item.ShowItem;
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
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
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
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconSort",
					},
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
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
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new(ContextFlyoutItemType.Toggle, Commands.SortAscending),
						new(ContextFlyoutItemType.Toggle, Commands.SortDescending),
					],
				},
				new()
				{
					Text = "GroupBy".GetLocalizedResource(),
					Glyph = "\uF168",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items =
					[
						new(ContextFlyoutItemType.Toggle, Commands.GroupByNone),
						new(ContextFlyoutItemType.Toggle, Commands.GroupByName),
						new()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
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
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
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
							ShowInRecycleBin = true,
							IsHidden = !currentInstanceViewModel.IsPageTypeRecycleBin,
							Items =
							[
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateDeletedYear),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateDeletedMonth),
								new(ContextFlyoutItemType.Toggle, Commands.GroupByDateDeletedDay),
							],
						},
						new(ContextFlyoutItemType.Toggle, Commands.GroupByFolderPath),
						new()
						{
							ItemType = ContextFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new(ContextFlyoutItemType.Toggle, Commands.GroupAscending),
						new(ContextFlyoutItemType.Toggle, Commands.GroupDescending),
					],
				},
				new(ContextFlyoutItemType.Button, Commands.RefreshItems)
				{
					ShowItem = !itemsSelected,
				},
				new()
				{
					ItemType = ContextFlyoutItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new()
				{
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = Commands.AddItem.Glyph.OpacityStyle
					},
					Text = Commands.AddItem.Label,
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					ShowItem = !itemsSelected,
					ShowInFtpPage = true
				},
				new(ContextFlyoutItemType.Button, Commands.FormatDrive),
				new(ContextFlyoutItemType.Button, Commands.EmptyRecycleBin)
				{
					ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				},
				new(ContextFlyoutItemType.Button, Commands.RestoreAllRecycleBin)
				{
					ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				},
				new(ContextFlyoutItemType.Button, Commands.RestoreRecycleBin)
				{
					ShowItem = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
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
					IsHidden = true,
					CollapseLabel = true,
					Items =
					[
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					],
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new(ContextFlyoutItemType.Button, Commands.OpenFileLocation),
				new(ContextFlyoutItemType.Button, Commands.OpenDirectoryInNewTabAction),
				new(ContextFlyoutItemType.Button, Commands.OpenInNewWindowItemAction),
				new(ContextFlyoutItemType.Button, Commands.OpenDirectoryInNewPaneAction),
				new()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					ShowInSearchPage = true,
					Items =
					[
						new(ContextFlyoutItemType.Button, Commands.SetAsWallpaperBackground),
						new(ContextFlyoutItemType.Button, Commands.SetAsLockscreenBackground),
						new(ContextFlyoutItemType.Button, Commands.SetAsSlideshowBackground),
						new(ContextFlyoutItemType.Button, Commands.SetAsAppBackground),
					]
				},
				new(ContextFlyoutItemType.Button, Commands.RotateLeft)
				{
					ShowItem = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				},
				new(ContextFlyoutItemType.Button, Commands.RotateRight)
				{
					ShowItem = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				},
				new(ContextFlyoutItemType.Button, Commands.RunAsAdmin),
				new(ContextFlyoutItemType.Button, Commands.RunAsAnotherUser),
				new(ContextFlyoutItemType.Separator)
				{
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
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
					ShowItem = true,
				},
				new(ContextFlyoutItemType.Button, Commands.CopyPath)
				{
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCopyPath
						&& itemsSelected
						&&!currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new(ContextFlyoutItemType.Button, Commands.CreateFolderWithSelection)
				{
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection && itemsSelected
				},
				new(ContextFlyoutItemType.Button, Commands.CreateShortcut)
				{
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCreateShortcut
						&& itemsSelected
						&& (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new(ContextFlyoutItemType.Button, Commands.Rename)
				{
					IsPrimary = true,
					ShowItem = itemsSelected
				},
				new(ContextFlyoutItemType.Button, Commands.ShareItem)
				{
					IsPrimary = true
				},
				new(ContextFlyoutItemType.Button, ModifiableCommands.DeleteItem)
				{
					ShowItem = itemsSelected,
					IsPrimary = true,
				},
				new(ContextFlyoutItemType.Button, Commands.OpenProperties)
				{
					IsPrimary = true,
					ShowItem = Commands.OpenProperties.IsExecutable
				},
				new(ContextFlyoutItemType.Button, Commands.OpenParentFolder),
				new(ContextFlyoutItemType.Button, Commands.PinFolderToSidebar)
				{
					ShowItem = Commands.PinFolderToSidebar.IsExecutable && UserSettingsService.GeneralSettingsService.ShowPinnedSection,
				},
				new(ContextFlyoutItemType.Button, Commands.UnpinFolderFromSidebar)
				{
					ShowItem = Commands.UnpinFolderFromSidebar.IsExecutable && UserSettingsService.GeneralSettingsService.ShowPinnedSection,
				},
				new(ContextFlyoutItemType.Button, Commands.PinToStart)
				{
					ShowItem = selectedItems.All(x => (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable || (x is ShortcutItem shortcutItem && FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath))) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				},
				new(ContextFlyoutItemType.Button, Commands.UnpinFromStart)
				{
					ShowItem = selectedItems.All(x => (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable|| (x is ShortcutItem shortcutItem && FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath))) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowOnShift = true,
				},
				new()
				{
					Text = "Compress".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items =
					[
						new(ContextFlyoutItemType.Button, Commands.CompressIntoArchive),
						new(ContextFlyoutItemType.Button, Commands.CompressIntoZip),
						new(ContextFlyoutItemType.Button, Commands.CompressIntoSevenZip),
					],
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && itemsSelected && CompressHelper.CanCompress(selectedItems)
				},
				new()
				{
					Text = "Extract".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items =
					[
						new(ContextFlyoutItemType.Button, Commands.DecompressArchive),
						new(ContextFlyoutItemType.Button, Commands.DecompressArchiveHereSmart),
						new(ContextFlyoutItemType.Button, Commands.DecompressArchiveHere),
						new(ContextFlyoutItemType.Button, Commands.DecompressArchiveToChildFolder),
					],
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && CompressHelper.CanDecompress(selectedItems)
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					CollapseLabel = true,
					ShowInSearchPage = true,
					ShowItem = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items =
					[
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					],
					ShowInSearchPage = true,
					ShowItem = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					CollapseLabel = true,
					IsEnabled = false,
					ShowItem = isDriveRoot
				},
				new()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					CollapseLabel = true,
					ShowItem = isDriveRoot,
					IsEnabled = false
				},
				// Shell extensions are not available on the FTP server or in the archive,
				// but following items are intentionally added because icons in the context menu will not appear
				// unless there is at least one menu item with an icon that is not an OpacityIcon. (#12943)
				new()
				{
					ItemType = ContextFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowItem = true,
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					IsEnabled = false,
					ShowItem = true,
				},
			}.Where(x => x.ShowItem).ToList();
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
					ShowItem = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new(ContextFlyoutItemType.Button, Commands.CreateShortcutFromDialog)
				{
					ShowItem = true,
				},
				new(ContextFlyoutItemType.Separator)
				{
					ShowItem = true,
				},
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
							ShowItem = true,
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
							ShowItem = true,
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
