// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Commands;

public abstract class CommandGroup
{
	public abstract string DisplayName { get; }
	public abstract RichGlyph Glyph { get; }
	public abstract string AccessKey { get; }
	public abstract IReadOnlyList<CommandCodes> Commands { get; }

	public object? Icon
		=> Glyph.ToIcon();

	public FontIcon? FontIcon
		=> Glyph.ToFontIcon();

	public Style? ThemedIconStyle
		=> Glyph.ToThemedIconStyle();
}