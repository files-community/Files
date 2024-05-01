// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Windows.Input;
using Windows.System;

namespace Files.App.Data.Models
{
	public sealed class ContextFlyoutItemModel
	{
		public ContextFlyoutItemType ItemType { get; set; }

		public ICommand? Command { get; set; }

		public object? CommandParameter { get; set; }

		public string? Text { get; set; }

		public BitmapImage? BitmapIcon { get; set; }

		public string? Glyph { get; set; }

		public string? GlyphFontFamilyName { get; set; }

		public KeyboardAccelerator? KeyboardAccelerator { get; set; }

		public string? KeyboardAcceleratorTextOverride { get; set; }

		public object? Tag { get; set; }

		public Func<Task>? LoadSubMenuAction { get; set; }

		public string? ID { get; set; }

		public OpacityIconModel? OpacityIcon { get; set; }

		public List<ContextFlyoutItemModel>? Items { get; set; }

		public bool IsAvailable { get; set; } = true;
		public bool IsVisible { get; set; } = true;
		public bool IsChecked { get; set; }
		public bool IsEnabled { get; set; } = true;
		public bool IsPrimary { get; set; }

		public bool ShowOnShift { get; set; }
		public bool ShowInRecycleBin { get; set; }
		public bool ShowInSearchPage { get; set; }
		public bool ShowInFtpPage { get; set; }
		public bool ShowInZipPage { get; set; }

		public bool SingleItemOnly { get; set; }

		public bool CollapseLabel { get; set; }

		public bool ShowLoadingIndicator { get; set; }

		/// <summary>
		/// Initializes an instance of <see cref="CustomMenuFlyoutItem"/>.
		/// </summary>
		public ContextFlyoutItemModel(ContextFlyoutItemType type = ContextFlyoutItemType.Button)
		{
			ItemType = type;
		}

		/// <summary>
		/// Initializes an instance of <see cref="CustomMenuFlyoutItem"/> with <see cref="IRichCommand"/>.
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
