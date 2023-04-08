using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class PushAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly PushOptions pushOptions;

		public string Label { get; } = "Push".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uE74A");

		public bool IsExecutable => context.IsGitRepository;

		public PushAction()
		{
			context.PropertyChanged += Context_PropertyChanged;

			pushOptions = new();
			pushOptions.CredentialsProvider = new CredentialsHandler(
				(url, username, types) =>
				new UsernamePasswordCredentials
				{
					Username = "user",
					Password = "pass"
				});
		}

		public Task ExecuteAsync()
		{
			using var repository = new Repository(context.GitRepositoryPath);
			var branch = repository.Branches[context.GitBranchName];
			repository.Network.Push(branch, pushOptions);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsGitRepository):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
