// Copyright (c) 2023 Files Community
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

		// Static Methods

		public static List<ContextFlyoutItemModel> GenerateContextFlyout(
			BaseLayoutViewModel commandsViewModel,
			SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
			List<ListedItem> selectedItems,
			CurrentInstanceViewModel currentInstanceViewModel,
			ItemViewModel? itemViewModel = null)
		{
			bool itemsSelected = itemViewModel is null;

			bool canDecompress =
				selectedItems.Count != 0 &&
				selectedItems.All(x => x.IsArchive) ||
				selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && FileExtensionHelpers.IsZipFile(x.FileExtension));

			bool canCompress = !canDecompress || selectedItems.Count > 1;

			bool showOpenItemWith =
				selectedItems.All(i =>
					(i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) ||
					(i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));

			bool areAllItemsFolders = selectedItems.All(i => i.PrimaryItemAttribute == StorageItemTypes.Folder);

			bool isFirstFileExecutable = FileExtensionHelpers.IsExecutableFile(selectedItems.FirstOrDefault()?.FileExtension);

			string newArchiveName =
				Path.GetFileName(
					selectedItems.Count is 1
						? selectedItems[0].ItemPath
						: Path.GetDirectoryName(selectedItems[0].ItemPath))
				?? string.Empty;

			bool isDriveRoot =
				itemViewModel?.CurrentFolder is not null &&
				(itemViewModel.CurrentFolder.ItemPath == Path.GetPathRoot(itemViewModel.CurrentFolder.ItemPath));

			return new List<ContextFlyoutItemModel>()
			{
				new()
				{
					Text = "LayoutMode".GetLocalizedResource(),
					Glyph = "\uE152",
					IsAvailable = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new()
					{
						new(Commands.LayoutDetails)
						{
							IsToggle = true
						},
						new(Commands.LayoutTiles)
						{
							IsToggle = true
						},
						new(Commands.LayoutList)
						{
							IsToggle = true
						},
						new(Commands.LayoutGridSmall)
						{
							IsToggle = true
						},
						new(Commands.LayoutGridMedium)
						{
							IsToggle = true
						},
						new(Commands.LayoutGridLarge)
						{
							IsToggle = true
						},
						new(Commands.LayoutColumns)
						{
							IsToggle = true
						},
						new(Commands.LayoutAdaptive)
						{
							IsToggle = true
						},
					},
				},
				new()
				{
					Text = "SortBy".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconSort",
					},
					IsAvailable = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new()
					{
						new(Commands.SortByName)
						{
							IsToggle = true
						},
						new(Commands.SortByDateModified)
						{
							IsToggle = true
						},
						new(Commands.SortByDateCreated)
						{
							IsToggle = true
						},
						new(Commands.SortByType)
						{
							IsToggle = true
						},
						new(Commands.SortBySize)
						{
							IsToggle = true
						},
						new(Commands.SortBySyncStatus)
						{
							IsToggle = true
						},
						new(Commands.SortByTag)
						{
							IsToggle = true
						},
						new(Commands.SortByPath)
						{
							IsToggle = true
						},
						new(Commands.SortByOriginalFolder)
						{
							IsToggle = true
						},
						new(Commands.SortByDateDeleted)
						{
							IsToggle = true
						},
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new(Commands.SortAscending)
						{
							IsToggle = true
						},
						new(Commands.SortDescending)
						{
							IsToggle = true
						},
					},
				},
				new()
				{
					Text = "GroupBy".GetLocalizedResource(),
					Glyph = "\uF168",
					IsAvailable = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new()
					{
						new(Commands.GroupByNone)
						{
							IsToggle = true
						},
						new(Commands.GroupByName)
						{
							IsToggle = true
						},
						new()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new()
							{
								new(Commands.GroupByDateModifiedYear)
								{
									IsToggle = true
								},
								new(Commands.GroupByDateModifiedMonth)
								{
									IsToggle = true
								},
								new(Commands.GroupByDateModifiedDay)
								{
									IsToggle = true
								},
							},
						},
						new()
						{
							Text = "DateCreated".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new()
							{
								new(Commands.GroupByDateCreatedYear)
								{
									IsToggle = true
								},
								new(Commands.GroupByDateCreatedMonth)
								{
									IsToggle = true
								},
								new(Commands.GroupByDateCreatedDay)
								{
									IsToggle = true
								},
							},
						},
						new(Commands.GroupByType)
						{
							IsToggle = true
						},
						new(Commands.GroupBySize)
						{
							IsToggle = true
						},
						new(Commands.GroupBySyncStatus)
						{
							IsToggle = true
						},
						new(Commands.GroupByTag)
						{
							IsToggle = true
						},
						new(Commands.GroupByOriginalFolder)
						{
							IsToggle = true
						},
						new()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							ShowInRecycleBin = true,
							IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin,
							Items = new()
							{
								new(Commands.GroupByDateDeletedYear)
								{
									IsToggle = true
								},
								new(Commands.GroupByDateDeletedMonth)
								{
									IsToggle = true
								},
								new(Commands.GroupByDateDeletedDay)
								{
									IsToggle = true
								},
							},
						},
						new(Commands.GroupByFolderPath)
						{
							IsToggle = true
						},
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new(Commands.GroupAscending)
						{
							IsToggle = true,
							IsVisible = true
						},
						new(Commands.GroupDescending)
						{
							IsToggle = true,
							IsVisible = true
						},
					},
				},
				new(Commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsAvailable = !itemsSelected
				},
				new()
				{
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = Commands.AddItem.Glyph.OpacityStyle
					},
					Text = Commands.AddItem.Label,
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					IsAvailable = !itemsSelected,
					ShowInFtpPage = true
				},
				new(Commands.FormatDrive),
				new(Commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				},
				new(Commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				},
				new(Commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				},
				new(Commands.OpenItem),
				new(Commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				},
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith"
					},
					Tag = "OpenWithOverflow",
					IsVisible = false,
					CollapseLabel = true,
					Items = new()
					{
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					IsAvailable = itemsSelected && showOpenItemWith
				},
				new(Commands.OpenFileLocation),
				new(Commands.OpenDirectoryInNewTabAction),
				new(Commands.OpenInNewWindowItemAction),
				new(Commands.OpenDirectoryInNewPaneAction),
				new()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					IsAvailable = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					ShowInSearchPage = true,
					Items = new()
					{
						new(Commands.SetAsWallpaperBackground),
						new(Commands.SetAsLockscreenBackground),
						new(Commands.SetAsSlideshowBackground),
					}
				},
				new(Commands.RotateLeft)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				},
				new(Commands.RotateRight)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				},
				new(Commands.RunAsAdmin),
				new(Commands.RunAsAnotherUser),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsAvailable = itemsSelected
				},
				new(Commands.CutItem)
				{
					IsPrimary = true,
				},
				new(Commands.CopyItem)
				{
					IsPrimary = true,
				},
				new(Commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				},
				new(Commands.CopyPath)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCopyPath
						&& itemsSelected
						&&!currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new(Commands.CreateFolderWithSelection)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection && itemsSelected
				},
				new(Commands.CreateShortcut)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateShortcut
						&& itemsSelected
						&& (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				},
				new(Commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				},
				new(Commands.ShareItem)
				{
					IsPrimary = true
				},
				new(ModifiableCommands.DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				},
				new(Commands.OpenProperties)
				{
					IsPrimary = true,
					IsVisible = Commands.OpenProperties.IsExecutable
				},
				new(Commands.OpenParentFolder),
				new(Commands.PinItemToFavorites)
				{
					IsVisible = Commands.PinItemToFavorites.IsExecutable && UserSettingsService.GeneralSettingsService.ShowFavoritesSection,
				},
				new(Commands.UnpinItemFromFavorites)
				{
					IsVisible = Commands.UnpinItemFromFavorites.IsExecutable && UserSettingsService.GeneralSettingsService.ShowFavoritesSection,
				},
				new(Commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				},
				new(Commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
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
					Items = new List<ContextFlyoutItemModel>
					{
						new(Commands.CompressIntoArchive),
						new(Commands.CompressIntoZip),
						new(Commands.CompressIntoSevenZip),
					},
					IsAvailable = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && itemsSelected && CompressHelper.CanCompress(selectedItems)
				},
				new()
				{
					Text = "Extract".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new()
					{
						new(Commands.DecompressArchive),
						new(Commands.DecompressArchiveHereSmart),
						new(Commands.DecompressArchiveHere),
						new(Commands.DecompressArchiveToChildFolder),
					},
					IsAvailable = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && CompressHelper.CanDecompress(selectedItems)
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					CollapseLabel = true,
					ShowInSearchPage = true,
					IsAvailable = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsVisible = false,
					CollapseLabel = true,
					Items = new()
					{
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					IsAvailable = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					CollapseLabel = true,
					IsEnabled = false,
					IsAvailable = isDriveRoot
				},
				new()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					CollapseLabel = true,
					IsAvailable = isDriveRoot,
					IsEnabled = false
				},
				// Shell extensions are not available on the FTP server or in the archive,
				// but following items are intentionally added because icons in the context menu will not appear
				// unless there is at least one menu item with an icon that is not an OpacityIcon. (#12943)
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.IsAvailable).ToList();
		}

		public static List<ContextFlyoutItemModel> GetNewItemItems(BaseLayoutViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextFlyoutItemModel>()
			{
				new(Commands.CreateFolder),
				new()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new(Commands.CreateShortcutFromDialog),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
				}
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

		public static List<ContextFlyoutItemModel> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, BaseLayoutViewModel commandsViewModel, bool shiftPressed, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, ItemViewModel? itemViewModel = null)
		{
			var menuItemsList =
				GenerateContextFlyout(
					commandsViewModel,
					selectedItemsPropertiesViewModel,
					selectedItems,
					currentInstanceViewModel,
					itemViewModel);

			menuItemsList =
				FillContextFlyout(
					menuItemsList,
					selectedItems,
					shiftPressed,
					currentInstanceViewModel,
					false);

			return menuItemsList;
		}

		public static Task<List<ContextFlyoutItemModel>> GetItemContextShellCommandsAsync(string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu, CancellationToken cancellationToken)
		{
			return ShellContextFlyoutFactory.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, workingDirectory: workingDir, selectedItems: selectedItems, cancellationToken: cancellationToken);
		}

		public static List<ContextFlyoutItemModel> FillContextFlyout(List<ContextFlyoutItemModel> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel, bool removeOverflowMenu = true)
		{
			items = items.Where(x => CheckVisibility(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList();
			items.ForEach(x => x.Items = x.Items?.Where(y => CheckVisibility(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList());

			var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
			if (overflow is not null)
			{
				if (!shiftPressed && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextMenuFlyoutItemType.Separator)
						overflow.Items.Add(new() { ItemType = ContextMenuFlyoutItemType.Separator });

					items = items.Except(overflowItems).ToList();
					overflow.Items.AddRange(overflowItems);
				}

				// remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool CheckVisibility(ContextFlyoutItemModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems)
		{
			return
				(item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) &&
				(item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) &&
				(item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp) &&
				(item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder) &&
				(!item.SingleItemOnly || selectedItems.Count == 1) &&
				item.IsAvailable;
		}

		public static void SwapPlaceholderWithShellOption(CommandBarFlyout contextMenu, string placeholderName, ContextFlyoutItemModel? replacingItem, int position)
		{
			var placeholder = contextMenu.SecondaryCommands
				.Where(x => Equals((x as AppBarButton)?.Tag, placeholderName))
				.FirstOrDefault() as AppBarButton;

			if (placeholder is not null)
				placeholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

			if (replacingItem is not null)
			{
				var (_, bitLockerCommands) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(new List<ContextFlyoutItemModel>() { replacingItem });
				contextMenu.SecondaryCommands.Insert(
					position,
					bitLockerCommands.FirstOrDefault()
				);
			}
		}
	}
}
