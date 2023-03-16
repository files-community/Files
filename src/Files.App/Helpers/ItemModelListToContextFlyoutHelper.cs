using Files.App.UserControls;
using Files.App.ViewModels;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Files.App.Helpers.ContextFlyouts
{
	/// <summary>
	/// This helper class is used to convert ContextMenuFlyoutItemViewModels into a control that can be displayed to the user.
	/// This is for use in scenarios where XAML templates and data binding will not suffice.
	/// <see cref="Files.App.ViewModels.ContextMenuFlyoutItemViewModel"/>
	/// </summary>
	public static class ItemModelListToContextFlyoutHelper
	{
		public static List<MenuFlyoutItemBase>? GetMenuFlyoutItemsFromModel(List<ContextMenuFlyoutItemViewModel>? items)
		{
			if (items is null)
				return null;

			var flyout = new List<MenuFlyoutItemBase>();
			items.ForEach(i =>
			{
				flyout.Add(GetMenuItem(i));
			});
			return flyout;
		}

		public static (List<ICommandBarElement> primaryElements, List<ICommandBarElement> secondaryElements) GetAppBarItemsFromModel(List<ContextMenuFlyoutItemViewModel> items)
		{
			var primaryModels = items.Where(i => i.IsPrimary).ToList();
			var secondaryModels = items.Except(primaryModels).ToList();

			if (!secondaryModels.IsEmpty() && secondaryModels.Last().ItemType is ItemType.Separator)
				secondaryModels.RemoveAt(secondaryModels.Count - 1);

			var primary = new List<ICommandBarElement>();
			primaryModels.ForEach(i => primary.Add(GetCommandBarItem(i)));
			var secondary = new List<ICommandBarElement>();
			secondaryModels.ForEach(i => secondary.Add(GetCommandBarItem(i)));

			return (primary, secondary);
		}

		/// <summary>
		/// Same as GetAppBarItemsFromModel, but ignores the IsPrimary property and returns one list
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static List<ICommandBarElement> GetAppBarButtonsFromModelIgnorePrimary(List<ContextMenuFlyoutItemViewModel> items)
		{
			var elements = new List<ICommandBarElement>();
			items.ForEach(i => elements.Add(GetCommandBarItem(i)));
			return elements;
		}

		private static MenuFlyoutItemBase GetMenuItem(ContextMenuFlyoutItemViewModel item)
		{
			return item.ItemType switch
			{
				ItemType.Separator => new MenuFlyoutSeparator(),
				_ => GetMenuFlyoutItem(item),
			};
		}

		private static MenuFlyoutItemBase GetMenuFlyoutItem(ContextMenuFlyoutItemViewModel item)
		{
			if (item.Items?.Count > 0)
			{
				var flyoutSubItem = new MenuFlyoutSubItem()
				{
					Text = item.Text,
					Tag = item.Tag,
				};
				item.Items?.ForEach(i =>
				{
					flyoutSubItem.Items.Add(GetMenuItem(i));
				});
				return flyoutSubItem;
			}
			return GetItem(item);
		}

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

			if (i.ItemType is ItemType.Toggle)
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
				{
					flyoutItem.Icon = new FontIcon{ Glyph = i.Glyph };
				}
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
				};
			}

			if (i.KeyboardAccelerator is not null)
				flyoutItem.KeyboardAccelerators.Add(i.KeyboardAccelerator);
			flyoutItem.IsEnabled = i.IsEnabled;

			if (i.KeyboardAcceleratorTextOverride is not null)
				flyoutItem.KeyboardAcceleratorTextOverride = i.KeyboardAcceleratorTextOverride;

			return flyoutItem;
		}

		public static ICommandBarElement GetCommandBarItem(ContextMenuFlyoutItemViewModel item)
		{
			return item.ItemType switch
			{
				ItemType.Separator => new AppBarSeparator()
				{
					Tag = item.Tag,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				},
				_ => GetCommandBarButton(item),
			};
		}

		private static ICommandBarElement GetCommandBarButton(ContextMenuFlyoutItemViewModel item)
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
					var fontFamily = App.Current.Resources[item.GlyphFontFamilyName] as FontFamily;
					icon.FontFamily = fontFamily;
				}
			}

			MenuFlyout? ctxFlyout = null;
			if ((item.Items is not null && item.Items.Count > 0) || item.ID == "ItemOverflow")
			{
				ctxFlyout = new MenuFlyout();
				GetMenuFlyoutItemsFromModel(item.Items)?.ForEach(i => ctxFlyout.Items.Add(i));
			}

			UIElement? content = null;
			if (item.BitmapIcon is not null)
				content = new Image()
				{
					Source = item.BitmapIcon,
				};
			else if (item.OpacityIcon.IsValid)
				content = item.OpacityIcon.ToOpacityIcon();
			else if (item.ShowLoadingIndicator)
				content = new ProgressRing()
				{
					IsIndeterminate = true,
					IsActive = true,
				};

			if (item.ItemType is ItemType.Toggle)
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
				}
			}

			return element;
		}
	}
}