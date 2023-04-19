using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class SearchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Search".GetLocalizedResource();

		public string Description { get; } = "SearchDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.F, KeyModifiers.Ctrl);
		public HotKey SecondHotKey { get; } = new(Keys.F3);

		public RichGlyph Glyph { get; } = new();

		public bool IsExecutable => !context.IsSearchBoxVisible;

		public SearchAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.ToolbarViewModel.SwitchSearchBoxVisibility();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsSearchBoxVisible):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
