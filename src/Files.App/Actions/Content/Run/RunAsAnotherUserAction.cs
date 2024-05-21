﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class RunAsAnotherUserAction : BaseRunAsAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		public override string Label
			=> "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource();

		public override string Description
			=> "RunAsAnotherUserDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE7EE");

		public override bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
			!FileExtensionHelpers.IsAhkFile(ContentPageContext.SelectedItem.FileExtension) &&
			(FileExtensionHelpers.IsExecutableFile(ContentPageContext.SelectedItem.FileExtension) ||
			(ContentPageContext.SelectedItem is ShortcutItem shortcut &&
			shortcut.IsExecutable));

		public RunAsAnotherUserAction() : base("runasuser")
		{
		}
	}
}
