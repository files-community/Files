// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class EditPathAction : IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "EditPath".GetLocalizedResource();

		public string Description
			=> "EditPathDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.L, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.D, KeyModifiers.Menu);

		public EditPathAction()
		{
		}

		public Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is not null)
				ContentPageContext.ShellPage.ToolbarViewModel.IsEditModeEnabled = true;

			return Task.CompletedTask;
		}
	}
}
