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

			var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
			if (overflow is not null)
			{
				if (!shiftPressed && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextMenuFlyoutItemType.Separator)
						overflow.Items.Add(new ContextFlyoutItemModel { ItemType = ContextMenuFlyoutItemType.Separator });

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
					Text = "LayoutMode".GetLocalizedResource(),
					Glyph = "\uE152",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextFlyoutItemModel>
					{
						new ContextFlyoutItemModelBuilder(Commands.LayoutDetails)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutTiles)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutList)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutGridSmall)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutGridMedium)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutGridLarge)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutColumns)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.LayoutAdaptive)
						{
							IsToggle = true
						}.Build(),
					},
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
					Items = new List<ContextFlyoutItemModel>
					{
						new ContextFlyoutItemModelBuilder(Commands.SortByName)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByDateModified)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByDateCreated)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByType)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortBySize)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByTag)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByPath)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortByDateDeleted)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModel
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextFlyoutItemModelBuilder(Commands.SortAscending)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.SortDescending)
						{
							IsToggle = true
						}.Build(),
					},
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
					Items = new List<ContextFlyoutItemModel>
					{
						new ContextFlyoutItemModelBuilder(Commands.GroupByNone)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.GroupByName)
						{
							IsToggle = true
						}.Build(),
						new()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new List<ContextFlyoutItemModel>
							{
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateModifiedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateModifiedMonth)
								{
									IsToggle = true
								}.Build(),
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateModifiedDay)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new()
						{
							Text = "DateCreated".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new List<ContextFlyoutItemModel>
							{
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateCreatedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateCreatedMonth)
								{
									IsToggle = true
								}.Build(),
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateCreatedDay)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new ContextFlyoutItemModelBuilder(Commands.GroupByType)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.GroupBySize)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.GroupBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.GroupByTag)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.GroupByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							ShowInRecycleBin = true,
							IsHidden = !currentInstanceViewModel.IsPageTypeRecycleBin,
							Items = new List<ContextFlyoutItemModel>
							{
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateDeletedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateDeletedMonth)
								{
									IsToggle = true
								}.Build(),
								new ContextFlyoutItemModelBuilder(Commands.GroupByDateDeletedDay)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new ContextFlyoutItemModelBuilder(Commands.GroupByFolderPath)
						{
							IsToggle = true
						}.Build(),
						new ContextFlyoutItemModel
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextFlyoutItemModelBuilder(Commands.GroupAscending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
						new ContextFlyoutItemModelBuilder(Commands.GroupDescending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
					},
				},
				new ContextFlyoutItemModelBuilder(Commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				}.Build(),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
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
				new ContextFlyoutItemModelBuilder(Commands.FormatDrive).Build(),
				new ContextFlyoutItemModelBuilder(Commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenItem).Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				}.Build(),
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith"
					},
					Tag = "OpenWithOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<ContextFlyoutItemModel>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new ContextFlyoutItemModelBuilder(Commands.OpenFileLocation).Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenDirectoryInNewTabAction).Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenInNewWindowItemAction).Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenDirectoryInNewPaneAction).Build(),
				new()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					ShowInSearchPage = true,
					Items = new List<ContextFlyoutItemModel>
					{
						new ContextFlyoutItemModelBuilder(Commands.SetAsWallpaperBackground).Build(),
						new ContextFlyoutItemModelBuilder(Commands.SetAsLockscreenBackground).Build(),
						new ContextFlyoutItemModelBuilder(Commands.SetAsSlideshowBackground).Build(),
					}
				},
				new ContextFlyoutItemModelBuilder(Commands.RotateLeft)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.RotateRight)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.RunAsAdmin).Build(),
				new ContextFlyoutItemModelBuilder(Commands.RunAsAnotherUser).Build(),
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextFlyoutItemModelBuilder(Commands.CutItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.CopyItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.CopyPath)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCopyPath
						&& itemsSelected
						&&!currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.CreateFolderWithSelection)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection && itemsSelected
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.CreateShortcut)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateShortcut
						&& itemsSelected
						&& (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.ShareItem)
				{
					IsPrimary = true
				}.Build(),
				new ContextFlyoutItemModelBuilder(ModifiableCommands.DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenProperties)
				{
					IsPrimary = true,
					IsVisible = Commands.OpenProperties.IsExecutable
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.OpenParentFolder).Build(),
				new ContextFlyoutItemModelBuilder(Commands.PinItemToFavorites)
				{
					IsVisible = Commands.PinItemToFavorites.IsExecutable && UserSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.UnpinItemFromFavorites)
				{
					IsVisible = Commands.UnpinItemFromFavorites.IsExecutable && UserSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextFlyoutItemModelBuilder(Commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextFlyoutItemModel
				{
					Text = "Compress".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new List<ContextFlyoutItemModel>
					{
						new ContextFlyoutItemModelBuilder(Commands.CompressIntoArchive).Build(),
						new ContextFlyoutItemModelBuilder(Commands.CompressIntoZip).Build(),
						new ContextFlyoutItemModelBuilder(Commands.CompressIntoSevenZip).Build(),
					},
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && itemsSelected && CompressHelper.CanCompress(selectedItems)
				},
				new ContextFlyoutItemModel
				{
					Text = "Extract".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new List<ContextFlyoutItemModel>
					{
						new ContextFlyoutItemModelBuilder(Commands.DecompressArchive).Build(),
						new ContextFlyoutItemModelBuilder(Commands.DecompressArchiveHereSmart).Build(),
						new ContextFlyoutItemModelBuilder(Commands.DecompressArchiveHere).Build(),
						new ContextFlyoutItemModelBuilder(Commands.DecompressArchiveToChildFolder).Build(),
					},
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
					Items = new List<ContextFlyoutItemModel>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
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
					Items = new List<ContextFlyoutItemModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.ShowItem).ToList();
		}

		public static List<ContextFlyoutItemModel> GetNewItemItems(BaseLayoutViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextFlyoutItemModel>()
			{
				new ContextFlyoutItemModelBuilder(Commands.CreateFolder).Build(),
				new()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new ContextFlyoutItemModelBuilder(Commands.CreateShortcutFromDialog).Build(),
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
