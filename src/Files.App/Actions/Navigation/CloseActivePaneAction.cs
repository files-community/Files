﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class CloseActivePaneAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "CloseActivePane".GetLocalizedResource();

		public string Description
			=> "CloseActivePaneDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.W, KeyModifiers.CtrlShift);

		public RichGlyph Glyph
			=> new("\uE89F");

		public bool IsExecutable
			=> ContentPageContext.IsMultiPaneActive;

		public CloseActivePaneAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage?.PaneHolder.CloseActivePane();
			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
