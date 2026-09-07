// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UserControls.Menus;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT;
namespace Files.App.Helpers.ContextFlyouts
{
	/// <summary>
	/// Provides static helper for conversion from ContextMenuFlyoutItemViewModels into a control.
	/// </summary>
	public static class ContextFlyoutModelToElementHelper
	{
		[DynamicWindowsRuntimeCast(typeof(MenuFlyoutSubItem))]
		public static List<MenuFlyoutItemBase>? GetMenuFlyoutItemsFromModel(List<ContextMenuFlyoutItemViewModel>? items)
		{
			if (items is null)
				return null;

			var flyout = new List<MenuFlyoutItemBase>();
			items.ForEach(i =>
			{
				var menuItem = GetMenuItem(i);
				flyout.Add(menuItem);
				if (menuItem is MenuFlyoutSubItem menuFlyoutSubItem && menuFlyoutSubItem.Items.Count == 0 && i.LoadSubMenuAction is null)
				{
					// Add a placeholder (only for genuinely-empty submenus; lazy shell submenus fill in later).
					menuItem.Visibility = Visibility.Collapsed;

					var placeholder = new MenuFlyoutItem()
					{
						Text = menuFlyoutSubItem.Text,
						Tag = menuFlyoutSubItem.Tag,
						Icon = menuFlyoutSubItem.Icon,
						AccessKey = i.AccessKey,
					};
					flyout.Add(placeholder);
				}
			});
			return flyout;
		}

		public static MenuFlyoutItemBase GetMenuItem(ContextMenuFlyoutItemViewModel item)
		{
			return item.ItemType switch
			{
				ContextMenuFlyoutItemType.Separator => new MenuFlyoutSeparator(),
				_ => GetMenuFlyoutItem(item),
			};
		}

		[DynamicWindowsRuntimeCast(typeof(Style))]
		private static MenuFlyoutItemBase GetMenuFlyoutItem(ContextMenuFlyoutItemViewModel item)
		{
			if (item.Items is not null)
			{
				var flyoutSubItem = new MenuFlyoutSubItem()
				{
					Text = item.Text,
					Tag = item.Tag,
					AccessKey = item.AccessKey,
				};

				if (item.BitmapIcon is not null)
				{
					flyoutSubItem.Style = App.Current.Resources["MenuFlyoutSubItemWithImageStyle"] as Style;
					try
					{
						MenuFlyoutSubItemCustomProperties.SetBitmapIcon(flyoutSubItem, item.BitmapIcon);
					}
					catch (Exception e)
					{
						Debug.WriteLine(e);
					}
				}
				else if (item.ThemedIconModel is { IsValid: true } themedIcon
					&& App.Current.Resources[themedIcon.ThemedIconStyle] is Style themedIconStyle)
				{
					// MenuFlyoutSubItem is sealed and its Icon must be an IconElement, so a ThemedIcon (a Control)
					// can't be set as the Icon directly. Draw it via the templated style, mirroring the image path.
					flyoutSubItem.Style = App.Current.Resources["MenuFlyoutSubItemWithThemedIconStyle"] as Style;
					MenuFlyoutSubItemCustomProperties.SetThemedIconStyle(flyoutSubItem, themedIconStyle);
				}
				else if (!string.IsNullOrEmpty(item.Glyph))
				{
					// Glyph-only submenus (Layout, Group by, ...): same custom template for consistent chevron/height, with
					// the glyph drawn in the icon slot.
					flyoutSubItem.Style = App.Current.Resources["MenuFlyoutSubItemWithThemedIconStyle"] as Style;
					flyoutSubItem.Icon = new FontIcon { Glyph = item.Glyph };
				}

				item.Items.ForEach(i =>
				{
					flyoutSubItem.Items.Add(GetMenuItem(i));
				});

				flyoutSubItem.IsEnabled = item.IsEnabled;
				flyoutSubItem.Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible;

				// Shell submenus (Send to, Open with, ...) load their children lazily. Fire the load and fill the
				// submenu in once it completes, keeping it visible meanwhile.
				if (item.LoadSubMenuAction is not null && flyoutSubItem.Items.Count == 0)
					FastContextFlyout.PopulateShellSubMenu(flyoutSubItem, item, static models => models[0].Items);

				return flyoutSubItem;
			}
			return GetItem(item);
		}

		[DynamicWindowsRuntimeCast(typeof(Style))]
		[DynamicWindowsRuntimeCast(typeof(FontFamily))]
		private static MenuFlyoutItemBase GetItem(ContextMenuFlyoutItemViewModel i)
		{
			if (i.BitmapIcon is not null)
			{
				var item = new MenuFlyoutItemWithImage()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					AccessKey = i.AccessKey,
				};
				try
				{
					item.BitmapIcon = i.BitmapIcon;
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
				}
				return item;
			}
			MenuFlyoutItem flyoutItem;

			if (i.ItemType is ContextMenuFlyoutItemType.Toggle)
			{
				flyoutItem = new ToggleMenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					IsChecked = i.IsChecked,
					AccessKey = i.AccessKey,
				};
				if (!string.IsNullOrEmpty(i.Glyph))
				{
					flyoutItem.Icon = new FontIcon { Glyph = i.Glyph };
				}
			}
			else if (i.ThemedIconModel.IsValid)
			{
				flyoutItem = new MenuFlyoutItemWithThemedIcon()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					ThemedIconStyle = App.Current.Resources[i.ThemedIconModel.ThemedIconStyle] as Style,
					AccessKey = i.AccessKey,
				};
			}
			else
			{
				var icon = string.IsNullOrEmpty(i.Glyph) ? null : new FontIcon
				{
					Glyph = i.Glyph,
				};

				if (icon is not null && !string.IsNullOrEmpty(i.GlyphFontFamilyName))
				{
					var fontFamily = App.Current.Resources[i.GlyphFontFamilyName] as FontFamily;
					icon.FontFamily = fontFamily;
				}

				flyoutItem = new MenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					Icon = icon,
					AccessKey = i.AccessKey,
				};
			}

			if (i.KeyboardAccelerator is not null)
			{
				flyoutItem.KeyboardAccelerators.Add(i.KeyboardAccelerator);
				// Fixes #16193: VirtualKey doesn't support OEM keys (e.g. "§"); hide the auto-generated
				// accelerator text so rendering it can't fault.
				flyoutItem.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
			}

			flyoutItem.IsEnabled = i.IsEnabled;
			flyoutItem.Visibility = i.IsHidden ? Visibility.Collapsed : Visibility.Visible;

			if (i.KeyboardAcceleratorTextOverride is not null)
				flyoutItem.KeyboardAcceleratorTextOverride = i.KeyboardAcceleratorTextOverride;

			return flyoutItem;
		}
	}
}
