using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Shell;
using Files.App.ViewModels;
using Files.Shared;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Files.App.Helpers
{
	public static class ShellContextmenuHelper
	{
		public static async Task<List<ContextMenuFlyoutItemViewModel>> GetShellContextmenuAsync(bool showOpenMenu, bool shiftPressed, string workingDirectory, List<ListedItem> selectedItems, CancellationToken cancellationToken)
		{
			bool IsItemSelected = selectedItems?.Count > 0;

			var menuItemsList = new List<ContextMenuFlyoutItemViewModel>();

			var filePaths = IsItemSelected ?
				selectedItems.Select(x => x.ItemPath).ToArray() : new[] { workingDirectory };

			Func<string, bool> FilterMenuItems(bool showOpenMenu)
			{
				var knownItems = new List<string>()
				{
					"opennew", "opencontaining", "opennewprocess",
					"runas", "runasuser", "pintohome", "PinToStartScreen",
					"cut", "copy", "paste", "delete", "properties", "link",
					"Windows.ModernShare", "Windows.Share", "setdesktopwallpaper",
					"eject", "rename", "explore", "openinfiles", "extract",
					"copyaspath", "undelete", "empty",
					Win32API.ExtractStringFromDLL("shell32.dll", 30312), // SendTo menu
                    Win32API.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
                };

				bool filterMenuItemsImpl(string menuItem) => !string.IsNullOrEmpty(menuItem)
					&& (knownItems.Contains(menuItem) || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase)));

				return filterMenuItemsImpl;
			}

			var contextMenu = await ContextMenu.GetContextMenuForFiles(filePaths,
				(shiftPressed ? Shell32.CMF.CMF_EXTENDEDVERBS : Shell32.CMF.CMF_NORMAL) | Shell32.CMF.CMF_SYNCCASCADEMENU, FilterMenuItems(showOpenMenu));

			if (contextMenu is not null)
			{
				LoadMenuFlyoutItem(menuItemsList, contextMenu, contextMenu.Items, cancellationToken, true);
			}

			if (cancellationToken.IsCancellationRequested)
			{
				menuItemsList.Clear();
			}

			return menuItemsList;
		}

		public static void LoadMenuFlyoutItem(IList<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
								ContextMenu contextMenu,
								IEnumerable<Win32ContextMenuItem> menuFlyoutItems,
								CancellationToken cancellationToken,
								bool showIcons = true,
								int itemsBeforeOverflow = int.MaxValue)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			var itemsCount = 0; // Separators do not count for reaching the overflow threshold
			var menuItems = menuFlyoutItems.TakeWhile(x => x.Type == MenuItemType.MFT_SEPARATOR || ++itemsCount <= itemsBeforeOverflow).ToList();
			var overflowItems = menuFlyoutItems.Except(menuItems).ToList();

			if (overflowItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any())
			{
				var moreItem = menuItemsListLocal.Where(x => x.ID == "ItemOverflow").FirstOrDefault();
				if (moreItem is null)
				{
					var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
					{
						Text = "ContextMenuMoreItemsLabel".GetLocalizedResource(),
						Glyph = "\xE712",
					};
					LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, overflowItems, cancellationToken, showIcons);
					menuItemsListLocal.Insert(0, menuLayoutSubItem);
				}
				else
				{
					LoadMenuFlyoutItem(moreItem.Items, contextMenu, overflowItems, cancellationToken, showIcons);
				}
			}
			foreach (var menuFlyoutItem in menuItems
				.SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR) // Remove leading separators
				.Reverse()
				.SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR)) // Remove trailing separators
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}

				if ((menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR) && (menuItemsListLocal.FirstOrDefault().ItemType == ItemType.Separator))
				{
					// Avoid duplicate separators
					continue;
				}

				BitmapImage image = null;
				if (showIcons)
				{
					if (menuFlyoutItem.Icon is { Length: > 0 })
					{
						image = new BitmapImage();
						using var ms = new MemoryStream(menuFlyoutItem.Icon);
						image.SetSourceAsync(ms.AsRandomAccessStream()).AsTask().Wait(10);
					}
				}

				if (menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR)
				{
					var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
					{
						ItemType = ItemType.Separator,
						Tag = menuFlyoutItem
					};
					menuItemsListLocal.Insert(0, menuLayoutItem);
				}
				else if (menuFlyoutItem.SubItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any()
					&& !string.IsNullOrEmpty(menuFlyoutItem.Label))
				{
					var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
					{
						Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
						Tag = menuFlyoutItem,
						Items = new List<ContextMenuFlyoutItemViewModel>(),
					};
					LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
					menuItemsListLocal.Insert(0, menuLayoutSubItem);
				}
				else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
				{
					var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
					{
						Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
						Tag = menuFlyoutItem,
						BitmapIcon = image
					};
					menuLayoutItem.Command = new RelayCommand<object>(x => InvokeShellMenuItem(contextMenu, x));
					menuLayoutItem.CommandParameter = menuFlyoutItem;
					menuItemsListLocal.Insert(0, menuLayoutItem);
				}
			}

			async void InvokeShellMenuItem(ContextMenu contextMenu, object? tag)
			{
				if (tag is not Win32ContextMenuItem menuItem) return;

				var menuId = menuItem.ID;
				var isFont = new[] { ".fon", ".otf", ".ttc", ".ttf" }.Contains(Path.GetExtension(contextMenu.ItemsPath[0]), StringComparer.OrdinalIgnoreCase);
				var verb = menuItem.CommandString;
				switch (verb)
				{
					case "install" when isFont:
						{
							var userFontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
							var destName = Path.Combine(userFontDir, Path.GetFileName(contextMenu.ItemsPath[0]));
							Win32API.RunPowershellCommand($"-command \"Copy-Item '{contextMenu.ItemsPath[0]}' '{userFontDir}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(contextMenu.ItemsPath[0])}' -Path 'HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts' -PropertyType string -Value '{destName}'\"", false);
						}
						break;

					case "installAllUsers" when isFont:
						{
							var winFontDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
							Win32API.RunPowershellCommand($"-command \"Copy-Item '{contextMenu.ItemsPath[0]}' '{winFontDir}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(contextMenu.ItemsPath[0])}' -Path 'HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts' -PropertyType string -Value '{Path.GetFileName(contextMenu.ItemsPath[0])}'\"", true);
						}
						break;

					case "mount":
						var vhdPath = contextMenu.ItemsPath[0];
						Win32API.MountVhdDisk(vhdPath);
						break;

					case "format":
						var drivePath = contextMenu.ItemsPath[0];
						Win32API.OpenFormatDriveDialog(drivePath);
						break;

					default:
						await contextMenu.InvokeItem(menuId);
						break;
				}

				//contextMenu.Dispose(); // Prevents some menu items from working (TBC)
			}
		}

		public static List<ContextMenuFlyoutItemViewModel> GetOpenWithItems(List<ContextMenuFlyoutItemViewModel> flyout)
		{
			var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
			flyout.Remove(item);
			return item?.Items;
		}
	}
}