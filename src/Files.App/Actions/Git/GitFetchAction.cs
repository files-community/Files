// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class GitFetchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		public string Label
			=> Strings.GitFetch.GetLocalizedResource();

		public string Description
			=> Strings.GitFetchDescription.GetLocalizedResource();

		public bool IsExecutable
			=> _context.CanExecuteGitAction;

		public GitFetchAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			await GitHelpers.FetchOrigin(_context.ShellPage!.InstanceViewModel.GitRepositoryPath);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
