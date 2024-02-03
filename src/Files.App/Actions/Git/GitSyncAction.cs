// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class GitSyncAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "GitSync".GetLocalizedResource();

		public string Description { get; } = "GitSyncDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uEDAB");

		public bool IsExecutable =>
			ContentPageContext.CanExecuteGitAction;

		public GitSyncAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var instance = ContentPageContext.ShellPage?.InstanceViewModel;

			return GitHelpers.PullOriginAsync(instance?.GitRepositoryPath)
				.ContinueWith(t => GitHelpers.PushToOriginAsync(
					instance?.GitRepositoryPath,
					instance?.GitBranchName));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
