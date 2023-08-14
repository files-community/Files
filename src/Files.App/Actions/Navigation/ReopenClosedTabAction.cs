// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabView;

namespace Files.App.Actions
{
	internal class ReopenClosedTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> "ReopenClosedTab".GetLocalizedResource();

		public string Description
			=> "ReopenClosedTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.T, KeyModifiers.CtrlShift);

		public bool IsExecutable =>
			context.Control is not null &&
			!BaseTabView.IsRestoringClosedTab &&
			BaseTabView.RecentlyClosedTabs.Count > 0;

		public ReopenClosedTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
			BaseTabView.StaticPropertyChanged += BaseMultitaskingControl_StaticPropertyChanged;
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
