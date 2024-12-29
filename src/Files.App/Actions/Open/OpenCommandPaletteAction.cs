// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenCommandPaletteAction : IAction
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

		public Task ExecuteAsync(object? parameter = null)
		{
			_context.ShellPage?.ToolbarViewModel.OpenCommandPalette();

			return Task.CompletedTask;
		}
	}
}
