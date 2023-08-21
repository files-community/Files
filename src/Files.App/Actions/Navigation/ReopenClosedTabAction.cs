// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.CustomTabView;

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
			!BaseCustomTabView.IsRestoringClosedTab &&
			BaseCustomTabView.RecentlyClosedTabs.Count > 0;

		public ReopenClosedTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
			BaseCustomTabView.StaticPropertyChanged += BaseMultitaskingControl_StaticPropertyChanged;
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
