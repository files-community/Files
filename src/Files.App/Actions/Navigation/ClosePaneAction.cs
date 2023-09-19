// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ClosePaneAction : ObservableObject, IAction
	{
		private DateTime lastExecuted = DateTime.MinValue;

		private readonly TimeSpan debounceTime = TimeSpan.FromMilliseconds(800);

		private readonly IContentPageContext context;

		public string Label
			=> "NavigationToolbarClosePane/Label".GetLocalizedResource();

		public string Description
			=> "ClosePaneDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.W, KeyModifiers.CtrlShift);

		public RichGlyph Glyph
			=> new("\uE89F");

		public bool IsExecutable
			=> context.IsMultiPaneActive;

		public ClosePaneAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			DateTime now = DateTime.Now;

			if (now - lastExecuted < debounceTime) // Execute only if enough time has passed since the last execution
			{
				return Task.CompletedTask;
			}
			
			lastExecuted = now;

			context.ShellPage!.PaneHolder.CloseActivePane();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
