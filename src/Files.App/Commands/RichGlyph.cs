using Files.App.UserControls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Application;

namespace Files.App.Commands
{
	public readonly struct RichGlyph
	{
		public static RichGlyph None { get; } = new(string.Empty);

		public bool IsNone { get; }

		public string BaseGlyph { get; }
		public string FontFamily { get; }
		public string OpacityStyle { get; }

		public RichGlyph(string baseGlyph = "", string fontFamily = "", string opacityStyle = "")
		{
			BaseGlyph = baseGlyph;
			FontFamily = fontFamily;
			OpacityStyle = opacityStyle;

			IsNone = string.IsNullOrEmpty(baseGlyph) && string.IsNullOrEmpty(fontFamily) && string.IsNullOrEmpty(opacityStyle);
		}

		public void Deconstruct(out string baseGlyph, out string fontFamily, out string opacityStyle)
		{
			baseGlyph = BaseGlyph;
			fontFamily = FontFamily;
			opacityStyle = OpacityStyle;
		}

		public FontFamily ToFontFamily()
		{
			return string.IsNullOrEmpty(FontFamily)
				? App.AppModel.SymbolFontFamily
				: (FontFamily)Current.Resources[FontFamily];
		}

		public FontIcon? ToFontIcon()
		{
			if (IsNone)
				return null;

			var icon = new FontIcon
			{
				Glyph = BaseGlyph,
			};
			if (!string.IsNullOrEmpty(FontFamily))
				icon.FontFamily = (FontFamily)Current.Resources[FontFamily];

			return icon;
		}

		public OpacityIcon? ToOpacityIcon()
		{
			if (IsNone)
				return null;

			var icon = new OpacityIcon
			{
				Style = (Style)Current.Resources[OpacityStyle]
			};

			return icon;
		}
	}
}
