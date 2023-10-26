// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabBar;

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
			!BaseTabBar.IsRestoringClosedTab &&
			BaseTabBar.RecentlyClosedTabs.Count > 0;

		public ReopenClosedTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
			BaseTabBar.StaticPropertyChanged += BaseMultitaskingControl_StaticPropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.Control!.ReopenClosedTabAsync();

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
