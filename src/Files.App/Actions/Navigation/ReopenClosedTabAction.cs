// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.App.UserControls.MultitaskingControl;

namespace Files.App.Actions
{
	internal class ReopenClosedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "ReopenClosedTab".GetLocalizedResource();

		public string Description { get; } = "ReopenClosedTabDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.T, KeyModifiers.CtrlShift);

		public bool IsExecutable =>
			context.Control is not null &&
			!BaseMultitaskingControl.IsRestoringClosedTab &&
			BaseMultitaskingControl.RecentlyClosedTabs.Count > 0;

		public ReopenClosedTabAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
			BaseMultitaskingControl.StaticPropertyChanged += BaseMultitaskingControl_StaticPropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.ReopenClosedTab();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? _, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IMultitaskingContext.Control))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void BaseMultitaskingControl_StaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
