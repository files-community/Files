// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class GitCloneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext pageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		public string Label { get; } = Strings.Clone.GetLocalizedResource();

		public string Description { get; } = Strings.GitCloneDescription.GetLocalizedResource();

		public bool IsExecutable
			=> pageContext.CanCreateItem && !pageContext.IsGitRepository;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Git");

		public GitCloneAction()
		{
			pageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (pageContext.ShellPage is null)
				return Task.CompletedTask;

			var repoUrl = parameter?.ToString() ?? string.Empty;
			var viewModel = new CloneRepoDialogViewModel(repoUrl, pageContext.ShellPage.ShellViewModel.WorkingDirectory);
			return dialogService.ShowDialogAsync(viewModel);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanCreateItem):
				case nameof(IContentPageContext.IsGitRepository):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
