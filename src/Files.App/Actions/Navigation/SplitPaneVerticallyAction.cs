// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SplitPaneVerticallyAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.SplitPaneVertically.GetLocalizedResource();

		public string Description
			=> Strings.AddVerticalPaneDescription.GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.V, KeyModifiers.AltShift);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Panes.Vertical");

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneAvailable &&
			!ContentPageContext.IsMultiPaneActive;

		public SplitPaneVerticallyAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage!.PaneHolder.OpenSecondaryPane(ContentPageContext.ShellPage!.ShellViewModel.WorkingDirectory, ShellPaneArrangement.Vertical);

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
