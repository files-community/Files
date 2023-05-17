// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.Data.Models
{
	public class ContextMenuFlyoutItemBuilder
	{
		private static readonly ContextMenuFlyoutItem defaultItem = new()
		{
			ShowItem = false,
			IsHidden = true
		};

		private readonly IRichCommand _command;

		private bool? _IsVisible;
		public bool IsVisible
		{
			get => _IsVisible ?? _command.IsExecutable;
			init => _IsVisible = value;
		}

		public bool IsPrimary { get; init; }

		public bool IsToggle { get; init; }

		public object Tag { get; init; }

		public bool ShowOnShift { get; init; }

		public List<ContextMenuFlyoutItem>? Items { get; init; }

		public ContextMenuFlyoutItemBuilder(IRichCommand command)
		{
			_command = command;
		}

		public ContextMenuFlyoutItem Build()
		{
			if (_IsVisible is false)
				return defaultItem;

			bool isExecutable = _command.IsExecutable;

			if (_IsVisible is null && !isExecutable)
				return defaultItem;

			ContextMenuFlyoutItemType type =
				IsToggle ? ContextMenuFlyoutItemType.Toggle : ContextMenuFlyoutItemType.Item;

			var viewModel = new ContextMenuFlyoutItem
			{
				Text = _command.Label,
				Tag = Tag,
				Command = _command,
				IsEnabled = isExecutable,
				IsChecked = _command.IsOn,
				IsPrimary = IsPrimary,
				Items = Items,
				ItemType = type,
				ShowItem = true,
				ShowOnShift = ShowOnShift,
				ShowInRecycleBin = true,
				ShowInSearchPage = true,
				ShowInFtpPage = true,
				ShowInZipPage = true,
			};

			var glyph = _command.Glyph;

			if (!string.IsNullOrEmpty(glyph.OpacityStyle))
			{
				viewModel.OpacityIcon = new()
				{
					OpacityIconStyle = glyph.OpacityStyle,
				};
			}
			else
			{
				viewModel.Glyph = glyph.BaseGlyph;
				viewModel.GlyphFontFamilyName = glyph.FontFamily;
			}

			if (_command.HotKeyText is not null)
				viewModel.KeyboardAcceleratorTextOverride = _command.HotKeyText;

			return viewModel;
		}
	}
}
