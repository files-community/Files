// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Factories
{
	/// <summary>
	/// This helper class is used to convert ContextMenuFlyoutItemViewModels into a control that can be displayed to the user.
	/// This is for use in scenarios where XAML templates and data binding will not suffice.
	/// </summary>
	public static class MenuFlyoutFactory
	{
		public static List<MenuFlyoutItemBase>? GetMenuFlyoutItemsFromModel(List<CustomMenuFlyoutItem>? items)
		{
			if (items is null)
				return null;

			var flyout = new List<MenuFlyoutItemBase>();

			foreach (var item in items)
			{
				var menuItem = GetMenuItem(item);

				flyout.Add(menuItem);

				// Add a placeholder
				if (menuItem is MenuFlyoutSubItem menuFlyoutSubItem &&
					menuFlyoutSubItem.Items.Count == 0)
				{
					menuItem.Visibility = Visibility.Collapsed;

					var placeHolder = new MenuFlyoutItem()
					{
						Text = menuFlyoutSubItem.Text,
						Tag = menuFlyoutSubItem.Tag,
						Icon = menuFlyoutSubItem.Icon,
					};

					flyout.Add(placeHolder);
				}
			}

			return flyout;
		}

		public static (List<ICommandBarElement> primaryElements, List<ICommandBarElement> secondaryElements) GetAppBarItemsFromModel(List<CustomMenuFlyoutItem> items)
		{
			// Get primary items
			var primaryModels = items.Where(i => i.IsPrimary).ToList();

			// Get secondary items
			var secondaryModels = items.Except(primaryModels).ToList();

			// Remove the separator if the last item is a separator
			if (!secondaryModels.IsEmpty() &&
				secondaryModels.Last().ItemType is ContextMenuFlyoutItemType.Separator)
				secondaryModels.RemoveAt(secondaryModels.Count - 1);

			// Generate primary command bar
			var primary = new List<ICommandBarElement>();
			primaryModels.ForEach(i => primary.Add(GetCommandBarItem(i)));

			// Generate secondary command bar
			var secondary = new List<ICommandBarElement>();
			secondaryModels.ForEach(i => secondary.Add(GetCommandBarItem(i)));

			return (primary, secondary);
		}

		public static List<ICommandBarElement> GetAppBarButtonsFromModelIgnorePrimary(List<CustomMenuFlyoutItem> items)
		{
			// Generate command bar from all items
			var elements = new List<ICommandBarElement>();
			items.ForEach(i => elements.Add(GetCommandBarItem(i)));

			return elements;
		}

		public static MenuFlyoutItemBase GetMenuItem(CustomMenuFlyoutItem item)
		{
			return item.ItemType switch
			{
				ContextMenuFlyoutItemType.Separator => new MenuFlyoutSeparator(),
				_ => GetMenuFlyoutItem(item),
			};
		}

		private static MenuFlyoutItemBase GetMenuFlyoutItem(CustomMenuFlyoutItem item)
		{
			// Return single item
			if (item.Items is null)
				return GetItem(item);

			// Get sub items
			var flyoutSubItem = new MenuFlyoutSubItem()
			{
				Text = item.Text,
				Tag = item.Tag,
			};

			item.Items.ForEach(i =>
			{
				flyoutSubItem.Items.Add(GetMenuItem(i));
			});

			flyoutSubItem.IsEnabled = item.IsEnabled;
			flyoutSubItem.Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible;

			return flyoutSubItem;
		}

		private static MenuFlyoutItemBase GetItem(CustomMenuFlyoutItem i)
		{
			// Generate image item
			if (i.BitmapIcon is not null)
			{
				var item = new MenuFlyoutItemWithImage()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
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

			// Generate toggleable item
			if (i.ItemType is ContextMenuFlyoutItemType.Toggle)
			{
				flyoutItem = new ToggleMenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					IsChecked = i.IsChecked,
				};

				if (!string.IsNullOrEmpty(i.Glyph))
					flyoutItem.Icon = new FontIcon { Glyph = i.Glyph };
			}
			else if (i.Icon is not null)
			{
				flyoutItem = new MenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					Icon = i.Icon,
				};
			}
			// Generate default item
			else
			{
				var icon = string.IsNullOrEmpty(i.Glyph)
					? null
					: new FontIcon { Glyph = i.Glyph };

				if (icon is not null && !string.IsNullOrEmpty(i.GlyphFontFamilyName))
				{
					var fontFamily = Application.Current.Resources[i.GlyphFontFamilyName] as FontFamily;
					icon.FontFamily = fontFamily;
				}

				flyoutItem = new MenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					Icon = icon,
				};
			}

			// Set keyboard accelerators
			if (i.KeyboardAccelerator is not null)
				flyoutItem.KeyboardAccelerators.Add(i.KeyboardAccelerator);

			if (i.KeyboardAcceleratorTextOverride is not null)
				flyoutItem.KeyboardAcceleratorTextOverride = i.KeyboardAcceleratorTextOverride;

			// Set accessibilities
			flyoutItem.IsEnabled = i.IsEnabled;
			flyoutItem.Visibility = i.IsHidden ? Visibility.Collapsed : Visibility.Visible;

			return flyoutItem;
		}

		public static ICommandBarElement GetCommandBarItem(CustomMenuFlyoutItem item)
		{
			return item.ItemType switch
			{
				ContextMenuFlyoutItemType.Separator => new AppBarSeparator()
				{
					Tag = item.Tag,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				},
				_ => GetCommandBarButton(item),
			};
		}

		private static ICommandBarElement GetCommandBarButton(CustomMenuFlyoutItem item)
		{
			ICommandBarElement element;
			FontIcon? icon = null;

			if (!string.IsNullOrEmpty(item.Glyph))
			{
				icon = new FontIcon
				{
					Glyph = item.Glyph,
				};

				if (!string.IsNullOrEmpty(item.GlyphFontFamilyName))
				{
					var fontFamily = Application.Current.Resources[item.GlyphFontFamilyName] as FontFamily;
					icon.FontFamily = fontFamily;
				}
			}

			MenuFlyout? ctxFlyout = null;

			if (item.Items is not null && item.Items.Count > 0 || item.ID == "ItemOverflow")
			{
				ctxFlyout = new MenuFlyout();
				GetMenuFlyoutItemsFromModel(item.Items)?.ForEach(i => ctxFlyout.Items.Add(i));
			}

			UIElement? content = null;

			if (item.BitmapIcon is not null)
			{
				content = new Image()
				{
					Source = item.BitmapIcon,
				};
			}
			else if (item.OpacityIcon.IsValid)
			{
				content = item.OpacityIcon.ToOpacityIcon();
			}
			else if (item.Icon is not null)
			{
				content = item.Icon;
			}
			else if (item.ShowLoadingIndicator)
			{
				content = new ProgressRing()
				{
					IsIndeterminate = true,
					IsActive = true,
				};
			}

			if (item.ItemType is ContextMenuFlyoutItemType.Toggle)
			{
				element = new AppBarToggleButton()
				{
					Label = item.Text,
					Tag = item.Tag,
					Command = item.Command,
					CommandParameter = item.CommandParameter,
					IsChecked = item.IsChecked,
					Content = content,
					LabelPosition = item.CollapseLabel ? CommandBarLabelPosition.Collapsed : CommandBarLabelPosition.Default,
					IsEnabled = item.IsEnabled,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				};

				if (element is AppBarToggleButton toggleButton)
				{
					if (icon is not null)
						toggleButton.Icon = icon;

					if (item.IsPrimary || item.CollapseLabel)
						toggleButton.SetValue(ToolTipService.ToolTipProperty, item.Text);

					if (item.KeyboardAccelerator is not null && item.KeyboardAcceleratorTextOverride is not null)
					{
						toggleButton.KeyboardAccelerators.Add(item.KeyboardAccelerator);
						toggleButton.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
					}
				}
			}
			else
			{
				element = new AppBarButton()
				{
					Label = item.Text,
					Tag = item.Tag,
					Command = item.Command,
					CommandParameter = item.CommandParameter,
					Flyout = ctxFlyout,
					LabelPosition = item.CollapseLabel ? CommandBarLabelPosition.Collapsed : CommandBarLabelPosition.Default,
					Content = content,
					IsEnabled = item.IsEnabled,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				};

				if (element is AppBarButton button)
				{
					if (icon is not null)
						button.Icon = icon;

					if (item.IsPrimary || item.CollapseLabel)
						button.SetValue(ToolTipService.ToolTipProperty, item.Text);

					if (item.KeyboardAccelerator is not null && item.KeyboardAcceleratorTextOverride is not null)
					{
						button.KeyboardAccelerators.Add(item.KeyboardAccelerator);
						button.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
					}
				}
			}

			return element;
		}
	}
}
