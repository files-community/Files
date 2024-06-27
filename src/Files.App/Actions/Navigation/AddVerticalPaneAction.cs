﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class AddVerticalPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "AddVerticalPane".GetLocalizedResource();

		public string Description
			=> "AddVerticalPaneDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.AltShift);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconAddVerticalPane");

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneEnabled &&
			!ContentPageContext.IsMultiPaneActive;

		public AddVerticalPaneAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage!.PaneHolder.AddVerticalPane();

			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsMultiPaneEnabled):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
