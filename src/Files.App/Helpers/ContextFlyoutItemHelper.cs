// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.Backend.Helpers;
using Files.Backend.Services;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage;

namespace Files.App.Helpers
{
	/// <summary>
	/// Used to create lists of ContextMenuFlyoutItems that can be used by ItemModelListToContextFlyoutHelper to create context
	/// menus and toolbars for the user.
	/// <see cref="ContextMenuFlyoutItem"/>
	/// <see cref="ContextFlyouts.ItemModelListToContextFlyoutHelper"/>
	/// </summary>
	public static class ContextFlyoutItemHelper
	{
		private static readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private static readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

		public static List<ContextMenuFlyoutItem> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, ItemViewModel? itemViewModel = null)
		{
			var menuItemsList = GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, currentInstanceViewModel: currentInstanceViewModel, itemViewModel: itemViewModel);
			menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, removeOverflowMenu: false);
			return menuItemsList;
		}

		public static Task<List<ContextMenuFlyoutItem>> GetItemContextShellCommandsAsync(string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu, CancellationToken cancellationToken)
			=> ShellContextmenuHelper.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, workingDirectory: workingDir, selectedItems: selectedItems, cancellationToken: cancellationToken);

		public static List<ContextMenuFlyoutItem> Filter(List<ContextMenuFlyoutItem> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel, bool removeOverflowMenu = true)
		{
			items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList();
			items.ForEach(x => x.Items = x.Items?.Where(y => Check(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList());

			var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
			if (overflow is not null)
			{
				if (!shiftPressed && userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextMenuFlyoutItemType.Separator)
						overflow.Items.Add(new ContextMenuFlyoutItem { ItemType = ContextMenuFlyoutItemType.Separator });

					items = items.Except(overflowItems).ToList();
					overflow.Items.AddRange(overflowItems);
				}

				// remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool Check(ContextMenuFlyoutItem item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems)
		{
			return (item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin)
				&& (item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults)
				&& (item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp)
				&& (item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder)
				&& (!item.SingleItemOnly || selectedItems.Count == 1)
				&& item.ShowItem;
		}

		public static List<ContextMenuFlyoutItem> GetBaseItemMenuItems(
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

			return new List<ContextMenuFlyoutItem>()
			{
				new ContextMenuFlyoutItem()
				{
					Text = "LayoutMode".GetLocalizedResource(),
					Glyph = "\uE152",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItem>
					{
						new ContextMenuFlyoutItemBuilder(commands.LayoutDetails)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.LayoutTiles)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.LayoutGridSmall)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.LayoutGridMedium)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.LayoutGridLarge)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.LayoutColumns)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.LayoutAdaptive)
						{
							IsToggle = true
						}.Build(),
					},
				},
				new ContextMenuFlyoutItem()
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
					Items = new List<ContextMenuFlyoutItem>
					{
						new ContextMenuFlyoutItemBuilder(commands.SortByName)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortByDateModified)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortByDateCreated)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortByType)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortBySize)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortByTag)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortByDateDeleted)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItem
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemBuilder(commands.SortAscending)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.SortDescending)
						{
							IsToggle = true
						}.Build(),
					},
				},
				new ContextMenuFlyoutItem()
				{
					Text = "NavToolbarGroupByRadioButtons/Text".GetLocalizedResource(),
					Glyph = "\uF168",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItem>
					{
						new ContextMenuFlyoutItemBuilder(commands.GroupByNone)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.GroupByName)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItem()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new List<ContextMenuFlyoutItem>
							{
								new ContextMenuFlyoutItemBuilder(commands.GroupByDateModifiedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemBuilder(commands.GroupByDateModifiedMonth)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new ContextMenuFlyoutItem()
						{
							Text = "DateCreated".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new List<ContextMenuFlyoutItem>
							{
								new ContextMenuFlyoutItemBuilder(commands.GroupByDateCreatedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemBuilder(commands.GroupByDateCreatedMonth)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new ContextMenuFlyoutItemBuilder(commands.GroupByType)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.GroupBySize)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.GroupBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.GroupByTag)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.GroupByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItem()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							ShowInRecycleBin = true,
							IsHidden = !currentInstanceViewModel.IsPageTypeRecycleBin,
							Items = new List<ContextMenuFlyoutItem>
							{
								new ContextMenuFlyoutItemBuilder(commands.GroupByDateDeletedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemBuilder(commands.GroupByDateDeletedMonth)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new ContextMenuFlyoutItemBuilder(commands.GroupByFolderPath)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItem
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemBuilder(commands.GroupAscending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.GroupDescending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
					},
				},
				new ContextMenuFlyoutItemBuilder(commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemBuilder(commands.AddItem)
				{
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					IsVisible = !itemsSelected
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.FormatDrive).Build(),
				new ContextMenuFlyoutItemBuilder(commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.OpenItem).Build(),
				new ContextMenuFlyoutItemBuilder(commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				}.Build(),
				new ContextMenuFlyoutItem()
				{
					Text = "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith"
					},
					Tag = "OpenWithOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<ContextMenuFlyoutItem>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new ContextMenuFlyoutItemBuilder(commands.OpenFileLocation).Build(),
				new ContextMenuFlyoutItem()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab"
					},
					Command = commandsViewModel.OpenDirectoryInNewTabCommand,
					ShowItem = itemsSelected && selectedItems.Count < 5 && areAllItemsFolders && userSettingsService.GeneralSettingsService.ShowOpenInNewTab,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItem()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow"
					},
					Command = commandsViewModel.OpenInNewWindowItemCommand,
					ShowItem = itemsSelected && selectedItems.Count < 5 && areAllItemsFolders && userSettingsService.GeneralSettingsService.ShowOpenInNewWindow,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItem()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
					ShowItem = itemsSelected && userSettingsService.GeneralSettingsService.ShowOpenInNewPane && areAllItemsFolders,
					SingleItemOnly = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItem()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					ShowInSearchPage = true,
					Items = new List<ContextMenuFlyoutItem>
					{
						new ContextMenuFlyoutItemBuilder(commands.SetAsWallpaperBackground).Build(),
						new ContextMenuFlyoutItemBuilder(commands.SetAsLockscreenBackground).Build(),
						new ContextMenuFlyoutItemBuilder(commands.SetAsSlideshowBackground).Build(),
					}
				},
				new ContextMenuFlyoutItemBuilder(commands.RotateLeft)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.RotateRight)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.RunAsAdmin).Build(),
				new ContextMenuFlyoutItemBuilder(commands.RunAsAnotherUser).Build(),
				new ContextMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemBuilder(commands.CutItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.CopyItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.CopyPath)
				{
					IsVisible = itemsSelected && selectedItems.Count == 1 && !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItem()
				{
					Text = "BaseLayoutItemContextFlyoutCreateFolderWithSelection/Text".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconNewFolder",
					},
					Command = commandsViewModel.CreateFolderWithSelection,
					ShowItem = itemsSelected,
				},
				new ContextMenuFlyoutItemBuilder(commands.CreateShortcut)
				{
					IsVisible = itemsSelected && (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.ShareItem)
				{
					IsPrimary = true
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItem()
				{
					Text = "Properties".GetLocalizedResource(),
					IsPrimary = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = commandsViewModel.ShowPropertiesCommand,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
				},
				new ContextMenuFlyoutItemBuilder(commands.OpenParentFolder).Build(),
				new ContextMenuFlyoutItemBuilder(commands.PinItemToFavorites)
				{
					IsVisible = commands.PinItemToFavorites.IsExecutable && userSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.UnpinItemFromFavorites)
				{
					IsVisible = commands.UnpinItemFromFavorites.IsExecutable && userSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextMenuFlyoutItemBuilder(commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextMenuFlyoutItem
				{
					Text = "Archive".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new List<ContextMenuFlyoutItem>
					{
						new ContextMenuFlyoutItemBuilder(commands.DecompressArchive)
						{
							IsVisible = ArchiveHelpers.CanDecompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.DecompressArchiveHere)
						{
							IsVisible = ArchiveHelpers.CanDecompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.DecompressArchiveToChildFolder)
						{
							IsVisible = ArchiveHelpers.CanDecompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItem
						{
							ShowItem = canDecompress && canCompress,
							ItemType = ContextMenuFlyoutItemType.Separator,
						},
						new ContextMenuFlyoutItemBuilder(commands.CompressIntoArchive)
						{
							IsVisible = ArchiveHelpers.CanCompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.CompressIntoZip)
						{
							IsVisible = ArchiveHelpers.CanCompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemBuilder(commands.CompressIntoSevenZip)
						{
							IsVisible = ArchiveHelpers.CanCompress(selectedItems)
						}.Build(),
					},
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItem()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					CollapseLabel = true,
					ShowInSearchPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItem()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<ContextMenuFlyoutItem>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					ShowInSearchPage = true,
				},
				new ContextMenuFlyoutItem()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItem>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.ShowItem).ToList();
		}

		public static List<ContextMenuFlyoutItem> GetNewItemItems(BaseLayoutCommandsViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextMenuFlyoutItem>()
			{
				new ContextMenuFlyoutItemBuilder(commands.CreateFolder).Build(),
				new ContextMenuFlyoutItem()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					CommandParameter = null,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new ContextMenuFlyoutItemBuilder(commands.CreateShortcutFromDialog).Build(),
				new ContextMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
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
						list.Add(new ContextMenuFlyoutItem()
						{
							Text = i.Name,
							BitmapIcon = bitmap,
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
					else
					{
						list.Add(new ContextMenuFlyoutItem()
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