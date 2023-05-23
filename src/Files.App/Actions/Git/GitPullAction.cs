using Files.App.Commands;
using Files.App.Contexts;
using LibGit2Sharp;

namespace Files.App.Actions
{
	internal class GitPullAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly PullOptions options;

		public string Label { get; } = "GitPull".GetLocalizedResource();

		public string Description { get; } = "GitPullDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new("\uE74B");

		public bool IsExecutable
			=> context.IsGitRepository;

		public GitPullAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			options = new PullOptions();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			using var repository = new Repository(context.ShellPage!.InstanceViewModel.GitRepositoryPath);
			var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
			if (signature is null)
				return Task.CompletedTask;

			LibGit2Sharp.Commands.Pull(
				repository,
				signature,
				options);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.IsGitRepository))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
