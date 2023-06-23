// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CloseOtherTabsCurrentAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> "CloseOtherTabs".GetLocalizedResource();

		public string Description
			=> "CloseOtherTabsCurrentDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public CloseOtherTabsCurrentAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.Control is not null)
				MultitaskingTabsHelpers.CloseOtherTabs(context.CurrentTabItem, context.Control);

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			return context.Control is not null && context.TabCount > 1;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
