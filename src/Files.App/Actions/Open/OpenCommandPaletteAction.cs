// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenCommandPaletteAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CommandPalette".GetLocalizedResource();

		public string Description
			=> "OpenCommandPaletteDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.CtrlShift);

		public OpenCommandPaletteAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.ToolbarViewModel.OpenCommandPalette();

			return Task.CompletedTask;
		}
	}
}
