// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CloseTabsToTheLeftSelectedAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> "CloseTabsToTheLeft".GetLocalizedResource();

		public string Description
			=> "CloseTabsToTheLeftSelectedDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public CloseTabsToTheLeftSelectedAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.Control is not null)
				MultitaskingTabsHelpers.CloseTabsToTheLeft(context.SelectedTabItem, context.Control);

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			return context.Control is not null && context.SelectedTabIndex > 0;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.SelectedTabIndex):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
