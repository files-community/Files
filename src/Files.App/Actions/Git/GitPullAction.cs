// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class GitPullAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "GitPull".GetLocalizedResource();

		public string Description
			=> "GitPullDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE74B");

		public bool IsExecutable
			=> ContentPageContext.CanExecuteGitAction;

		public GitPullAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return GitHelpers.PullOriginAsync(ContentPageContext.ShellPage!.InstanceViewModel.GitRepositoryPath);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
