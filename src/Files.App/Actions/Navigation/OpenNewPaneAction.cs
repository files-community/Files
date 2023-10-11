// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenNewPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "NavigationToolbarNewPane/Label".GetLocalizedResource();

		public string Description
			=> "OpenNewPaneDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemPlus, KeyModifiers.MenuShift);

		public HotKey SecondHotKey
			=> new(Keys.Add, KeyModifiers.MenuShift);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenNewPane");

		public bool IsExecutable => 
			context.IsMultiPaneEnabled &&
			!context.IsMultiPaneActive;

		public OpenNewPaneAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.ShellPage!.PaneHolder.OpenPathInNewPane("Home");

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
