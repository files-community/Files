// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class NavigateUpAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Up".GetLocalizedResource();

		public string Description
			=> "NavigateUpDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Up, KeyModifiers.Menu);

		public RichGlyph Glyph
			=> new("\uE74A");

		public bool IsExecutable
			=> ContentPageContext.CanNavigateToParent;

		public NavigateUpAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			ContentPageContext.ShellPage!.Up_Click();

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
