// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class GitFetchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		public string Label
			=> "GitFetch".GetLocalizedResource();

		public string Description
			=> "GitFetchDescription".GetLocalizedResource();

		public bool IsExecutable
			=> _context.CanExecuteGitAction;

		public GitFetchAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
			
			_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			GitHelpers.FetchOrigin(_context.ShellPage!.InstanceViewModel.GitRepositoryPath);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
