using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class PullAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly PullOptions pullOptions;

		private readonly Signature signature;

		public string Label { get; } = "Pull".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uE74B");

		public bool IsExecutable => context.IsGitRepository;

		public PullAction()
		{
			context.PropertyChanged += Context_PropertyChanged;

			pullOptions = new PullOptions();
			pullOptions.FetchOptions = new FetchOptions();
			pullOptions.FetchOptions.CredentialsProvider = new CredentialsHandler(
				(url, username, types) =>
				new UsernamePasswordCredentials
				{
					Username = "user",
					Password = "pass"
				});
			signature = new Signature(new Identity("Temp", "Temp"), DateTime.Now);
		}

		public Task ExecuteAsync()
		{
			using var repository = new Repository(context.GitRepositoryPath);
			LibGit2Sharp.Commands.Pull(repository, signature, pullOptions);

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
