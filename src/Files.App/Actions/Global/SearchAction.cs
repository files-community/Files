// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class SearchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Search".GetLocalizedResource();

		public string Description
			=> "SearchDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.F3);

		public RichGlyph Glyph
			=> new();

		public bool IsExecutable
			=> !context.IsSearchBoxVisible;

		public SearchAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.ToolbarViewModel.SwitchSearchBoxVisibility();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsSearchBoxVisible):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
