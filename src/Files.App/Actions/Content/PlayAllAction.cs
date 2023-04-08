using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Helpers;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class PlayAllAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "PlayAll".GetLocalizedResource();

		public string Description { get; } = "TODO: Need to be described.";

		public RichGlyph Glyph { get; } = new("\uE768");

		public bool IsExecutable => context.PageType is not ContentPageTypes.RecycleBin &&
			context.SelectedItems.Count > 1 &&
			context.SelectedItems.All(item => FileExtensionHelpers.IsMediaFile(item.FileExtension));

		public PlayAllAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return NavigationHelpers.OpenSelectedItems(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
