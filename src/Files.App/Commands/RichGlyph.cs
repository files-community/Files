using Files.App.UserControls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Application;

namespace Files.App.Commands
{
	public readonly struct RichGlyph
	{
		public static RichGlyph None { get; } = new(string.Empty);

		public bool IsNone => string.IsNullOrEmpty(BaseGlyph);

		public string BaseGlyph { get; }
		public string OverlayGlyph { get; }
		public string FontFamily { get; }

		public RichGlyph(string baseGlyph, string overlayGlyph = "", string fontFamily = "")
			=> (BaseGlyph, OverlayGlyph, FontFamily) = (baseGlyph, overlayGlyph, fontFamily);

		public void Deconstruct(out string baseGlyph, out string overlayGlyph, out string fontFamily)
			=> (baseGlyph, overlayGlyph, fontFamily) = (BaseGlyph, OverlayGlyph, FontFamily);

		public FontFamily ToFontFamily() => string.IsNullOrEmpty(FontFamily)
			? App.AppModel.SymbolFontFamily
			: (FontFamily)Current.Resources[FontFamily];

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

		public ColoredIcon? ToColoredIcon()
		{
			if (IsNone)
				return null;

			var icon = new ColoredIcon
			{
				BaseLayerGlyph = BaseGlyph,
				OverlayLayerGlyph = OverlayGlyph,
			};
			if (!string.IsNullOrEmpty(FontFamily))
				icon.FontFamily = (FontFamily)Current.Resources[FontFamily];

			return icon;
		}
	}
}
