// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Windows.Win32;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class GoToFolderAliasAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.GoToFolderAlias.GetLocalizedResource();

		public string Description
			=> Strings.GoToFolderAliasDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Navigation;

		public HotKey HotKey
			=> GetSlashHotKey();

		public HotKey SecondHotKey
			=> new(Keys.Divide);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Shortcut");

		public Task ExecuteAsync(object? parameter = null)
		{
			return context.ShellPage?.ToolbarViewModel.SwitchToPathModeWithInputAsync(FolderAliasHelpers.Prefix)
				?? Task.CompletedTask;
		}

		private static HotKey GetSlashHotKey()
		{
			// The low byte is the virtual key and the high byte the modifiers it needs
			var scan = PInvoke.VkKeyScan('/');

			return scan is not -1 && scan >> 8 is 0
				? new((Keys)(scan & 0xFF))
				: HotKey.None;
		}
	}
}
