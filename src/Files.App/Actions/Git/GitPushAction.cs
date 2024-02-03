// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class GitPushAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Push".GetLocalizedResource();

		public string Description { get; } = "GitPushDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE74A");

		public bool IsExecutable =>
			ContentPageContext.CanExecuteGitAction;

		public GitPushAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return GitHelpers.PushToOriginAsync(
				ContentPageContext.ShellPage?.InstanceViewModel.GitRepositoryPath,
				ContentPageContext.ShellPage?.InstanceViewModel.GitBranchName);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
