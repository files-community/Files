// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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

		public ActionCategory Category
			=> ActionCategory.DualPane;

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
			var shellPage = ContentPageContext.ShellPage ?? throw new InvalidOperationException("An active shell page is required to split the pane.");
			var paneHolder = shellPage.GetRequiredPaneHolder();
			var shellViewModel = shellPage.GetRequiredShellViewModel();
			paneHolder.OpenSecondaryPane(shellViewModel.WorkingDirectory ?? string.Empty, ShellPaneArrangement.Vertical);

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
