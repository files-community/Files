using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class AddBranchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "AddBranch".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uE710");

		public bool IsExecutable => context.IsGitRepository;

		public AddBranchAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return GitHelpers.CreateNewBranch(context.GitRepositoryPath!);
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
