// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class SplitPaneHorizontallyAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "SplitPaneHorizontally".GetLocalizedResource();

		public string Description
			=> "SplitPaneHorizontallyDescription".GetLocalizedResource();

		//public HotKey HotKey
		//	=> new(Keys.OemPlus, KeyModifiers.AltShift);

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconHorizontalPane");

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneEnabled &&
			!ContentPageContext.IsMultiPaneActive;

		public SplitPaneHorizontallyAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage!.PaneHolder.SplitCurrentPane();

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
