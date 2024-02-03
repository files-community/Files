// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ClosePaneAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "NavigationToolbarClosePane/Label".GetLocalizedResource();

		public string Description
			=> "ClosePaneDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.W, KeyModifiers.CtrlShift);

		public RichGlyph Glyph
			=> new("\uE89F");

		public bool IsExecutable
			=> ContentPageContext.IsMultiPaneActive;

		public ClosePaneAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			ContentPageContext.ShellPage!.PaneHolder.CloseActivePane();

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
