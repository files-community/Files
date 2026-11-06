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
				IsEnabled = menuFlyoutItem.IsEnabled,
				IsChecked = menuFlyoutItem.IsChecked,
				ItemType = menuFlyoutItem.IsToggle ? ContextMenuFlyoutItemType.Toggle : ContextMenuFlyoutItemType.Item,
			};
		}

		public static async Task<ShellContextMenuResult> GetShellContextmenuAsync(bool showOpenMenu, bool shiftPressed, string? workingDirectory, List<ListedItem>? selectedItems, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return ShellContextMenuResult.Empty;

			var filePaths = selectedItems is { Count: > 0 }
				? selectedItems.Select(x => x.IsRecycleBinItem ? ShellPath.GetRecycleBinParsingName(x.ItemPath!) : x.ItemPath!).ToArray()
				: [workingDirectory ?? throw new ArgumentException("A working directory is required when no items are selected.", nameof(workingDirectory))];

			Func<string?, bool> FilterMenuItems(bool showOpenMenu)
			{
				var knownItems = new HashSet<string>()
				{
					"opennew", "opencontaining", "opennewprocess",
					"runas", "runasuser", "pintohome", "PinToStartScreen",
					"cut", "copy", "paste", "delete", "properties", "link", "New",
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

			var options = selectedItems is { Count: > 0 }
				? Server.ContextMenuQueryOptions.ModernItems
				: Server.ContextMenuQueryOptions.ModernItems | Server.ContextMenuQueryOptions.Background;
			var contextMenu = await SafetyExtensions.IgnoreExceptions(() => ContextMenu.GetContextMenuForFiles(filePaths,
				shiftPressed ? PInvoke.CMF_EXTENDEDVERBS : PInvoke.CMF_NORMAL, FilterMenuItems(showOpenMenu), options), App.Logger);
			if (contextMenu is null)
				return ShellContextMenuResult.Empty;

			// Loads in parallel on the server
			var classicItems = LoadClassicItemsAsync(contextMenu, cancellationToken);
			var modernItems = new List<ContextMenuFlyoutItemViewModel>();
			if (!await SafetyExtensions.IgnoreExceptions(() => LoadMenuFlyoutItemAsync(modernItems, contextMenu, contextMenu.Items!, cancellationToken, true), App.Logger)
				|| cancellationToken.IsCancellationRequested)
			{
				contextMenu.Dispose();
				return ShellContextMenuResult.Empty;
			}

			// Without visible subcommands, a command is invoked itself
			foreach (var model in modernItems)
			{
				if (model.Items is not null && model.Tag is Win32ContextMenuItem { ID: >= 0 } item)
					model.Command = new AsyncRelayCommand(() => contextMenu.InvokeItem(item.ID));
			}

			_ = SafetyExtensions.IgnoreExceptions(() => LoadModernIconsAsync(contextMenu, modernItems, cancellationToken), App.Logger);
			return new(contextMenu, modernItems, classicItems);
		}

		private static async Task LoadModernIconsAsync(ContextMenu contextMenu, List<ContextMenuFlyoutItemViewModel> models, CancellationToken cancellationToken)
		{
			var images = new Dictionary<Win32ContextMenuItem, BitmapImage>();
			foreach (var model in models)
			{
				if (model.ItemType is not ContextMenuFlyoutItemType.Separator && model.BitmapIcon is null && model.Tag is Win32ContextMenuItem item)
					images[item] = model.BitmapIcon = new BitmapImage();
			}

			if (images.Count == 0)
				return;

			var icons = await contextMenu.LoadIconsAsync();
			var items = contextMenu.Items!;
			for (var index = 0; index < icons.Count && index < items.Count && !cancellationToken.IsCancellationRequested; index++)
			{
				if (icons[index] is { Length: > 0 } icon && images.TryGetValue(items[index], out var image))
				{
					using var stream = new MemoryStream(icon);
					await SafetyExtensions.IgnoreExceptions(() => image.SetSourceAsync(stream.AsRandomAccessStream()).AsTask(), App.Logger);
				}
			}
		}

		private static async Task<List<ContextMenuFlyoutItemViewModel>> LoadClassicItemsAsync(ContextMenu contextMenu, CancellationToken cancellationToken)
		{
			var classicItems = await SafetyExtensions.IgnoreExceptions(async () =>
			{
				var items = new List<ContextMenuFlyoutItemViewModel>();
				await LoadMenuFlyoutItemAsync(items, contextMenu, await contextMenu.LoadClassicItemsAsync(), cancellationToken, true);
				return items;
			}, App.Logger);

			return classicItems ?? [];
		}

		private static async Task LoadMenuFlyoutItemAsync(
			List<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
			ContextMenu contextMenu,
			IEnumerable<Win32ContextMenuItem> menuFlyoutItems,
			CancellationToken cancellationToken,
			bool showIcons = true)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			foreach (var menuFlyoutItem in menuFlyoutItems
				.SkipWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR) // Remove leading separators
				.Reverse()
				.SkipWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR)) // Remove trailing separators
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				// Avoid duplicate separators
				if ((menuFlyoutItem.Type == MENU_ITEM_TYPE.MFT_SEPARATOR) && (menuItemsListLocal.FirstOrDefault()?.ItemType == ContextMenuFlyoutItemType.Separator))
					continue;

				BitmapImage? image = null;
				if (showIcons && menuFlyoutItem.Icon is { Length: > 0 })
				{
					image = new BitmapImage();
					using var ms = new MemoryStream(menuFlyoutItem.Icon);
					await image.SetSourceAsync(ms.AsRandomAccessStream());
				}

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
			}
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
		/// Loads the shell menu items into a FastContextFlyout-based menu (sidebar, widgets): adds the classic items
		/// under "Show more options" and swaps the Open with / Send to / BitLocker placeholders.
		/// </summary>
		public static async Task LoadShellMenuItemsAsync(
			string path,
			FastContextFlyout flyout,
			FrameworkElement anchor,
			Windows.Foundation.Point position,
			CancellationToken cancellationToken,
			ContextMenuOptions? options = null,
			bool showOpenWithMenu = false,
			bool showSendToMenu = false)
		{
			try
			{
				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
				var token = flyout.BeginShellMenuLoad(cancellationToken);

				// "Show more options" is added before showing so filling it with the classic items never resizes the menu
				var (overflowSubMenu, overflowSeparator) = flyout.AddShowMoreOptions();
				var shellMenu = options is not null && !options.IsLocationItem
					? ShellContextMenuResult.Empty
					: await flyout.WaitForShellMenuAsync(ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(
						workingDir: null,
						[new ListedItem(null) { ItemPath = path }],
						shiftPressed: shiftPressed,
						showOpenMenu: false,
						token), anchor, position, token);
				if (!flyout.AttachShellMenu(shellMenu, token))
					return;

				flyout.AddModernShellModels(shellMenu.ModernItems, overflowSubMenu, overflowSeparator);
				var openWithSubMenu = flyout.ConvertPlaceholderToSubMenu("OpenWithPlaceholder", Strings.OpenWith.GetLocalizedResource(), "App.ThemedIcons.OpenWith");
				var sendToSubMenu = flyout.ConvertPlaceholderToSubMenu("SendToPlaceholder", Strings.SendTo.GetLocalizedResource(), null);
				flyout.Show(anchor, position);
				await flyout.LoadClassicShellModelsAsync(shellMenu, overflowSubMenu, token, shellMenuItems =>
				{
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

					// The rest go under "Show more options"
					flyout.AddShellModels(shellMenuItems, overflowSubMenu, overflowSeparator, aboveExisting: false);

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

					FillOrRemove(openWithSubMenu, openWithItem, GetOpenWithItems);
					FillOrRemove(sendToSubMenu, sendToItem, GetSendToItems);
				});

				flyout.FinalizePrimaryRowPosition();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
	}
}
