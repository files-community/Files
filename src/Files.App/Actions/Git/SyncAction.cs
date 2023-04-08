using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class SyncAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "Sync".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";
		
		public RichGlyph Glyph { get; } = new("\uE895");

		public bool IsExecutable => context.IsGitRepository;

		public SyncAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			var commands = Ioc.Default.GetRequiredService<ICommandManager>();

			return commands.Pull.ExecuteAsync().ContinueWith(t => commands.Push.ExecuteAsync());
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
