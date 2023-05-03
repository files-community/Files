// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.ViewModels
{
	public class ContextMenuFlyoutItemViewModelBuilder
	{
		private static readonly ContextMenuFlyoutItemViewModel none = new()
		{
			ShowItem = false,
			IsHidden = true,
		};

		private readonly IRichCommand command;

		private bool? _IsVisible = null;
		public bool? IsVisible
		{
			get => _IsVisible ?? command.IsExecutable;
			init => _IsVisible = value;
		}

		public bool IsPrimary { get; init; } = false;

		public bool IsToggle { get; init; } = false;

		public object Tag { get; init; }

		public bool ShowOnShift { get; init; } = false;

		public List<ContextMenuFlyoutItemViewModel>? Items { get; init; } = null;

		public ContextMenuFlyoutItemViewModelBuilder(IRichCommand command)
		{
			this.command = command;
		}

		public ContextMenuFlyoutItemViewModel Build()
		{
			if (IsVisible is false)
				return none;

			bool isExecutable = command.IsExecutable;

			if (_IsVisible is null && !isExecutable)
				return none;

			ItemType type = IsToggle ? ItemType.Toggle : ItemType.Item;

			var viewModel = new ContextMenuFlyoutItemViewModel
			{
				Text = command.Label,
				Tag = Tag,
				Command = command,
				IsEnabled = isExecutable,
				IsChecked = command.IsOn,
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

			var glyph = command.Glyph;
			if (!string.IsNullOrEmpty(glyph.OpacityStyle))
			{
				viewModel.OpacityIcon = new OpacityIconModel
				{
					OpacityIconStyle = glyph.OpacityStyle,
				};
			}
			else
			{
				viewModel.Glyph = glyph.BaseGlyph;
				viewModel.GlyphFontFamilyName = glyph.FontFamily;
			}

			if (command.HotKeyText is not null)
				viewModel.KeyboardAcceleratorTextOverride = command.HotKeyText;

			return viewModel;
		}
	}
}
