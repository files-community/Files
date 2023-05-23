using Files.App.Contexts;
using LibGit2Sharp;

namespace Files.App.Actions
{
	internal class GitFetchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly FetchOptions options;

		public string Label { get; } = "GitFetch".GetLocalizedResource();

		public string Description { get; } = "GitFetchDescription".GetLocalizedResource();

		public bool IsExecutable 
			=> context.CanExecuteGitAction;

		public GitFetchAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			options = new FetchOptions();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			using var repository = new Repository(context.ShellPage!.InstanceViewModel.GitRepositoryPath);

			var remote = repository.Network.Remotes["origin"];

			// TODO: Toggle IShellPage.IsExecutingGitAction

			LibGit2Sharp.Commands.Fetch(
				repository, 
				remote.Url, 
				remote.FetchRefSpecs.Select(rs => rs.Specification), 
				options, 
				"Refs updated");

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanExecuteGitAction))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
