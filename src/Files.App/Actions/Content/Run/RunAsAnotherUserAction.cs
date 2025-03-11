// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed partial class RunAsAnotherUserAction : BaseRunAsAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		public override string Label
			=> Strings.BaseLayoutContextFlyoutRunAsAnotherUser_Text.GetLocalizedResource();

		public override string Description
			=> Strings.RunAsAnotherUserDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE7EE");

		public override bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
			!FileExtensionHelpers.IsAhkFile(ContentPageContext.SelectedItem.FileExtension) &&
			(FileExtensionHelpers.IsExecutableFile(ContentPageContext.SelectedItem.FileExtension) ||
			(ContentPageContext.SelectedItem is IShortcutItem shortcut &&
			shortcut.IsExecutable));

		public RunAsAnotherUserAction() : base("runasuser")
		{
		}
	}
}
