// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class RunAsAnotherUserAction : BaseRunAsAction
	{
		private readonly IContentPageContext _context;

		public override string Label
			=> "BaseLayoutContextFlyoutRunAsAnotherUser/Text".GetLocalizedResource();

		public override string Description
			=> "RunAsAnotherUserDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE7EE");

		public override bool IsExecutable =>
			_context.SelectedItem is not null &&
			(FileExtensionHelpers.IsExecutableFile(_context.SelectedItem.FileExtension) ||
			(_context.SelectedItem is ShortcutItem shortcut &&
			shortcut.IsExecutable));

		public RunAsAnotherUserAction() : base("runasuser")
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}
	}
}
