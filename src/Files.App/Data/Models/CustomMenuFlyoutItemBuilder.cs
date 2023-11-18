// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.System;

namespace Files.App.Data.Models
{
	public class CustomMenuFlyoutItemBuilder
	{
		private static readonly CustomMenuFlyoutItem none = new()
		{
			IsAvailable = false,
			IsVisible = false,
		};

		private readonly IRichCommand command;

		private bool? isVisible = null;
		public bool IsVisible
		{
			get => isVisible ?? command.IsExecutable;
			init => isVisible = value;
		}

		public bool IsPrimary { get; init; } = false;
		public bool IsToggle { get; init; } = false;
		public object? CommandParameter { get; init; }
		public object? Tag { get; init; }
		public bool ShowOnShift { get; init; } = false;
		public List<CustomMenuFlyoutItem>? Items { get; init; } = null;

		public CustomMenuFlyoutItemBuilder(IRichCommand command)
		{
			this.command = command;
		}

		public CustomMenuFlyoutItem Build()
		{
			if (isVisible is false)
				return none;

			if (CommandParameter is not null)
				command.Parameter = CommandParameter;

			bool isExecutable = command.IsExecutable;

			if (isVisible is null && !isExecutable)
				return none;

			ContextMenuFlyoutItemType type = IsToggle ? ContextMenuFlyoutItemType.Toggle : ContextMenuFlyoutItemType.Item;

			var viewModel = new CustomMenuFlyoutItem
			{
				Text = command.Label,
				Command = command,
				IsEnabled = isExecutable,
				IsChecked = command.IsOn,
				IsPrimary = IsPrimary,
				ItemType = type,
				IsAvailable = true,
				ShowOnShift = ShowOnShift,
				ShowInRecycleBin = true,
				ShowInSearchPage = true,
				ShowInFtpPage = true,
				ShowInZipPage = true,
			};

			if (Items is not null)
				viewModel.Items = Items;

			if (Tag is not null)
				viewModel.Tag = Tag;

			if (!string.IsNullOrEmpty(command.Glyph.OpacityStyle))
			{
				viewModel.OpacityIcon = new OpacityIconModel(command.Glyph.OpacityStyle);
			}
			else
			{
				viewModel.Glyph = command.Glyph.BaseGlyph;
				viewModel.GlyphFontFamilyName = command.Glyph.FontFamily;
			}

			if (command.HotKeys.Length > 0 &&
				!(command.HotKeys[0].Key is Keys.Enter &&
				command.HotKeys[0].Modifier is KeyModifiers.None))
			{
				viewModel.KeyboardAccelerator = new Microsoft.UI.Xaml.Input.KeyboardAccelerator()
				{
					Key = (VirtualKey)command.HotKeys[0].Key,
					Modifiers = (VirtualKeyModifiers)command.HotKeys[0].Modifier
				};
				viewModel.KeyboardAcceleratorTextOverride = command.HotKeys[0].Label;
			}

			return viewModel;
		}
	}
}
