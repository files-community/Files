// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.LayoutModes;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage;

namespace Files.App.Data.Factories
{
	public static class ContentCustomMenuFlyoutFactory
	{
		private static IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>()!;
		private static IModifiableCommandManager ModifiableCommands { get; } = Ioc.Default.GetRequiredService<IModifiableCommandManager>();
		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static IAddItemService AddItemService { get; } = Ioc.Default.GetRequiredService<IAddItemService>();
		private static ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public static List<CustomMenuFlyoutItem> GetContextMenuItemsWithoutShellItems(
			CurrentInstanceViewModel currentInstanceViewModel,
			List<ListedItem> selectedItems,
			BaseLayoutViewModel commandsViewModel,
			bool shiftPressed,
			SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
			ItemViewModel? itemViewModel = null)
		{
			var menuItemsList =
				ContentCustomMenuFlyoutFactory.GetContextMenuItems(
					commandsViewModel,
					selectedItemsPropertiesViewModel,
					selectedItems,
					currentInstanceViewModel,
					itemViewModel);

			menuItemsList = ContentCustomMenuFlyoutFactory.UpdateOverflowItems(
				menuItemsList,
				selectedItems,
				shiftPressed,
				currentInstanceViewModel,
				false);

			return menuItemsList;
		}

		private static List<CustomMenuFlyoutItem> GetContextMenuItems(
			BaseLayoutViewModel commandsViewModel,
			SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
			List<ListedItem> selectedItems,
			CurrentInstanceViewModel currentInstanceViewModel,
			ItemViewModel? itemViewModel = null)
		{
			bool itemsSelected = itemViewModel is null;

			bool canDecompress =
				selectedItems.Any() &&
				selectedItems.All(x => x.IsArchive) ||
				selectedItems.All(x =>
					x.PrimaryItemAttribute == StorageItemTypes.File &&
					FileExtensionHelpers.IsZipFile(x.FileExtension));

			bool canCompress = !canDecompress || selectedItems.Count > 1;

			bool showOpenItemWith =
				selectedItems.All(i =>
					(i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) ||
					(i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));

			bool areAllItemsFolders = selectedItems.All(i => i.PrimaryItemAttribute == StorageItemTypes.Folder);

			bool isFirstFileExecutable = FileExtensionHelpers.IsExecutableFile(selectedItems.FirstOrDefault()?.FileExtension);

			string newArchiveName =
				Path.GetFileName(selectedItems.Count is 1
					? selectedItems[0].ItemPath
					: Path.GetDirectoryName(selectedItems[0].ItemPath))
				?? string.Empty;

			bool isDriveRoot =
				itemViewModel?.CurrentFolder is not null &&
				(itemViewModel.CurrentFolder.ItemPath == Path.GetPathRoot(itemViewModel.CurrentFolder.ItemPath));

			var tagItems = GetFileTagItems(commandsViewModel, selectedItems);

			return new List<CustomMenuFlyoutItem>()
			{
				new CustomMenuFlyoutItem()
				{
					Text = "LayoutMode".GetLocalizedResource(),
					Glyph = "\uE152",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<CustomMenuFlyoutItem>
					{
						new CustomMenuFlyoutItemBuilder(Commands.LayoutDetails)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.LayoutTiles)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.LayoutGridSmall)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.LayoutGridMedium)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.LayoutGridLarge)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.LayoutColumns)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.LayoutAdaptive)
						{
							IsToggle = true
						}.Build(),
					},
				},
				new CustomMenuFlyoutItem()
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
					Items = new List<CustomMenuFlyoutItem>
					{
						new CustomMenuFlyoutItemBuilder(Commands.SortByName)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByDateModified)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByDateCreated)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByType)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortBySize)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByTag)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByPath)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortByDateDeleted)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItem
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new CustomMenuFlyoutItemBuilder(Commands.SortAscending)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SortDescending)
						{
							IsToggle = true
						}.Build(),
					},
				},
				new CustomMenuFlyoutItem()
				{
					Text = "NavToolbarGroupByRadioButtons/Text".GetLocalizedResource(),
					Glyph = "\uF168",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<CustomMenuFlyoutItem>
					{
						new CustomMenuFlyoutItemBuilder(Commands.GroupByNone)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.GroupByName)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItem()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new List<CustomMenuFlyoutItem>
							{
								new CustomMenuFlyoutItemBuilder(Commands.GroupByDateModifiedYear)
								{
									IsToggle = true
								}.Build(),
								new CustomMenuFlyoutItemBuilder(Commands.GroupByDateModifiedMonth)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new CustomMenuFlyoutItem()
						{
							Text = "DateCreated".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items = new List<CustomMenuFlyoutItem>
							{
								new CustomMenuFlyoutItemBuilder(Commands.GroupByDateCreatedYear)
								{
									IsToggle = true
								}.Build(),
								new CustomMenuFlyoutItemBuilder(Commands.GroupByDateCreatedMonth)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new CustomMenuFlyoutItemBuilder(Commands.GroupByType)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.GroupBySize)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.GroupBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.GroupByTag)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.GroupByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItem()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							ShowInRecycleBin = true,
							IsHidden = !currentInstanceViewModel.IsPageTypeRecycleBin,
							Items = new List<CustomMenuFlyoutItem>
							{
								new CustomMenuFlyoutItemBuilder(Commands.GroupByDateDeletedYear)
								{
									IsToggle = true
								}.Build(),
								new CustomMenuFlyoutItemBuilder(Commands.GroupByDateDeletedMonth)
								{
									IsToggle = true
								}.Build(),
							},
						},
						new CustomMenuFlyoutItemBuilder(Commands.GroupByFolderPath)
						{
							IsToggle = true
						}.Build(),
						new CustomMenuFlyoutItem
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new CustomMenuFlyoutItemBuilder(Commands.GroupAscending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
						new CustomMenuFlyoutItemBuilder(Commands.GroupDescending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
					},
				},
				new CustomMenuFlyoutItemBuilder(Commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				}.Build(),
				new CustomMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new CustomMenuFlyoutItem()
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
				new CustomMenuFlyoutItemBuilder(Commands.FormatDrive).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenItem).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				}.Build(),
				new CustomMenuFlyoutItem()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith"
					},
					Tag = "OpenWithOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<CustomMenuFlyoutItem>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new CustomMenuFlyoutItemBuilder(Commands.OpenFileLocation).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenDirectoryInNewTabAction).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenInNewWindowItemAction).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenDirectoryInNewPaneAction).Build(),
				new CustomMenuFlyoutItem()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false),
					ShowInSearchPage = true,
					Items = new List<CustomMenuFlyoutItem>
					{
						new CustomMenuFlyoutItemBuilder(Commands.SetAsWallpaperBackground).Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SetAsLockscreenBackground).Build(),
						new CustomMenuFlyoutItemBuilder(Commands.SetAsSlideshowBackground).Build(),
					}
				},
				new CustomMenuFlyoutItemBuilder(Commands.RotateLeft)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.RotateRight)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.RunAsAdmin).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.RunAsAnotherUser).Build(),
				new CustomMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new CustomMenuFlyoutItemBuilder(Commands.CutItem)
				{
					IsPrimary = true,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.CopyItem)
				{
					IsPrimary = true,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.CopyPath)
				{
					IsVisible = itemsSelected && !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.CreateFolderWithSelection)
				{
					IsVisible = itemsSelected
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.CreateShortcut)
				{
					IsVisible = itemsSelected && (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.ShareItem)
				{
					IsPrimary = true
				}.Build(),
				new CustomMenuFlyoutItemBuilder(ModifiableCommands .DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenProperties)
				{
					IsPrimary = true,
					IsVisible = Commands.OpenProperties.IsExecutable
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.OpenParentFolder).Build(),
				new CustomMenuFlyoutItemBuilder(Commands.PinItemToFavorites)
				{
					IsVisible = Commands.PinItemToFavorites.IsExecutable && UserSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.UnpinItemFromFavorites)
				{
					IsVisible = Commands.UnpinItemFromFavorites.IsExecutable && UserSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new CustomMenuFlyoutItemBuilder(Commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new CustomMenuFlyoutItem
				{
					Text = "Compress".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new List<CustomMenuFlyoutItem>
					{
						new CustomMenuFlyoutItemBuilder(Commands.CompressIntoArchive).Build(),
						new CustomMenuFlyoutItemBuilder(Commands.CompressIntoZip).Build(),
						new CustomMenuFlyoutItemBuilder(Commands.CompressIntoSevenZip).Build(),
					},
					ShowItem = itemsSelected && CompressHelper.CanCompress(selectedItems)
				},
				new CustomMenuFlyoutItem
				{
					Text = "Extract".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new List<CustomMenuFlyoutItem>
					{
						new CustomMenuFlyoutItemBuilder(Commands.DecompressArchive).Build(),
						new CustomMenuFlyoutItemBuilder(Commands.DecompressArchiveHere).Build(),
						new CustomMenuFlyoutItemBuilder(Commands.DecompressArchiveToChildFolder).Build(),
					},
					ShowItem = CompressHelper.CanDecompress(selectedItems)
				},
				new CustomMenuFlyoutItem()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					CollapseLabel = true,
					ShowInSearchPage = true,
					ShowItem = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new CustomMenuFlyoutItem()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = new List<CustomMenuFlyoutItem>() {
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					},
					ShowInSearchPage = true,
					ShowItem = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new CustomMenuFlyoutItem()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					CollapseLabel = true,
					IsEnabled = false,
					ShowItem = isDriveRoot
				},
				new CustomMenuFlyoutItem()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					CollapseLabel = true,
					ShowItem = isDriveRoot,
					IsEnabled = false
				},
				new CustomMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "EditTagsOverflowSeparator",
					IsHidden =
						tagItems is null ||
						tagItems.Count == 0 ||
						!itemsSelected ||
						!UserSettingsService.GeneralSettingsService.ShowEditTagsMenu ||
						!currentInstanceViewModel.CanTagFilesInPage,
					ShowInFtpPage = false,
					ShowInZipPage = false,
					ShowInRecycleBin = false,
					ShowInSearchPage = true,
				},
				new CustomMenuFlyoutItem()
				{
					Text = "EditTags".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconTag"
					},
					IsHidden = tagItems is null || tagItems.Count == 0,
					Tag = "EditTagsOverflow",
					Items = tagItems,
					ShowInSearchPage = true,
					ShowItem =
						itemsSelected &&
						UserSettingsService.GeneralSettingsService.ShowEditTagsMenu &&
						currentInstanceViewModel.CanTagFilesInPage,
				},
				// NOTE: Shell extensions are not available on the FTP server or in the archive,
				// but following items are intentionally added because icons in the context menu will not appear
				// unless there is at least one menu item with an icon that is not an OpacityIcon. (#12943)
				new CustomMenuFlyoutItem()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
				},
				new CustomMenuFlyoutItem()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<CustomMenuFlyoutItem>(),
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

		private static List<CustomMenuFlyoutItem> GetNewItemItems(
			BaseLayoutViewModel commandsViewModel,
			bool canCreateFileInPage)
		{
			var list = new List<CustomMenuFlyoutItem>()
			{
				new CustomMenuFlyoutItemBuilder(Commands.CreateFolder).Build(),
				new CustomMenuFlyoutItem()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new CustomMenuFlyoutItemBuilder(Commands.CreateShortcutFromDialog).Build(),
				new CustomMenuFlyoutItem()
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
						list.Add(new CustomMenuFlyoutItem()
						{
							Text = i.Name,
							BitmapIcon = bitmap,
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
					else
					{
						list.Add(new CustomMenuFlyoutItem()
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

		private static List<CustomMenuFlyoutItem>? GetFileTagItems(
			BaseLayoutViewModel commandsViewModel,
			List<ListedItem> selectedItems)
		{
			if (FileTagsSettingsService.FileTagList.Count == 0)
				return null;

			var list = new List<CustomMenuFlyoutItem>();

			FileTagsSettingsService.FileTagList.ForEach(item =>
			{
				bool isChecked = true;

				foreach (var selectedItem in selectedItems)
				{
					var existingTags = selectedItem.FileTags ?? Array.Empty<string>();

					if (!existingTags.Contains(item.Uid))
					{
						isChecked = false;
						break;
					}
				}

				var tagItem = new CustomMenuFlyoutItem
				{
					Text = item.Name,
					Tag = item,
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = isChecked,
					Icon = new PathIcon()
					{
						Data = (Geometry)XamlBindingHelper.ConvertValue(
							typeof(Geometry),
							(string)Application.Current.Resources["ColorIconFilledTag"]),
						Foreground = new SolidColorBrush(ColorHelpers.FromHex(item.Color))
					},
					Command = commandsViewModel.ToggleFileTagsCommand,
					CommandParameter = item,
				};

				list.Add(tagItem);
			});

			return list;
		}

		private static List<CustomMenuFlyoutItem> UpdateOverflowItems(
			List<CustomMenuFlyoutItem> items,
			List<ListedItem> selectedItems,
			bool shiftPressed,
			CurrentInstanceViewModel currentInstanceViewModel,
			bool removeOverflowMenu = true)
		{
			items =
				items.Where(item =>
					ValidateItemVisibility(
						item,
						currentInstanceViewModel,
						selectedItems))
				.ToList();

			items.ForEach(item => item.Items =
				item.Items?.Where(nestedItem =>
						ValidateItemVisibility(
							nestedItem,
							currentInstanceViewModel,
							selectedItems))
					.ToList() ?? new List<CustomMenuFlyoutItem>());

			// Get overflow placeholder
			var overflow = items.Where(x => x.ID == "ItemOverflow").FirstOrDefault();

			if (overflow is not null)
			{
				// De-clutter items with ShowOnShift
				if (!shiftPressed && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				{
					// Get the items to move into the sub menu
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Add a separator between items already there and the new ones
					if (overflow.Items.Count != 0 &&
						overflowItems.Count > 0 &&
						overflow.Items.Last().ItemType != ContextMenuFlyoutItemType.Separator)
					{
						overflow.Items.Add(
							new()
							{
								ItemType = ContextMenuFlyoutItemType.Separator
							});
					}

					// Remove the items to move from the default menu
					items = items.Except(overflowItems).ToList();

					// Add into the sub menu
					overflow.Items.AddRange(overflowItems);
				}

				// Remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool ValidateItemVisibility(
			CustomMenuFlyoutItem item,
			CurrentInstanceViewModel currentInstanceViewModel,
			List<ListedItem> selectedItems)
		{
			return
				(item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) &&
				(item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) &&
				(item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp) &&
				(item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder) &&
				(!item.SingleItemOnly || selectedItems.Count == 1) &&
				item.ShowItem;
		}
	}
}
