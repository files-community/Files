using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Application;

namespace Files.App.Commands
{
	public class Glyph : IGlyph
	{
		public static Glyph None { get; } = new();

		public string Base { get; init; } = string.Empty;
		public string Overlay { get; init; } = string.Empty;
		public string Family { get; init; } = string.Empty;

		public FontFamily FontFamily => !string.IsNullOrEmpty(Family)
			? (FontFamily)Current.Resources[Family]
			: App.AppModel.SymbolFontFamily;

		public Glyph() {}
		public Glyph(string glyphBase) : this() => Base = glyphBase;
		public Glyph(string glyphBase, string glyphOverlay) : this(glyphBase) => Overlay = glyphOverlay;
	}
}
