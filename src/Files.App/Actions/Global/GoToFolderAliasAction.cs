// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
			=> new(Keys.Oem2);

		public HotKey SecondHotKey
			=> new(Keys.Divide);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Shortcut");

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not null)
				_ = context.ShellPage.ToolbarViewModel.SwitchToPathModeWithInputAsync(FolderAliasHelpers.Prefix.ToString());

			return Task.CompletedTask;
		}
	}
}
