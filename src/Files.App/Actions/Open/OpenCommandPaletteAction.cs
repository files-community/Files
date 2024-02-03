// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenCommandPaletteAction : IAction
	{
		private readonly IContentPageContext _context;

		public string Label
			=> "CommandPalette".GetLocalizedResource();

		public string Description
			=> "OpenCommandPaletteDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.P, KeyModifiers.CtrlShift);

		public OpenCommandPaletteAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			_context.ShellPage?.ToolbarViewModel.OpenCommandPalette();

			return Task.CompletedTask;
		}
	}
}
