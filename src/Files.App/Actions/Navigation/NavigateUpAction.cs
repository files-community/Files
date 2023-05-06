// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class NavigateUpAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Up".GetLocalizedResource();

		public string Description { get; } = "NavigateUpDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.Up, KeyModifiers.Menu);

		public RichGlyph Glyph { get; } = new("\uE74A");

		public bool IsExecutable => context.CanNavigateToParent;

		public NavigateUpAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.Up_Click();
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanNavigateToParent):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
