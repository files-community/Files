// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Files.App.Data.Enums;

namespace Files.App.Data.Commands;

public abstract class CommandGroup
{
	public abstract string DisplayName { get; }
	public virtual string Description => DisplayName;
	public abstract RichGlyph Glyph { get; }
	public abstract string AccessKey { get; }
	public abstract IReadOnlyList<CommandCodes> Commands { get; }

	/// <summary>
	/// Gets the name used to identify this group in settings (e.g. "Extract", "SetAs", "NewItem").
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	/// Gets the category for organizing this group in the toolbar customization tree.
	/// </summary>
	public abstract ActionCategory Category { get; }

	public virtual string AutomationId => string.Empty;

	public object? Icon
		=> Glyph.ToIcon();

	public FontIcon? FontIcon
		=> Glyph.ToFontIcon();

	public Style? ThemedIconStyle
		=> Glyph.ToThemedIconStyle();
}