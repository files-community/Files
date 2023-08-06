﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class PreviousTabAction : ObservableObject, IAction
	{
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
			if (App.AppModel.TabStripSelectedIndex is 0)
				App.AppModel.TabStripSelectedIndex = multitaskingContext.TabCount - 1;
			else
				App.AppModel.TabStripSelectedIndex--;

			return Task.CompletedTask;
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
