// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class EditPathAction : IAction
	{
		private readonly IContentPageContext context;

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
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			if (context.ShellPage is not null)
				context.ShellPage.ToolbarViewModel.IsEditModeEnabled = true;

			return Task.CompletedTask;
		}
	}
}
