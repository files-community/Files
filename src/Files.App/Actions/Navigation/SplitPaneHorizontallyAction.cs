// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class SplitPaneHorizontallyAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.SplitPaneHorizontally.GetLocalizedResource();

		public string Description
			=> Strings.SplitPaneHorizontallyDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.H, KeyModifiers.AltShift);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Panes.Horizontal");

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneAvailable &&
			!ContentPageContext.IsMultiPaneActive;

		public SplitPaneHorizontallyAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage!.PaneHolder.OpenSecondaryPane(ContentPageContext.ShellPage!.ShellViewModel.WorkingDirectory, ShellPaneArrangement.Horizontal);

			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsMultiPaneAvailable):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
