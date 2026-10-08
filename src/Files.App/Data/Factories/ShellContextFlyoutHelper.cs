// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Helpers.ContextFlyouts;
using Files.Shared.Helpers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using System.Text;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Helpers
{
	public static class ShellContextFlyoutFactory
	{
		public static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private static (string Label, string AccessKey) ExtractLabelAndAccessKey(string rawLabel)
		{
			if (string.IsNullOrEmpty(rawLabel))
				return (string.Empty, string.Empty);

			string accessKey = string.Empty;
			var labelBuilder = new StringBuilder(rawLabel.Length);

			for (int i = 0; i < rawLabel.Length; i++)
			{
				char current = rawLabel[i];
				if (current != '&')
				{
					labelBuilder.Append(current);
					continue;
				}

				if (i + 1 >= rawLabel.Length)
				{
					labelBuilder.Append('&');
					continue;
				}

				char next = rawLabel[++i];
				if (next == '&')
				{
					labelBuilder.Append('&');
					continue;
				}

				if (string.IsNullOrEmpty(accessKey) && !char.IsWhiteSpace(next))
					accessKey = char.ToUpperInvariant(next).ToString();

				labelBuilder.Append(next);
			}

			return (labelBuilder.ToString(), accessKey);
		}

		private static ContextMenuFlyoutItemViewModel CreateShellMenuItem(Win32ContextMenuItem menuFlyoutItem, BitmapImage? image)
		{
			var (label, accessKey) = ExtractLabelAndAccessKey(menuFlyoutItem.Label ?? string.Empty);
			return new ContextMenuFlyoutItemViewModel
			{
				Text = label,
				AccessKey = accessKey,
				Tag = menuFlyoutItem,
				BitmapIcon = image,
			};
		}

		/// <param name="explorerCommandsLoaded">
		/// Receives the Windows 11 context menu commands as soon as they load, usually well before the classic shell
		/// menu; they are then left out of the returned list. When null, they are returned first in the list.
		/// </param>
		public static async Task<List<ContextMenuFlyoutItemViewModel>> GetShellContextmenuAsync(bool showOpenMenu, bool shiftPressed, string? workingDirectory, List<ListedItem>? selectedItems, CancellationToken cancellationToken, Action<List<ContextMenuFlyoutItemViewModel>>? explorerCommandsLoaded = null)
		{
			var menuItemsList = new List<ContextMenuFlyoutItemViewModel>();
			var filePaths = selectedItems is { Count: > 0 }
				? selectedItems.Select(x => x.ItemPath!).ToArray()
				: [workingDirectory ?? throw new ArgumentException("A working directory is required when no items are selected.", nameof(workingDirectory))];

			Func<string?, bool> FilterMenuItems(bool showOpenMenu)
			{
				var knownItems = new HashSet<string>()
				{
					"opennew", "opencontaining", "opennewprocess",
					"runas", "runasuser", "pintohome", "PinToStartScreen",
					"cut", "copy", "paste", "delete", "properties", "link",
					"Windows.ModernShare", "setdesktopwallpaper",
					"eject", "rename", "explore", "openinfiles", "extract",
					"copyaspath", "undelete", "empty", "format", "rotate90", "rotate270",
					Win32Helper.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5384), // Pin to Start
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5385), // Unpin from Start
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5386), // Pin to taskbar
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5387), // Unpin from taskbar
					"{9F156763-7844-4DC4-B2B1-901F640F5155}", // Open in Terminal
				};

				bool filterMenuItemsImpl(string? menuItem) => !string.IsNullOrEmpty(menuItem)
					&& (knownItems.Contains(menuItem) || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase)));

				return filterMenuItemsImpl;
			}

			var itemFilter = FilterMenuItems(showOpenMenu);

			// Windows 11 context menu commands, queried alongside the classic menu
			var explorerCommandMenuTask = ExplorerCommandMenu.GetExplorerCommandMenuAsync(filePaths, selectedItems is not { Count: > 0 }, itemFilter, cancellationToken);

			if (explorerCommandsLoaded is not null)
				_ = ShowExplorerCommandsAsync();

			async Task ShowExplorerCommandsAsync()
			{
				try
				{
					if (await explorerCommandMenuTask is not { } explorerCommandMenu)
						return;

					var models = await GetExplorerCommandModelsAsync(explorerCommandMenu, explorerCommandMenu.Items, cancellationToken);
					if (!cancellationToken.IsCancellationRequested && models.Count > 0)
						explorerCommandsLoaded(models);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}

			var contextMenu = await ContextMenu.GetContextMenuForFiles(filePaths,
				shiftPressed ? PInvoke.CMF_EXTENDEDVERBS : PInvoke.CMF_NORMAL, itemFilter);
			var explorerCommandMenu = await explorerCommandMenuTask;

			if (contextMenu is not null)
			{
				if (explorerCommandMenu is not null)
					contextMenu.Items!.RemoveAll(explorerCommandMenu.IsDuplicate);

				await LoadMenuFlyoutItemAsync(menuItemsList, contextMenu, contextMenu.Items!, cancellationToken, true);
			}

			if (explorerCommandMenu is not null && explorerCommandsLoaded is null)
				menuItemsList.InsertRange(0, await GetExplorerCommandModelsAsync(explorerCommandMenu, explorerCommandMenu.Items, cancellationToken));

			if (cancellationToken.IsCancellationRequested)
				menuItemsList.Clear();

			return menuItemsList;
		}

		private static async Task LoadMenuFlyoutItemAsync(
			List<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
			ContextMenu contextMenu,
			IEnumerable<Win32ContextMenuItem> menuFlyoutItems,
			CancellationToken cancellationToken,
			bool showIcons = true,
			int itemsBeforeOverflow = int.MaxValue)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			var itemsCount = 0; // Separators do not count for reaching the overflow threshold
			var menuItems = menuFlyoutItems.TakeWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR || ++itemsCount <= itemsBeforeOverflow).ToList();
			var overflowItems = menuFlyoutItems.Except(menuItems).ToList();

			if (overflowItems.Any(x => x.Type != MENU_ITEM_TYPE.MFT_SEPARATOR))
			{
				var moreItem = menuItemsListLocal.FirstOrDefault(x => x.ID == "ItemOverflow");
				if (moreItem is null)
				{
					var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
					{
						Text = Strings.ShowMoreOptions.GetLocalizedResource(),
						Glyph = "\xE712",
					};
					await LoadMenuFlyoutItemAsync(menuLayoutSubItem.Items
						?? throw new InvalidOperationException("The shell overflow menu has not been initialized."), contextMenu, overflowItems, cancellationToken, showIcons);
					menuItemsListLocal.Insert(0, menuLayoutSubItem);
				}
				else
				{
					await LoadMenuFlyoutItemAsync(moreItem.Items
						?? throw new InvalidOperationException("The shell overflow menu has not been initialized."), contextMenu, overflowItems, cancellationToken, showIcons);
				}
			}

			foreach (var menuFlyoutItem in menuItems
				.SkipWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR) // Remove leading separators
				.Reverse()
				.SkipWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR)) // Remove trailing separators
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				// Avoid duplicate separators
				if ((menuFlyoutItem.Type == MENU_ITEM_TYPE.MFT_SEPARATOR) && (menuItemsListLocal.FirstOrDefault()?.ItemType == ContextMenuFlyoutItemType.Separator))
					continue;

				BitmapImage? image = showIcons ? await GetBitmapAsync(menuFlyoutItem.Icon) : null;

				if (menuFlyoutItem.Type is MENU_ITEM_TYPE.MFT_SEPARATOR)
				{
					var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
					{
						ItemType = ContextMenuFlyoutItemType.Separator,
						Tag = menuFlyoutItem
					};
					menuItemsListLocal.Insert(0, menuLayoutItem);
				}
				else if (!string.IsNullOrEmpty(menuFlyoutItem.Label) && menuFlyoutItem.SubItems is not null)
				{
					if (string.Equals(menuFlyoutItem.Label, Win32Helper.ExtractStringFromDLL("shell32.dll", 30312)))
						menuFlyoutItem.CommandString = "sendto";

					var menuLayoutSubItem = CreateShellMenuItem(menuFlyoutItem, image);
					menuLayoutSubItem.Items = [];

					if (menuFlyoutItem.SubItems.Any())
					{
						await LoadMenuFlyoutItemAsync(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
					}
					else
					{
						menuLayoutSubItem.LoadSubMenuAction = async () =>
						{
							if (await contextMenu.LoadSubMenu(menuFlyoutItem.SubItems))
								await LoadMenuFlyoutItemAsync(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
						};
					}

					menuItemsListLocal.Insert(0, menuLayoutSubItem);
				}
				else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
				{
					var menuLayoutItem = CreateShellMenuItem(menuFlyoutItem, image);
					menuLayoutItem.Command = new AsyncRelayCommand<object>(x => InvokeShellMenuItemAsync(contextMenu, x));
					menuLayoutItem.CommandParameter = menuFlyoutItem;
					menuItemsListLocal.Insert(0, menuLayoutItem);
				}
			}

			async Task InvokeShellMenuItemAsync(ContextMenu contextMenu, object? tag)
			{
				if (tag is not Win32ContextMenuItem menuItem)
					return;

				var menuId = menuItem.ID;
				var isFont = FileExtensionHelpers.IsFontFile(contextMenu.ItemsPath[0]);
				var verb = menuItem.CommandString;
				switch (verb)
				{
					case "install" when isFont:
						await Win32Helper.InstallFontsAsync([.. contextMenu.ItemsPath], false);
						break;

					case "installAllUsers" when isFont:
						await Win32Helper.InstallFontsAsync([.. contextMenu.ItemsPath], true);
						break;

					case "mount":
						var vhdPath = contextMenu.ItemsPath[0];
						await Win32Helper.MountVhdDisk(vhdPath);
						break;

					case "format":
						var drivePath = contextMenu.ItemsPath[0];
						await Win32Helper.OpenFormatDriveDialog(drivePath);
						break;

					case "Windows.PowerShell.Run":
						await contextMenu.InvokeItem(
							menuId,
							contextMenu.ItemsPath[0].EndsWith(".ps1") ? Path.GetDirectoryName(contextMenu.ItemsPath[0]) : null
						);
						break;

					default:
						await contextMenu.InvokeItem(menuId);
						break;
				}

				//contextMenu.Dispose(); // Prevents some menu items from working (TBC)
			}
		}

		private static async Task<BitmapImage?> GetBitmapAsync(byte[]? icon)
		{
			if (icon is not { Length: > 0 })
				return null;

			var image = new BitmapImage();
			using var ms = new MemoryStream(icon);
			await image.SetSourceAsync(ms.AsRandomAccessStream());
			return image;
		}

		private static async Task<List<ContextMenuFlyoutItemViewModel>> GetExplorerCommandModelsAsync(
			ExplorerCommandMenu explorerCommandMenu,
			IEnumerable<Win32ContextMenuItem> menuItems,
			CancellationToken cancellationToken)
		{
			var models = new List<ContextMenuFlyoutItemViewModel>();
			foreach (var menuItem in menuItems)
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				if (menuItem.Type is MENU_ITEM_TYPE.MFT_SEPARATOR)
				{
					models.Add(new() { ItemType = ContextMenuFlyoutItemType.Separator, Tag = menuItem });
					continue;
				}

				var model = CreateShellMenuItem(menuItem, await GetBitmapAsync(menuItem.Icon));
				model.IsEnabled = menuItem is not ExplorerCommandMenuItem { IsEnabled: false };
				if (menuItem.SubItems is not null)
					model.Items = await GetExplorerCommandModelsAsync(explorerCommandMenu, menuItem.SubItems, cancellationToken);
				else if (menuItem is ExplorerCommandMenuItem explorerCommandItem)
					model.Command = new AsyncRelayCommand(() => explorerCommandMenu.InvokeItem(explorerCommandItem));

				models.Add(model);
			}

			return models;
		}

		public static List<ContextMenuFlyoutItemViewModel>? GetOpenWithItems(List<ContextMenuFlyoutItemViewModel> flyout)
		{
			var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
			if (item is not null)
				flyout.Remove(item);

			return item?.Items;
		}

		public static List<ContextMenuFlyoutItemViewModel>? GetSendToItems(List<ContextMenuFlyoutItemViewModel> flyout)
		{
			var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
			if (item is not null)
				flyout.Remove(item);

			return item?.Items;
		}

		/// <summary>
		/// Loads the shell menu items into a FastContextFlyout-based menu (sidebar, widgets): fills the
		/// pre-added "Show more options" submenu (or appends inline per the setting) and swaps the
		/// Open with / Send to / BitLocker placeholders.
		/// </summary>
		public static async Task LoadShellMenuItemsAsync(
			string path,
			FastContextFlyout flyout,
			ContextMenuOptions? options = null,
			MenuFlyoutSubItem? overflowSubMenu = null,
			MenuFlyoutSeparator? overflowSeparator = null,
			bool showOpenWithMenu = false,
			bool showSendToMenu = false)
		{
			try
			{
				if (options is not null && !options.IsLocationItem)
				{
					if (overflowSubMenu is not null)
						flyout.RemoveIfEmpty(overflowSubMenu, overflowSeparator);
					return;
				}

				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
				var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(
					workingDir: null,
					[new ListedItem(null) { ItemPath = path }],
					shiftPressed: shiftPressed,
					showOpenMenu: false,
					default,
					models => flyout.AddExplorerCommandModels(models, overflowSubMenu, overflowSeparator));

				// Open with / Send to / BitLocker get their own main-menu entries; everything else is overflow.
				var openWithItem = showOpenWithMenu ? shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" }) : null;
				if (openWithItem is not null)
					shellMenuItems.Remove(openWithItem);

				var sendToItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
				if (sendToItem is not null && (showSendToMenu || !UserSettingsService.GeneralSettingsService.ShowSendToMenu))
					shellMenuItems.Remove(sendToItem);
				sendToItem = showSendToMenu && UserSettingsService.GeneralSettingsService.ShowSendToMenu ? sendToItem : null;

				// BitLocker: replace the placeholders with whichever entries the shell offers (drives)
				flyout.ApplyBitLockerModels(shellMenuItems, overflowSubMenu, overflowSeparator);

				// The rest fill the pre-added "Show more options", or render inline per the setting
				flyout.AddShellModels(shellMenuItems, shiftPressed: false, overflowSubMenu, overflowSeparator, aboveExisting: false);

				// Open with / Send to: the placeholders were converted to their submenu form before the menu was
				// shown (stable heights); fill their contents now, or drop them if the shell has no such entries.
				void FillOrRemove(MenuFlyoutSubItem? subMenu, ContextMenuFlyoutItemViewModel? item, Func<List<ContextMenuFlyoutItemViewModel>, List<ContextMenuFlyoutItemViewModel>?> getter)
				{
					if (subMenu is null)
						return;

					if (item?.LoadSubMenuAction is not null)
						FastContextFlyout.PopulateShellSubMenu(subMenu, item, getter, () => flyout.Items.Remove(subMenu));
					else
						flyout.Items.Remove(subMenu);
				}

				FillOrRemove(flyout.ConvertPlaceholderToSubMenu("OpenWithPlaceholder", Strings.OpenWith.GetLocalizedResource(), "App.ThemedIcons.OpenWith"), openWithItem, GetOpenWithItems);
				FillOrRemove(flyout.ConvertPlaceholderToSubMenu("SendToPlaceholder", Strings.SendTo.GetLocalizedResource(), null), sendToItem, GetSendToItems);

				flyout.FinalizePrimaryRowPosition();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
	}
}
