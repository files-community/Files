using Files.App.Commands;
using Files.App.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public class ContextMenuFlyoutItemViewModel
	{
		public bool ShowItem { get; set; } = true;

		public ICommand Command { get; set; }

		public object CommandParameter { get; set; }

		public string Glyph { get; set; }

		public string GlyphFontFamilyName { get; set; }

		public string KeyboardAcceleratorTextOverride { get; set; }

		public string Text { get; set; }

		public object Tag { get; set; }

		public ItemType ItemType { get; set; }

		public bool IsSubItem { get; set; }

		public List<ContextMenuFlyoutItemViewModel> Items { get; set; }

		public BitmapImage BitmapIcon { get; set; }

		/// <summary>
		/// Only show the item when the shift key is held
		/// </summary>
		public bool ShowOnShift { get; set; }

		/// <summary>
		/// Only show when one item is selected
		/// </summary>
		public bool SingleItemOnly { get; set; }

		/// <summary>
		/// True if the item is shown in the recycle bin
		/// </summary>
		public bool ShowInRecycleBin { get; set; }

		/// <summary>
		/// True if the item is shown in search page
		/// </summary>
		public bool ShowInSearchPage { get; set; }

		/// <summary>
		/// True if the item is shown in FTP page
		/// </summary>
		public bool ShowInFtpPage { get; set; }

		/// <summary>
		/// True if the item is shown in ZIP archive page
		/// </summary>
		public bool ShowInZipPage { get; set; }

		public KeyboardAccelerator KeyboardAccelerator { get; set; }

		public bool IsChecked { get; set; }

		public bool IsEnabled { get; set; } = true;

		/// <summary>
		/// A unique identifier that can be used to save preferences for menu items
		/// </summary>
		public string ID { get; set; }

		public bool IsPrimary { get; set; }

		public bool CollapseLabel { get; set; }

		public ColoredIconModel ColoredIcon { get; set; }
		public OpacityIconModel OpacityIcon { get; set; }

		public bool ShowLoadingIndicator { get; set; }

		public bool IsHidden { get; set; }

		public ContextMenuFlyoutItemViewModel()
		{
		}
		public ContextMenuFlyoutItemViewModel(IRichCommand command)
		{
			Text = command.Label;
			Command = command;

			var glyph = command.Glyph;
			if (string.IsNullOrEmpty(glyph.OverlayGlyph))
			{
				Glyph = glyph.BaseGlyph;
				GlyphFontFamilyName = glyph.FontFamily;
			}
			else
			{
				ColoredIcon = new ColoredIconModel
				{
					BaseLayerGlyph = glyph.BaseGlyph,
					OverlayLayerGlyph = glyph.OverlayGlyph,
				};
			}

			ShowItem = command.IsExecutable;
			ShowInRecycleBin = ShowInSearchPage = ShowInFtpPage = ShowInZipPage = true;

			if (!command.CustomHotKey.IsNone)
				KeyboardAcceleratorTextOverride = command.CustomHotKey.ToString();
		}
	}

	public enum ItemType
	{
		Item,
		Separator,
		Toggle,
		SplitButton,
	}

	public struct ColoredIconModel
	{
		public string OverlayLayerGlyph { get; set; }

		public string BaseLayerGlyph { get; set; }

		public string BaseBackdropGlyph { get; set; }

		public ColoredIcon ToColoredIcon() => new()
		{
			OverlayLayerGlyph = OverlayLayerGlyph,
			BaseLayerGlyph = BaseLayerGlyph,
			BaseBackdropGlyph = BaseBackdropGlyph,
		};

		public bool IsValid => !string.IsNullOrEmpty(BaseLayerGlyph);
	}

	public struct OpacityIconModel
	{
		public Style OpacityIconStyle { get; set; }

		public OpacityIcon ToOpacityIconIcon() => new()
		{
			Style = OpacityIconStyle,
		};
	}
}
