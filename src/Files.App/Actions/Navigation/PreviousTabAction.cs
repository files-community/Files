// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class PreviousTabAction : ObservableObject, IAction
	{
		private readonly AppModel _appModel = Ioc.Default.GetRequiredService<AppModel>();

		private readonly IMultitaskingContext multitaskingContext;

		public string Label
			=> "PreviousTab".GetLocalizedResource();

		public string Description
			=> "PreviousTabDescription".GetLocalizedResource();

		public bool IsExecutable
			=> multitaskingContext.TabCount > 1;

		public HotKey HotKey
			=> new(Keys.Tab, KeyModifiers.CtrlShift);

		public PreviousTabAction()
		{
			multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (_appModel.TabStripSelectedIndex is 0)
				_appModel.TabStripSelectedIndex = multitaskingContext.TabCount - 1;
			else
				_appModel.TabStripSelectedIndex--;

			return Task.CompletedTask;
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
