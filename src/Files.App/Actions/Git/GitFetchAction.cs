// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class GitFetchAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "GitFetch".GetLocalizedResource();

		public string Description
			=> "GitFetchDescription".GetLocalizedResource();

		public bool IsExecutable
			=> ContentPageContext.CanExecuteGitAction;

		public GitFetchAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			GitHelpers.FetchOrigin(ContentPageContext.ShellPage!.InstanceViewModel.GitRepositoryPath);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
