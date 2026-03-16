// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Commands;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions;

public abstract class CommandGroup
{
	public abstract string DisplayName { get; }
	public abstract RichGlyph Glyph { get; }
	public abstract IReadOnlyList<CommandCodes> Commands { get; }

	public object? Icon
		=> Glyph.ToIcon();

	public FontIcon? FontIcon
		=> Glyph.ToFontIcon();

	public Style? ThemedIconStyle
		=> Glyph.ToThemedIconStyle();
}

internal sealed class ExtractCommandGroup : CommandGroup
{
	public override string DisplayName
		=> Strings.Extract.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.Zip");

	public override IReadOnlyList<CommandCodes> Commands =>
	[
		CommandCodes.DecompressArchive,
		CommandCodes.DecompressArchiveHereSmart,
		CommandCodes.DecompressArchiveHere,
		CommandCodes.DecompressArchiveToChildFolder,
	];
}

internal sealed class SetAsCommandGroup : CommandGroup
{
	public override string DisplayName
		=> Strings.SetAsBackgroundFlyout.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.SetWallpaper.16");

	public override IReadOnlyList<CommandCodes> Commands =>
	[
		CommandCodes.SetAsWallpaperBackground,
		CommandCodes.SetAsLockscreenBackground,
		CommandCodes.SetAsAppBackground,
	];
}

internal sealed class NewItemCommandGroup : CommandGroup
{
	public override string DisplayName
		=> Strings.BaseLayoutContextFlyoutNew_Label.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.New.Item");

	public override IReadOnlyList<CommandCodes> Commands =>
	[
		CommandCodes.CreateFolder,
		CommandCodes.CreateFile,
		CommandCodes.CreateShortcutFromDialog,
	];
}
