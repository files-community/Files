// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SplitPaneHorizontallyAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.SplitPaneHorizontally.GetLocalizedResource();

		public string Description
			=> Strings.SplitPaneHorizontallyDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.DualPane;

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
			var shellPage = ContentPageContext.ShellPage ?? throw new InvalidOperationException("An active shell page is required to split the pane.");
			var paneHolder = shellPage.GetRequiredPaneHolder();
			var shellViewModel = shellPage.GetRequiredShellViewModel();
			paneHolder.OpenSecondaryPane(shellViewModel.WorkingDirectory ?? string.Empty, ShellPaneArrangement.Horizontal);

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
