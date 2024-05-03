// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Windows.Input;
using Windows.System;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents flyout item model for <see cref="MenuFlyoutItemBase"/> and <see cref="ICommandBarElement"/>.
	/// </summary>
	public sealed class ContextFlyoutItemModel
	{
		/// <summary>
		/// Gets flyout item type.
		/// </summary>
		public ContextFlyoutItemType ItemType { get; set; }

		/// <summary>
		/// Gets command that gets invoked when clicked.
		/// </summary>
		/// <remarks>
		/// Supports <see cref="IRichCommand"/> as well.
		/// </remarks>
		public ICommand? Command { get; set; }

		/// <summary>
		/// Gets command parameter that passed to its command when gets invoked.
		/// </summary>
		public object? CommandParameter { get; set; }

		/// <summary>
		/// Gets the text shown in the item.
		/// </summary>
		public string? Text { get; set; }

		/// <summary>
		/// Gets the opacity icon shown in the item.
		/// </summary>
		public OpacityIconModel? OpacityIcon { get; set; }

		/// <summary>
		/// Gets the icon shown in the item.
		/// </summary>
		public BitmapImage? BitmapIcon { get; set; }

		/// <summary>
		/// Gets the glyph shown in the item.
		/// </summary>
		public string? Glyph { get; set; }

		/// <summary>
		/// Gets the glyph's font family name.
		/// </summary>
		public string? GlyphFontFamilyName { get; set; }

		/// <summary>
		/// Gets the keyboard shortcut that can invoke the item.
		/// </summary>
		public KeyboardAccelerator? KeyBinding { get; set; }

		/// <summary>
		/// Gets the humanized keyboard shortcut.
		/// </summary>
		public string? KeyBindingHumanized { get; set; }

		/// <summary>
		/// Gets the tag that is handled internally.
		/// </summary>
		public object? Tag { get; set; }

		/// <summary>
		/// Gets the action to load sub menu items.
		/// </summary>
		public Func<Task>? LoadSubMenuAction { get; set; }

		/// <summary>
		/// Gets the sub menu items.
		/// </summary>
		public List<ContextFlyoutItemModel>? Items { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is available in the flyout.
		/// </summary>
		/// <remarks>
		/// If true, the item will be included in the flyout; otherwise the item will be discarded above all.
		/// </remarks>
		public bool IsAvailable { get; set; } = true;

		/// <summary>
		/// Gets the value that indicates whether the item is visible in the flyout.
		/// </summary>
		/// <remarks>
		/// If true, the item will be <see cref="Visibility.Visible"/> in the flyout; otherwise the item will be <see cref="Visibility.Collapsed"/>.
		/// </remarks>
		public bool IsVisible { get; set; } = true;

		/// <summary>
		/// Gets the value that indicates whether the item type is checked by default.
		/// </summary>
		/// <remarks>
		/// Used for <see cref="ToggleMenuFlyoutItem"/> and <see cref="AppBarToggleButton"/>.
		/// </remarks>
		public bool IsCheckedByDefault { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is enabled and cannot be interacted with.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Gets the value that indicates whether the item is included in the <see cref="CommandBarFlyout.PrimaryCommands"/>.
		/// </summary>
		public bool IsPrimary { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item's text is visible.
		/// </summary>
		public bool IsTextVisible { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible when user invoked the flyout with Shift key tapped.
		/// </summary>
		public bool IsVisibleOnShiftPressed { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in Recycle Bin folders.
		/// </summary>
		public bool IsVisibleInRecycleBinPage { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in Files Search page.
		/// </summary>
		public bool IsVisibleInSearchPage { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in FTP file system folders.
		/// </summary>
		public bool IsVisibleInFtpPage { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in archive folders.
		/// </summary>
		public bool IsVisibleInArchivePage { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is loading.
		/// </summary>
		public bool IsLoadingIndicatorVisible { get; set; }

		/// <summary>
		/// Initializes an instance of <see cref="ContextFlyoutItemModel"/>.
		/// </summary>
		public ContextFlyoutItemModel(ContextFlyoutItemType type = ContextFlyoutItemType.Button)
		{
			ItemType = type;
		}

		/// <summary>
		/// Initializes an instance of <see cref="ContextFlyoutItemModel"/> with <see cref="IRichCommand"/>.
		/// </summary>
		/// <param name="type">Flyout item type.</param>
		/// <param name="richCommand"><see cref="IRichCommand"/> instance.</param>
		/// <param name="richCommandParameter"><see cref="IRichCommand"/> parameter.</param>
		public ContextFlyoutItemModel(ContextFlyoutItemType type, IRichCommand richCommand, object? richCommandParameter = null) : this(type)
		{
			if (!richCommand.IsExecutable)
				return;

			Command = richCommand;
			Text = richCommand.Label;
			IsEnabled = richCommand.IsExecutable;
			IsCheckedByDefault = richCommand.IsOn;
			ItemType = type;
			IsAvailable = true;
			IsVisible = true;
			IsVisibleInRecycleBinPage = true;
			IsVisibleInSearchPage = true;
			IsVisibleInFtpPage = true;
			IsVisibleInArchivePage = true;

			if (!string.IsNullOrEmpty(richCommand.Glyph.OpacityStyle))
			{
				OpacityIcon = new(richCommand.Glyph.OpacityStyle);
			}
			else
			{
				Glyph = richCommand.Glyph.BaseGlyph;
				GlyphFontFamilyName = richCommand.Glyph.FontFamily;
			}

			if (richCommand.HotKeys.Length > 0 &&
				!(richCommand.HotKeys[0].Key is Keys.Enter &&
				richCommand.HotKeys[0].Modifier is KeyModifiers.None))
			{
				KeyBinding = new()
				{
					Key = (VirtualKey)richCommand.HotKeys[0].Key,
					Modifiers = (VirtualKeyModifiers)richCommand.HotKeys[0].Modifier
				};

				KeyBindingHumanized = richCommand.HotKeys[0].LocalizedLabel;
			}
		}
	}
}
