// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Windows.Input;
using Windows.System;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents flyout item model for <see cref="MenuFlyoutItem"/> and <see cref="CommandBarFlyout"/>.
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
		public KeyboardAccelerator? KeyboardAccelerator { get; set; }

		/// <summary>
		/// Gets the humanized keyboard shortcut.
		/// </summary>
		public string? KeyboardAcceleratorTextOverride { get; set; }

		/// <summary>
		/// Gets the tag that is handled internally.
		/// </summary>
		public object? Tag { get; set; }

		/// <summary>
		/// Gets the action to load sub menu items.
		/// </summary>
		public Func<Task>? LoadSubMenuAction { get; set; }

		public string? ID { get; set; }

		/// <summary>
		/// Gets the opacity icon shown in the item.
		/// </summary>
		public OpacityIconModel? OpacityIcon { get; set; }

		/// <summary>
		/// Gets the sub menu items.
		/// </summary>
		public List<ContextFlyoutItemModel>? Items { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is available in the flyout.
		/// </summary>
		public bool IsAvailable { get; set; } = true;

		/// <summary>
		/// Gets the value that indicates whether the item is visible in the flyout.
		/// </summary>
		public bool IsVisible { get; set; } = true;

		/// <summary>
		/// Gets the value that indicates whether the item type is Checkable and checked.
		/// </summary>
		public bool IsChecked { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is enabled and cannot be interacted with.
		/// </summary>
		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// Gets the value that indicates whether the item is included in the <see cref="CommandBarFlyout.PrimaryCommands"/>.
		/// </summary>
		public bool IsPrimary { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible when user invoked the flyout with Shift key tapped.
		/// </summary>
		public bool ShowOnShift { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in Recycle Bin folders.
		/// </summary>
		public bool ShowInRecycleBin { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in Files Search page.
		/// </summary>
		public bool ShowInSearchPage { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in FTP file system folders.
		/// </summary>
		public bool ShowInFtpPage { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is visible in archive folders.
		/// </summary>
		public bool ShowInZipPage { get; set; }

		public bool SingleItemOnly { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item's text is visible.
		/// </summary>
		public bool CollapseLabel { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the item is loading.
		/// </summary>
		public bool ShowLoadingIndicator { get; set; }

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
		/// <param name="richCommand"><see cref="IRichCommand"/> instance.</param>
		/// <param name="richCommandParameter"><see cref="IRichCommand"/> parameter.</param>
		public ContextFlyoutItemModel(ContextFlyoutItemType type, IRichCommand richCommand, object? richCommandParameter = null) : this(type)
		{
			Command = richCommand;

			//richCommand.Parameter = richCommandParameter;

			bool isExecutable = richCommand.IsExecutable;
			if (!isExecutable)
				return;

			Text = richCommand.Label;
			IsEnabled = isExecutable;
			IsChecked = richCommand.IsOn;
			ItemType = type;
			IsAvailable = true;
			IsVisible = true;
			ShowInRecycleBin = true;
			ShowInSearchPage = true;
			ShowInFtpPage = true;
			ShowInZipPage = true;

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
				KeyboardAccelerator = new KeyboardAccelerator()
				{
					Key = (VirtualKey)richCommand.HotKeys[0].Key,
					Modifiers = (VirtualKeyModifiers)richCommand.HotKeys[0].Modifier
				};

				KeyboardAcceleratorTextOverride = richCommand.HotKeys[0].LocalizedLabel;
			}
		}
	}
}
