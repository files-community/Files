// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed class EditPathAction : IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.EditPath.GetLocalizedResource();

		public string Description
			=> Strings.EditPathDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.L, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.D, KeyModifiers.Alt);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Omnibar.Path");

		public EditPathAction()
		{

		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not null)
				context.ShellPage!.ToolbarViewModel.SwitchToPathMode();

			return Task.CompletedTask;
		}
	}
}
