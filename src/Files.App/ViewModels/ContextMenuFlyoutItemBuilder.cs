// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.ViewModels
{
	/// <summary>
	/// Represents a builder class that create a <see cref="ContextMenuFlyoutItem"/> instance.
	/// </summary>
	public class ContextMenuFlyoutItemBuilder
	{
		private static readonly ContextMenuFlyoutItem defaultItem = new()
		{
			ShowItem = false,
			IsHidden = true,
		};

		private readonly IRichCommand _command;

		private bool? _IsVisible = null;

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is visible.
		/// </summary>
		public bool? IsVisible
		{
			get => _IsVisible ?? _command.IsExecutable;
			init => _IsVisible = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is in the primary menu flyout.
		/// </summary>
		public bool IsPrimary { get; init; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is toggle button style.
		/// </summary>
		public bool IsToggle { get; init; }

		/// <summary>
		/// Gets or sets an arbitrary object value that can be used to store custom information about this object.
		/// </summary>
		public object? Tag { get; init; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu flyout item is displayed only when the shift key is held.
		/// </summary>
		public bool ShowOnShift { get; init; }

		/// <summary>
		/// Gets or sets a list that is the List of ContextMenuFlyoutItem
		/// </summary>
		public List<ContextMenuFlyoutItem>? Items { get; init; } = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="ContextMenuFlyoutItemBuilder"/> class.
		/// </summary>
		/// <param name="command"></param>
		public ContextMenuFlyoutItemBuilder(IRichCommand command)
		{
			_command = command;
		}

		/// <summary>
		/// Build a <see cref="ContextMenuFlyoutItem"/> instance from a <see cref="ContextMenuFlyoutItemBuilder"/> instance.
		/// </summary>
		public ContextMenuFlyoutItem Build()
		{
			if (IsVisible is false)
				return defaultItem;

			bool isExecutable = _command.IsExecutable;

			if (_IsVisible is null && !isExecutable)
				return defaultItem;

			ContextMenuFlyoutItemType type = IsToggle ? ContextMenuFlyoutItemType.Toggle : ContextMenuFlyoutItemType.Item;

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

			if (_command.HotKeyText is not null)
				viewModel.KeyboardAcceleratorTextOverride = _command.HotKeyText;

			return viewModel;
		}
	}
}
