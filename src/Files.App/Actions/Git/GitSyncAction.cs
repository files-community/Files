using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class GitSyncAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;

		public string Label { get; } = "GitSync".GetLocalizedResource();

		public string Description { get; } = "GitSyncDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uEDAB");

		public bool IsExecutable =>
			_context.CanExecuteGitAction;

		public GitSyncAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			GitHelpers.PullOrigin(_context.ShellPage?.InstanceViewModel.GitRepositoryPath);
			return GitHelpers.PushToOrigin(
				_context.ShellPage?.InstanceViewModel.GitRepositoryPath,
				_context.ShellPage?.InstanceViewModel.GitBranchName);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
