using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class RedoAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Redo".GetLocalizedResource();

		public string Description { get; } = "RedoDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Y, KeyModifiers.Ctrl);

		public bool IsExecutable => context.ShellPage is not null &&
			context.PageType is not ContentPageTypes.SearchResults;

		public RedoAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return context.ShellPage!.StorageHistoryHelpers.TryRedo();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
