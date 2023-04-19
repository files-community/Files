using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class ClosePaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "NavigationToolbarClosePane/Label".GetLocalizedResource();

		public string Description { get; } = "ClosePaneDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.W, KeyModifiers.CtrlShift);

		public RichGlyph Glyph { get; } = new("\uE89F");

		public bool IsExecutable => context.IsMultiPaneActive;

		public ClosePaneAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
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
