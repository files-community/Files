// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Commands;

internal sealed class ExtractCommandGroup : CommandGroup
{
	public override string Name => "Extract";

	public override string DisplayName
		=> Strings.Extract.GetLocalizedResource();

	public override string Description
		=> Strings.ExtractGroupDescription.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.Zip");

	public override string AccessKey
		=> "Z";

	public override ActionCategory Category
		=> ActionCategory.Archive;

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
	public override string Name => "SetAs";

	public override string DisplayName
		=> Strings.SetAsBackgroundFlyout.GetLocalizedResource();

	public override string Description
		=> Strings.SetAsGroupDescription.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.SetWallpaper.16");

	public override string AccessKey
		=> "B";

	public override ActionCategory Category
		=> ActionCategory.Image;

	public override IReadOnlyList<CommandCodes> Commands =>
	[
		CommandCodes.SetAsWallpaperBackground,
		CommandCodes.SetAsLockscreenBackground,
		CommandCodes.SetAsAppBackground,
		CommandCodes.SetAsSlideshowBackground,
	];
}

internal sealed class NewItemCommandGroup : CommandGroup
{
	public override string Name => "NewItem";

	public override string DisplayName
		=> Strings.BaseLayoutContextFlyoutNew_Label.GetLocalizedResource();

	public override string Description
		=> Strings.NewItemGroupDescription.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.New.Item");

	public override string AccessKey
		=> "W";

	public override ActionCategory Category
		=> ActionCategory.Create;

	public override string AutomationId
		=> "InnerNavigationToolbarNewButton";

	public override IReadOnlyList<CommandCodes> Commands =>
	[
		CommandCodes.CreateFolder,
		CommandCodes.CreateFile,
		CommandCodes.CreateShortcutFromDialog,
	];
}

internal sealed class EditTagsCommandGroup : CommandGroup
{
	public override string Name => "EditTags";

	public override string DisplayName
		=> Strings.EditTags.GetLocalizedResource();

	public override string Description
		=> Strings.EditTagsGroupDescription.GetLocalizedResource();

	public override RichGlyph Glyph
		=> new(themedIconStyle: "App.ThemedIcons.TagEdit");

	public override string AccessKey
		=> string.Empty;

	public override ActionCategory Category
		=> ActionCategory.FileSystem;

	// No predefined commands — the flyout contents are built dynamically
	// from the user's defined file tags via FileTagsContextMenu.
	public override IReadOnlyList<CommandCodes> Commands => [];
}