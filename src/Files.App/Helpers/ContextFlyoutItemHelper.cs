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
	/// Represents a helper that is used to create lists of <see cref="ContextMenuFlyoutItemViewModel"/> that can be used
	/// by <see cref="ContextFlyouts.ItemModelListToContextFlyoutHelper"/> to create context menus and toolbars for the user.
	/// </summary>
	public static class ContextFlyoutItemHelper
	{
		private static IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private static ICommandManager commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		private static IAddItemService addItemService { get; } = Ioc.Default.GetRequiredService<IAddItemService>();

		public static List<ContextMenuFlyoutItemViewModel> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, BaseLayoutCommandsViewModel commandsViewModel, bool shiftPressed, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, ItemViewModel? itemViewModel = null)
		{
			var menuItemsList = GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, currentInstanceViewModel: currentInstanceViewModel, itemViewModel: itemViewModel);
			menuItemsList = Filter(menuItemsList, selectedItems, shiftPressed, currentInstanceViewModel, false);

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
				// Items with ShowOnShift to overflow menu
				if (!shiftPressed && userSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ItemType.Separator)
						overflow.Items.Add(new ContextMenuFlyoutItemViewModel { ItemType = ItemType.Separator });

					items = items.Except(overflowItems).ToList();
					overflow.Items.AddRange(overflowItems);
				}

				// Remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool Check(ContextMenuFlyoutItemViewModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems)
		{
			return
				(item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) &&
				(item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) &&
				(item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp) &&
				(item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder) &&
				(!item.SingleItemOnly || selectedItems.Count == 1) &&
				item.ShowItem;
		}

		public static List<ContextMenuFlyoutItemViewModel> GetBaseItemMenuItems(BaseLayoutCommandsViewModel commandsViewModel, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, List<ListedItem> selectedItems, CurrentInstanceViewModel currentInstanceViewModel, ItemViewModel itemViewModel = null)
		{
			bool itemsSelected = itemViewModel is null;

			bool canDecompress =
				selectedItems.Any() &&
				selectedItems.All(x => x.IsArchive) ||
				selectedItems.All(x =>
					x.PrimaryItemAttribute == StorageItemTypes.File &&
					FileExtensionHelpers.IsZipFile(x.FileExtension)
				);

			bool canCompress = !canDecompress || selectedItems.Count > 1;

			bool showOpenItemWith =
				selectedItems.All(i =>
					(i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) ||
					(i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive)
				);

			bool areAllItemsFolders = selectedItems.All(i => i.PrimaryItemAttribute == StorageItemTypes.Folder);

			bool isFirstFileExecutable = FileExtensionHelpers.IsExecutableFile(selectedItems.FirstOrDefault()?.FileExtension);

			string newArchiveName =
				Path.GetFileName(
					selectedItems.Count is 1
						? selectedItems[0].ItemPath
						: Path.GetDirectoryName(selectedItems[0].ItemPath))
				?? string.Empty;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "LayoutMode".GetLocalizedResource(),
					Glyph = "\uE152",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>
					{
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutDetails){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutTiles){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutGridSmall){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutGridMedium){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutGridLarge){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutColumns){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.LayoutAdaptive){IsToggle = true}.Build(),
					},
				},
				new ContextMenuFlyoutItemViewModel()
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
					Items = new List<ContextMenuFlyoutItemViewModel>
					{
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByName){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByDateModified){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByDateCreated){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByType){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortBySize){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortBySyncStatus){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByTag){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByOriginalFolder){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortByDateDeleted){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModel
						{
							ItemType = ItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortAscending).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SortDescending).Build(),
					},
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "NavToolbarGroupByRadioButtons/Text".GetLocalizedResource(),
					Glyph = "\uF168",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items = new List<ContextMenuFlyoutItemViewModel>
					{
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByNone){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByName){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateModified){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateCreated){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByType){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupBySize){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupBySyncStatus){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByTag){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByOriginalFolder){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByDateDeleted){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupByFolderPath){IsToggle = true}.Build(),
						new ContextMenuFlyoutItemViewModel
						{
							ItemType = ItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupAscending){IsVisible = true}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.GroupDescending){IsVisible = true}.Build(),
					},
				},
				new ContextMenuFlyoutItemViewModelBuilder(commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModelBuilder(commands.AddItem)
				{
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					IsVisible = !itemsSelected
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.FormatDrive).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.OpenItem).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith"
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
				new ContextMenuFlyoutItemViewModelBuilder(commands.OpenFileLocation).Build(),
				new ContextMenuFlyoutItemViewModel()
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
				new ContextMenuFlyoutItemViewModel()
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
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = commandsViewModel.OpenDirectoryInNewPaneCommand,
					ShowItem = itemsSelected && userSettingsService.GeneralSettingsService.ShowOpenInNewPane && areAllItemsFolders,
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
					Items = new List<ContextMenuFlyoutItemViewModel>
					{
						new ContextMenuFlyoutItemViewModelBuilder(commands.SetAsWallpaperBackground).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SetAsLockscreenBackground).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.SetAsSlideshowBackground).Build(),
					}
				},
				new ContextMenuFlyoutItemViewModelBuilder(commands.RotateLeft)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.RotateRight)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsSelectedItemImage ?? false)
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.RunAsAdmin).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.RunAsAnotherUser).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ItemType.Separator,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModelBuilder(commands.CutItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.CopyItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.CopyPath)
				{
					IsVisible = itemsSelected && selectedItems.Count == 1 && !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutCreateFolderWithSelection/Text".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconNewFolder",
					},
					Command = commandsViewModel.CreateFolderWithSelection,
					ShowItem = itemsSelected,
				},
				new ContextMenuFlyoutItemViewModelBuilder(commands.CreateShortcut)
				{
					IsVisible = itemsSelected && (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.ShareItem)
				{
					IsPrimary = true
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
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
				new ContextMenuFlyoutItemViewModelBuilder(commands.OpenParentFolder).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.PinItemToFavorites)
				{
					IsVisible = commands.PinItemToFavorites.IsExecutable && userSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.UnpinItemFromFavorites)
				{
					IsVisible = commands.UnpinItemFromFavorites.IsExecutable && userSettingsService.GeneralSettingsService.ShowFavoritesSection,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => !x.IsShortcut && (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModel
				{
					Text = "Archive".GetLocalizedResource(),
					ShowInSearchPage = true,
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconZip",
					},
					Items = new List<ContextMenuFlyoutItemViewModel>
					{
						new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchive)
						{
							IsVisible = ArchiveHelpers.CanDecompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchiveHere)
						{
							IsVisible = ArchiveHelpers.CanDecompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.DecompressArchiveToChildFolder)
						{
							IsVisible = ArchiveHelpers.CanDecompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemViewModel
						{
							ShowItem = canDecompress && canCompress,
							ItemType = ItemType.Separator,
						},
						new ContextMenuFlyoutItemViewModelBuilder(commands.CompressIntoArchive)
						{
							IsVisible = ArchiveHelpers.CanCompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.CompressIntoZip)
						{
							IsVisible = ArchiveHelpers.CanCompress(selectedItems)
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(commands.CompressIntoSevenZip)
						{
							IsVisible = ArchiveHelpers.CanCompress(selectedItems)
						}.Build(),
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
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.ShowItem).ToList();
		}

		public static List<ContextMenuFlyoutItemViewModel> GetNewItemItems(BaseLayoutCommandsViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(commands.CreateFolder).Build(),
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
				new ContextMenuFlyoutItemViewModelBuilder(commands.CreateShortcutFromDialog).Build(),
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
						// Loading the bitmaps takes a while, so this caches them
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
