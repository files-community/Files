﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RefreshItemsAction : ObservableObject, IAction
	{
		private DateTime lastExecuted = DateTime.MinValue;

		private readonly TimeSpan debounceTime = TimeSpan.FromMilliseconds(800);

		private readonly IContentPageContext context;

		public string Label
			=> "Refresh".GetLocalizedResource();

		public string Description
			=> "RefreshItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE72C");

		public HotKey HotKey
			=> new(Keys.R, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.F5);

		public bool IsExecutable
			=> context.CanRefresh;

		public RefreshItemsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			DateTime now = DateTime.Now;

			if (now - lastExecuted < debounceTime)
			{
				// Too soon since the last execution, return immediately
				return;
			}

			lastExecuted = now;

			context.ShellPage?.Refresh_Click();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanRefresh):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
