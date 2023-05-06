// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class CloseTabsToTheLeftCurrentAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "CloseTabsToTheLeft".GetLocalizedResource();
		public string Description => "CloseTabsToTheLeftCurrentDescription".GetLocalizedResource();

		private bool isExecutable;
		public bool IsExecutable => isExecutable;

		public CloseTabsToTheLeftCurrentAction()
		{
			isExecutable = GetIsExecutable();
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.Control is not null)
			{
				MultitaskingTabsHelpers.CloseTabsToTheLeft(context.CurrentTabItem, context.Control);
			}
			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			return context.Control is not null && context.CurrentTabIndex > 0;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.SelectedTabIndex):
					SetProperty(ref isExecutable, GetIsExecutable(), nameof(IsExecutable));
					break;
			}
		}
	}
}
