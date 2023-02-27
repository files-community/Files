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

		public bool? isVisible = null;
		public bool IsVisible
		{
			get => isVisible ?? command.IsExecutable;
			init => isVisible = value;
		}

		public bool IsPrimary { get; init; } = false;

		public ContextMenuFlyoutItemViewModelBuilder(IRichCommand command)
		{
			this.command = command;
		}

		public ContextMenuFlyoutItemViewModel Build()
		{
			if (isVisible is false)
				return none;

			bool isExecutable = command.IsExecutable;

			if (isVisible is null && !isExecutable)
				return none;

			var viewModel = new ContextMenuFlyoutItemViewModel
			{
				Text = command.Label,
				Command = command,
				IsEnabled = isExecutable,
				IsChecked = command.IsOn,
				IsPrimary = IsPrimary,
				ShowItem = true,
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
			else if (string.IsNullOrEmpty(glyph.OverlayGlyph))
			{
				viewModel.Glyph = glyph.BaseGlyph;
				viewModel.GlyphFontFamilyName = glyph.FontFamily;
			}
			else
			{
				viewModel.ColoredIcon = new ColoredIconModel
				{
					BaseLayerGlyph = glyph.BaseGlyph,
					OverlayLayerGlyph = glyph.OverlayGlyph,
				};
			}

			if (!command.CustomHotKey.IsNone)
				viewModel.KeyboardAcceleratorTextOverride = command.CustomHotKey.ToString();

			return viewModel;
		}
	}
}
