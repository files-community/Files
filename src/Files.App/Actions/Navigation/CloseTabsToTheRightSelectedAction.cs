using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class CloseTabsToTheRightSelectedAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "CloseTabsToTheRight".GetLocalizedResource();
		public string Description => "CloseTabsToTheRightSelectedDescription".GetLocalizedResource();

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public CloseTabsToTheRightSelectedAction()
		{
			isExecutable = GetIsExecutable();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.Control is not null)
			{
				MultitaskingTabsHelpers.CloseTabsToTheRight(context.SelectedTabItem, context.Control);
			}
			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			return context.Control is not null && context.SelectedTabIndex < context.TabCount - 1;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
				case nameof(IMultitaskingContext.SelectedTabIndex):
					SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
					break;
			}
		}
	}
}
