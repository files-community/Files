using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class CloseSelectedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "CloseTab".GetLocalizedResource();

		public string Description { get; } = "CloseSelectedTabDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.W, KeyModifiers.Ctrl);

		public HotKey SecondHotKey { get; } = new(Keys.F4, KeyModifiers.Ctrl);

		public RichGlyph Glyph { get; } = new();

		public bool IsExecutable =>
			context.Control is not null &&
			context.TabCount > 0 &&
			context.CurrentTabItem is not null;

		public CloseSelectedTabAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.CloseTab(context.CurrentTabItem);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.CurrentTabItem):
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
