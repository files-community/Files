// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class NextTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext multitaskingContext;

		public string Label
			=> "NextTab".GetLocalizedResource();

		public string Description
			=> "NextTabDescription".GetLocalizedResource();

		public bool IsExecutable
			=> multitaskingContext.TabCount > 1;

		public HotKey HotKey
			=> new(Keys.Tab, KeyModifiers.Ctrl);

		public NextTabAction()
		{
			multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			App.AppModel.TabStripSelectedIndex = (App.AppModel.TabStripSelectedIndex + 1) % multitaskingContext.TabCount;

			return Task.CompletedTask;
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
