// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

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

		public object? ToIcon() => (object?)ToOpacityIcon() ?? ToFontIcon();

		public FontIcon? ToFontIcon()
		{
			if (IsNone)
				return null;

			var fontIcon = new FontIcon
			{
				Glyph = BaseGlyph
			};

			if (!string.IsNullOrEmpty(FontFamily))
				fontIcon.FontFamily = (FontFamily)Current.Resources[FontFamily];

			return fontIcon;
		}

		public OpacityIcon? ToOpacityIcon()
		{
			if (string.IsNullOrEmpty(OpacityStyle))
				return null;

			return new()
			{
				Style = (Style)Current.Resources[OpacityStyle]
			};
		}

		public Style? ToOpacityStyle()
		{
			if (string.IsNullOrEmpty(OpacityStyle))
				return null;
			return (Style)Current.Resources[OpacityStyle];
		}
	}
}
